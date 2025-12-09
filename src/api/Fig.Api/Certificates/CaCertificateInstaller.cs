using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Fig.Api.Certificates;

/// <summary>
/// Installs CA certificates from a configured directory into the system trust store.
/// This enables trusted SSL/TLS connections to services (like SQL Server) that use certificates
/// signed by custom or enterprise Certificate Authorities.
/// </summary>
public static class CaCertificateInstaller
{
    private static readonly string[] SupportedExtensions = [".crt", ".cer", ".pem"];

    /// <summary>
    /// Installs all CA certificates found in the specified directory path.
    /// On Linux, certificates are copied to /usr/local/share/ca-certificates and update-ca-certificates is run.
    /// On Windows, certificates are added to the LocalMachine Root store.
    /// </summary>
    /// <param name="caCertificatePath">The directory path containing CA certificate files (.crt, .cer, .pem)</param>
    /// <param name="logger">Logger for recording certificate installation progress</param>
    public static void InstallCertificates(string? caCertificatePath, Serilog.ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(caCertificatePath))
        {
            logger.Debug("CA certificate path not configured. Skipping CA certificate installation");
            return;
        }

        if (!Directory.Exists(caCertificatePath))
        {
            logger.Warning("CA certificate path '{CaCertificatePath}' does not exist. Skipping CA certificate installation", 
                caCertificatePath);
            return;
        }

        var certificateFiles = GetCertificateFiles(caCertificatePath);
        
        if (certificateFiles.Length == 0)
        {
            logger.Information("No certificate files found in '{CaCertificatePath}'. Supported extensions: {Extensions}", 
                caCertificatePath, string.Join(", ", SupportedExtensions));
            return;
        }

        logger.Information("Found {Count} CA certificate file(s) in '{CaCertificatePath}'", 
            certificateFiles.Length, caCertificatePath);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            InstallCertificatesOnLinux(certificateFiles, logger);
        }
        else
        {
            logger.Warning("CA certificate installation is not supported on this platform: {Platform}", 
                RuntimeInformation.OSDescription);
        }
    }

    private static string[] GetCertificateFiles(string directoryPath)
    {
        return Directory.GetFiles(directoryPath)
            .Where(f => SupportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToArray();
    }

    private static void InstallCertificatesOnLinux(string[] certificateFiles, Serilog.ILogger logger)
    {
        const string systemCertPath = "/usr/local/share/ca-certificates";
        
        try
        {
            // Ensure the system certificate directory exists
            if (!Directory.Exists(systemCertPath))
            {
                logger.Warning("System CA certificate directory '{Path}' does not exist. " +
                    "Ensure the container has the 'ca-certificates' package installed", systemCertPath);
                return;
            }

            var installedCount = 0;
            foreach (var certFile in certificateFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(certFile);
                    // Ensure the file has .crt extension for update-ca-certificates to recognize it
                    if (!fileName.EndsWith(".crt", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName = Path.GetFileNameWithoutExtension(fileName) + ".crt";
                    }
                    
                    var destPath = Path.Combine(systemCertPath, fileName);
                    File.Copy(certFile, destPath, overwrite: true);
                    logger.Debug("Copied CA certificate '{SourceFile}' to '{DestFile}'", certFile, destPath);
                    installedCount++;
                }
                catch (Exception ex)
                {
                    logger.Warning(ex, "Failed to copy CA certificate '{CertFile}' to system store", certFile);
                }
            }

            if (installedCount > 0)
            {
                // Run update-ca-certificates to refresh the trust store
                var processInfo = new ProcessStartInfo
                {
                    FileName = "update-ca-certificates",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        logger.Information("Successfully installed {Count} CA certificate(s) to system trust store. {Output}", 
                            installedCount, output.Trim());
                    }
                    else
                    {
                        logger.Error("Failed to update CA certificates. Exit code: {ExitCode}. Error: {Error}", 
                            process.ExitCode, error);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to install CA certificates on Linux");
        }
    }
}
