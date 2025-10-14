using System.IO.Compression;
using System.Text;

namespace Fig.Web.Utils;

public static class CompressionUtil
{
    /// <summary>
    /// Maximum allowed uncompressed JSON size in bytes (100 MB).
    /// </summary>
    private const long MaxUncompressedJsonBytes = 100 * 1024 * 1024;

    /// <summary>
    /// Compresses a JSON string into a zip file containing a single entry.
    /// </summary>
    /// <param name="jsonContent">The JSON content to compress</param>
    /// <param name="entryName">The name of the file inside the zip (e.g., "export.json")</param>
    /// <returns>Byte array containing the zip file data</returns>
    public static byte[] CompressToZip(string jsonContent, string entryName)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, Encoding.UTF8);
            writer.Write(jsonContent);
        }
        
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Attempts to decompress a zip file and extract the first JSON entry.
    /// </summary>
    /// <param name="data">The byte array containing zip data</param>
    /// <param name="jsonContent">The extracted JSON content if successful</param>
    /// <returns>True if successfully extracted, false otherwise</returns>
    public static bool TryDecompressFromZip(byte[] data, out string? jsonContent)
    {
        jsonContent = null;
        
        try
        {
            using var memoryStream = new MemoryStream(data);
            using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
            
            // Find the first .json entry
            var jsonEntry = archive.Entries.FirstOrDefault(e => 
                e.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
            
            if (jsonEntry == null)
                return false;
            
            // Protection against zip bombs: check uncompressed size before extracting
            if (jsonEntry.Length > MaxUncompressedJsonBytes)
            {
                Console.WriteLine($"JSON entry too large: {jsonEntry.Length} bytes exceeds maximum of {MaxUncompressedJsonBytes} bytes");
                return false;
            }
            
            using var entryStream = jsonEntry.Open();
            using var reader = new StreamReader(entryStream, Encoding.UTF8);
            jsonContent = reader.ReadToEnd();
            
            return true;
        }
        catch (InvalidDataException ex)
        {
            Console.WriteLine($"Invalid zip data: {ex.Message}");
            return false;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"IO error reading zip file: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error decompressing file: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if the provided data appears to be a zip file by examining the file signature.
    /// </summary>
    /// <param name="data">The byte array to check</param>
    /// <returns>True if data appears to be a zip file, false otherwise</returns>
    public static bool IsZipFile(byte[] data)
    {
        if (data.Length < 4)
            return false;
        
        // Check for ZIP file signature (PK\x03\x04, PK\x05\x06 for empty zip, or PK\x07\x08 for spanned/data descriptor)
        return (data[0] == 0x50 && data[1] == 0x4B && 
                (data[2] == 0x03 || data[2] == 0x05 || data[2] == 0x07) && 
                (data[3] == 0x04 || data[3] == 0x06 || data[3] == 0x08));
    }
}
