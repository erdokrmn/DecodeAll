using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace DecodeAI.Web.Services
{
    public class EncodeService
    {
        public async Task EncodeTextToFileAsync(string text, string outputPath)
        {
            try
            {
                if (string.IsNullOrEmpty(text))
                    throw new ArgumentNullException(nameof(text));

                using var outputFileStream = new FileStream(outputPath, FileMode.Create);
                using var compressionStream = new GZipStream(outputFileStream, CompressionLevel.Optimal);
                using var writer = new StreamWriter(compressionStream, Encoding.UTF8);
                await writer.WriteAsync(text);
            }
            catch (Exception ex)
            {
                throw new Exception($"EncodeTextToFileAsync Hatası: {ex.Message}");
            }
        }

        public async Task EncodeBinaryFileAsync(string inputPath, string outputPath)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(inputPath);

                using var outputFileStream = new FileStream(outputPath, FileMode.Create);
                using var compressionStream = new GZipStream(outputFileStream, CompressionLevel.Optimal);
                await compressionStream.WriteAsync(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                throw new Exception($"EncodeBinaryFileAsync Hatası: {ex.Message}");
            }
        }
    }
}
