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

    }
}
