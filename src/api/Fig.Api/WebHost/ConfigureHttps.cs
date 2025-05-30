using Serilog.Core;
using System.Security.Cryptography.X509Certificates;

namespace Fig.Api.WebHost
{
    public static class ConfigureHttps
    {
        public static void ConfigureHttpsListener(this ConfigureWebHostBuilder builder, Logger logger)
        {
            var certPath = Environment.GetEnvironmentVariable("SSL_CERT_PATH");
            var keyPath = Environment.GetEnvironmentVariable("SSL_KEY_PATH");
            var sslPortString = Environment.GetEnvironmentVariable("FIG_API_SSL_PORT");
            
            if (certPath != null && keyPath != null && int.TryParse(sslPortString, out int sslPort))
            {
                builder.ConfigureKestrel((context, serverOptions) =>
                {
                    var exportableCert = X509CertificateLoader.LoadPkcs12(X509Certificate2.CreateFromPemFile(certPath, keyPath).Export(X509ContentType.Pfx, ""), "", X509KeyStorageFlags.Exportable);
                    serverOptions.Configure()
                    .AnyIPEndpoint(sslPort, listenOptions =>
                    {
                        listenOptions.UseHttps(exportableCert);
                    });
                });
            }
            else
            {
                if (certPath == null)
                {
                    logger.Warning("Environment variable SSL_CERT_PATH is not set. Unable to configure fig for https");
                }
                if (keyPath == null)
                {
                    logger.Warning("Environment variable SSL_KEY_PATH is not set. Unable to configure fig for https");
                }
                if (sslPortString == null)
                {
                    logger.Warning("Environment variable FIG_API_SSL_PORT is not set. Unable to configure fig for https");
                }
                else if (!int.TryParse(sslPortString, out int _))
                {
                    logger.Warning("Environment variable FIG_API_SSL_PORT does not have a valid value that can be parsed to an int. Unable to configure fig for https");
                }
            }
        }
    }
}
