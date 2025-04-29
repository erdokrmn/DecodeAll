using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DecodeAI.Web.Services
{
    public class DecodeService
    {
        public async Task<(string decodedText, bool isBinary)> DecodeFileAsync(string filePath)
        {
            try
            {
                var bytes = await File.ReadAllBytesAsync(filePath);

                var universalDecoder = new UniversalDecoder();
                var (decodedText, methodChain, success) = await universalDecoder.SmartDecodeAsync(bytes);

                if (success)
                {
                    return (decodedText, false); // Başarıyla decode edildi, binary değil
                }
                else
                {
                    // Hiç çözemediysek base64 gösterelim
                    string base64 = Convert.ToBase64String(bytes);
                    return (base64, true);
                }
            }
            catch (Exception ex)
            {
                return ($"Decode hatası: {ex.Message}", true);
            }
        }


        private bool IsBinaryFile(byte[] bytes)
        {
            var text = Encoding.UTF8.GetString(bytes.Take(512).ToArray());
            return text.Any(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t');
        }

        private string DetectFileType(byte[] bytes)
        {
            if (bytes.Length >= 2)
            {
                if (bytes[0] == 0x1f && bytes[1] == 0x8b)
                    return "gzip";

                if (bytes[0] == 0x78 && (bytes[1] == 0x9c || bytes[1] == 0x01 || bytes[1] == 0xda))
                    return "zlib";
            }

            string textSample = Encoding.UTF8.GetString(bytes.Take(100).ToArray());
            if (IsBase64String(textSample))
                return "base64";

            return "plain";
        }

        private bool IsBase64String(string text)
        {
            text = text.Trim();
            return (text.Length % 4 == 0) && Regex.IsMatch(text, @"^[a-zA-Z0-9\+/]*={0,2}$");
        }

        private async Task<string> DecompressGzipAsync(byte[] bytes)
        {
            using var compressedStream = new MemoryStream(bytes);
            using var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var reader = new StreamReader(decompressionStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        private async Task<string> DecompressZlibAsync(byte[] bytes)
        {
            using var compressedStream = new MemoryStream(bytes, 2, bytes.Length - 2);
            using var decompressionStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
            using var reader = new StreamReader(decompressionStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        private string DecodeBase64(byte[] bytes)
        {
            var text = Encoding.UTF8.GetString(bytes);
            var decodedBytes = System.Convert.FromBase64String(text);
            return Encoding.UTF8.GetString(decodedBytes);
        }
    }
}
