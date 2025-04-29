using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class DecodeResult2
{
    public bool Success { get; set; }
    public string? DecodedText { get; set; }
    public string? Reason { get; set; }
}

public class SmartDecoderService
{
    private const double PrintableCharacterThreshold = 0.30;
    private const int MaxFailedSaveSize = 20 * 1024; // 20 KB
    private const string FailedDecodeDirectory = "FailedDecodes";

    public async Task<DecodeResult2> SmartDecodeAsync(byte[] data)
    {
        if (!IsLikelyTextFile(data))
        {
            await SaveFailedDecodeAsync(data, "InitialPrintableCheck");
            return new DecodeResult2 { Success = false, DecodedText = null, Reason = "Low printable character ratio" };
        }

        // 1- Try plain UTF-8 decoding
        var plainUtf8 = TryPlainUtf8(data);
        if (plainUtf8 != null)
            return new DecodeResult2 { Success = true, DecodedText = plainUtf8, Reason = "PlainUtf8" };
        else
            await SaveFailedDecodeAsync(data, "PlainUtf8");

        // 2- Try UTF-16 LE decoding
        var utf16le = TryUtf16Le(data);
        if (utf16le != null)
            return new DecodeResult2 { Success = true, DecodedText = utf16le, Reason = "Utf16Le" };
        else
            await SaveFailedDecodeAsync(data, "Utf16Le");

        // 3- Try Gzip decompress
        var gzip = await TryGzipDecompress(data);
        if (gzip != null)
            return new DecodeResult2 { Success = true, DecodedText = gzip, Reason = "GzipDecompress" };
        else
            await SaveFailedDecodeAsync(data, "GzipDecompress");

        // 4- Try Zlib decompress
        var zlib = await TryZlibDecompress(data);
        if (zlib != null)
            return new DecodeResult2 { Success = true, DecodedText = zlib, Reason = "ZlibDecompress" };
        else
            await SaveFailedDecodeAsync(data, "ZlibDecompress");

        // 5- Try simple XOR decrypt (optional ileri ekleriz)
        // var xor = TryXorBruteForce(data);
        // if (xor != null)
        //     return new DecodeResult { Success = true, DecodedText = xor, Reason = "XorBruteForce" };
        // else
        //     await SaveFailedDecodeAsync(data, "XorBruteForce");

        return new DecodeResult2 { Success = false, DecodedText = null, Reason = "All decoding methods failed" };
    }

    private bool IsLikelyTextFile(byte[] data)
    {
        int printable = data.Count(b => (b >= 32 && b <= 126) || b == 9 || b == 10 || b == 13);
        double ratio = (double)printable / data.Length;
        return ratio >= PrintableCharacterThreshold;
    }

    private async Task SaveFailedDecodeAsync(byte[] data, string methodName)
    {
        try
        {
            Directory.CreateDirectory(FailedDecodeDirectory);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var hash = Guid.NewGuid().ToString("N").Substring(0, 8);
            var filename = $"{methodName}_failed_{timestamp}_{hash}.txt";
            var fullPath = Path.Combine(FailedDecodeDirectory, filename);

            int saveLength = Math.Min(data.Length, MaxFailedSaveSize);
            await File.WriteAllBytesAsync(fullPath, data.Take(saveLength).ToArray());
        }
        catch (Exception ex)
        {
            // Loglama yapabilirsin istersen
            Console.WriteLine($"Failed to save failed decode: {ex.Message}");
        }
    }

    private string? TryPlainUtf8(byte[] data)
    {
        try
        {
            var text = Encoding.UTF8.GetString(data);
            if (text.Any(c => !char.IsControl(c) || c == '\n' || c == '\r'))
                return text;
        }
        catch
        {
            // UTF-8 decode hatası
        }
        return null;
    }

    private string? TryUtf16Le(byte[] data)
    {
        try
        {
            var text = Encoding.Unicode.GetString(data); // Unicode == UTF-16LE
            if (text.Any(c => !char.IsControl(c) || c == '\n' || c == '\r'))
                return text;
        }
        catch
        {
            // UTF-16LE decode hatası
        }
        return null;
    }

    private async Task<string?> TryGzipDecompress(byte[] data)
    {
        try
        {
            using var inputStream = new MemoryStream(data);
            using var gzipStream = new System.IO.Compression.GZipStream(inputStream, System.IO.Compression.CompressionMode.Decompress);
            using var reader = new StreamReader(gzipStream, Encoding.UTF8);
            var text = await reader.ReadToEndAsync();
            return text;
        }
        catch
        {
            // Gzip decompress hatası
        }
        return null;
    }

    private async Task<string?> TryZlibDecompress(byte[] data)
    {
        try
        {
            using var inputStream = new MemoryStream(data);
            inputStream.ReadByte(); // Zlib header byte 1
            inputStream.ReadByte(); // Zlib header byte 2
            using var deflateStream = new System.IO.Compression.DeflateStream(inputStream, System.IO.Compression.CompressionMode.Decompress);
            using var reader = new StreamReader(deflateStream, Encoding.UTF8);
            var text = await reader.ReadToEndAsync();
            return text;
        }
        catch
        {
            // Zlib decompress hatası
        }
        return null;
    }
}
