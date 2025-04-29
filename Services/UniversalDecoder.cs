using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DecodeAI
{
    public class UniversalDecoder
    {
        private readonly List<Func<byte[], (byte[]? result, bool success, string methodName)>> decompressMethods;
        private readonly List<Func<byte[], (string? result, bool success, string methodName)>> decodeMethods;
        private readonly List<Func<string, (string? result, bool success, string methodName)>> textExtractors;

        public UniversalDecoder()
        {
            decompressMethods = new List<Func<byte[], (byte[]?, bool, string)>>
            {
                TryGzipDecompress,
                TryZlibDecompress,
                TryBrotliDecompress,
                TryDeflateDecompress,
                TryBzip2Decompress
            };

            decodeMethods = new List<Func<byte[], (string?, bool, string)>>
            {
                TryPlainUtf8,
                TryUtf16LeDecode,
                TryUtf16BeDecode,
                TryLatin1Decode,
                TryShiftJisDecode
            };

            textExtractors = new List<Func<string, (string?, bool, string)>>
            {
                TryNullTerminatedStrings,
                TryPrintableOnlyExtract
            };
        }

        public async Task<(string decodedText, List<string> methodChain, bool success)> SmartDecodeAsync(byte[] originalData)
        {
            var methodChain = new List<string>();
            var currentData = originalData;
            bool decodeAttempted = false;
            bool anyDecodeSuccess = false;

            // --- Decompress While Döngüsü ---
            int decompressIndex = 0;
            var (decompressedData, decompressSuccess, decompressMethod) = decompressMethods[decompressIndex](originalData);

            if (decompressSuccess && decompressedData != null && decompressedData.Length > 0)
            {
                currentData = decompressedData;
                methodChain.Add(decompressMethod);
            }
            decompressIndex++;
           

            // --- Decode Döngüsü ---
            foreach (var decode in decodeMethods)
            {
                decodeAttempted = true;
                var (decodedText, decodeSuccess, decodeMethod) = decode(currentData);
                if (!decodeSuccess)
                {
                    await SaveFailedChainAsync(decodedText ?? string.Empty, new List<string>(methodChain) { decodeMethod });
                    continue;
                }

                anyDecodeSuccess = true;
                methodChain.Add(decodeMethod);

                // --- Extract Döngüsü ---
                foreach (var extract in textExtractors)
                {
                    var (extractedText, extractSuccess, extractMethod) = extract(decodedText!);
                    if (!extractSuccess)
                    {
                        await SaveFailedChainAsync(extractedText ?? string.Empty, new List<string>(methodChain) { extractMethod });
                        continue;
                    }

                    if (IsLikelyHumanReadable(extractedText!))
                    {
                        methodChain.Add(extractMethod);
                        await SaveSuccessfulDecodeAsync(extractedText!, methodChain);
                        return (extractedText!, methodChain, true);
                    }
                    else
                    {
                        await SaveFailedChainAsync(extractedText ?? string.Empty, new List<string>(methodChain) { extractMethod });
                    }
                }

                methodChain.Remove(decodeMethod); // Eğer extractor adımları başarısızsa decodeMethod'u zincirden çıkar
            }

            return ("", new List<string>(), false);
        }



        #region Decompress Methods

        private (byte[]?, bool, string) TryGzipDecompress(byte[] bytes)
        {
            try
            {
                using var compressedStream = new MemoryStream(bytes);
                using var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                using var output = new MemoryStream();
                decompressionStream.CopyTo(output);
                return (output.ToArray(), true, nameof(TryGzipDecompress));
            }
            catch { return (null, false, nameof(TryGzipDecompress)); }
        }

        private (byte[]?, bool, string) TryZlibDecompress(byte[] bytes)
        {
            try
            {
                using var compressedStream = new MemoryStream(bytes, 2, bytes.Length - 2);
                using var decompressionStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
                using var output = new MemoryStream();
                decompressionStream.CopyTo(output);
                return (output.ToArray(), true, nameof(TryZlibDecompress));
            }
            catch { return (null, false, nameof(TryZlibDecompress)); }
        }

        private (byte[]?, bool, string) TryBrotliDecompress(byte[] bytes)
        {
            try
            {
                using var compressedStream = new MemoryStream(bytes);
                using var decompressionStream = new BrotliStream(compressedStream, CompressionMode.Decompress);
                using var output = new MemoryStream();
                decompressionStream.CopyTo(output);
                return (output.ToArray(), true, nameof(TryBrotliDecompress));
            }
            catch { return (null, false, nameof(TryBrotliDecompress)); }
        }

        private (byte[]?, bool, string) TryDeflateDecompress(byte[] bytes)
        {
            try
            {
                using var compressedStream = new MemoryStream(bytes);
                using var decompressionStream = new DeflateStream(compressedStream, CompressionMode.Decompress);
                using var output = new MemoryStream();
                decompressionStream.CopyTo(output);
                return (output.ToArray(), true, nameof(TryDeflateDecompress));
            }
            catch { return (null, false, nameof(TryDeflateDecompress)); }
        }

        private (byte[]?, bool, string) TryBzip2Decompress(byte[] bytes)
        {
            try
            {
                using var compressedStream = new MemoryStream(bytes);
                using var bz2Stream = new ICSharpCode.SharpZipLib.BZip2.BZip2InputStream(compressedStream);
                using var output = new MemoryStream();
                bz2Stream.CopyTo(output);
                return (output.ToArray(), true, nameof(TryBzip2Decompress));
            }
            catch { return (null, false, nameof(TryBzip2Decompress)); }
        }

        #endregion

        #region Decode Methods

        private (string?, bool, string) TryPlainUtf8(byte[] bytes)
        {
            try { return (Encoding.UTF8.GetString(bytes), true, nameof(TryPlainUtf8)); } catch { return (null, false, nameof(TryPlainUtf8)); }
        }

        private (string?, bool, string) TryUtf16LeDecode(byte[] bytes)
        {
            try { return (Encoding.Unicode.GetString(bytes), true, nameof(TryUtf16LeDecode)); } catch { return (null, false, nameof(TryUtf16LeDecode)); }
        }

        private (string?, bool, string) TryUtf16BeDecode(byte[] bytes)
        {
            try { return (Encoding.BigEndianUnicode.GetString(bytes), true, nameof(TryUtf16BeDecode)); } catch { return (null, false, nameof(TryUtf16BeDecode)); }
        }

        private (string?, bool, string) TryLatin1Decode(byte[] bytes)
        {
            try { return (Encoding.GetEncoding("iso-8859-1").GetString(bytes), true, nameof(TryLatin1Decode)); } catch { return (null, false, nameof(TryLatin1Decode)); }
        }

        private (string?, bool, string) TryShiftJisDecode(byte[] bytes)
        {
            try { return (Encoding.GetEncoding("shift_jis").GetString(bytes), true, nameof(TryShiftJisDecode)); } catch { return (null, false, nameof(TryShiftJisDecode)); }
        }

        #endregion

        #region Text Extractors

        private (string?, bool, string) TryNullTerminatedStrings(string text)
        {
            try
            {
                var parts = text.Split('\0').Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
                return (string.Join("\n", parts), parts.Count > 0, nameof(TryNullTerminatedStrings));
            }
            catch { return (null, false, nameof(TryNullTerminatedStrings)); }
        }

        private (string?, bool, string) TryPrintableOnlyExtract(string text)
        {
            try
            {
                var printable = new string(text.Where(c => (c >= 32 && c <= 126) || c == '\n' || c == '\r' || c == '\t').ToArray());
                return (printable.Length > 0 ? printable : null, printable.Length > 0, nameof(TryPrintableOnlyExtract));
            }
            catch { return (null, false, nameof(TryPrintableOnlyExtract)); }
        }

        #endregion

        private bool IsLikelyHumanReadable(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            double printableRatio = text.Count(c => (c >= 32 && c <= 126) || c == '\n' || c == '\r' || c == '\t') / (double)text.Length;
            return printableRatio > 0.7;
        }

        private async Task SaveSuccessfulDecodeAsync(string decodedText, List<string> methodChain)
        {
            var basePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "DecodeResults");
            Directory.CreateDirectory(basePath);
            var filename = Path.Combine(basePath, $"{string.Join("_", methodChain)}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            await File.WriteAllTextAsync(filename, decodedText);
        }

        private async Task SaveFailedChainAsync(string content, List<string> methodChain)
        {
            var basePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "FailedDecodeChains");
            Directory.CreateDirectory(basePath);
            var filename = Path.Combine(basePath, $"{string.Join("_", methodChain)}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            await File.WriteAllTextAsync(filename, content);
        }
    }
}
