using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using K4os.Compression.LZ4.Streams;

namespace DecodeAI.Services
{
    public class UnityFSHeader
    {
        public string Signature { get; set; } = "";
        public string VersionString { get; set; } = "";
        public int UnityVersion { get; set; }
        public long MetadataSize { get; set; }
        public long FileSize { get; set; }
        public int FormatVersion { get; set; }
        public bool IsCompressed { get; set; }
    }

    public class UnityBlockInfo
    {
        public int DecompressedSize;
        public int CompressedSize;
        public byte Flags;
    }

    public static class UnityFSHeaderParser
    {
        public static UnityFSHeader? ParseHeader(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(fs);

                var signature = Encoding.ASCII.GetString(reader.ReadBytes(8)).TrimEnd('\0');
                if (!(signature.StartsWith("UnityFS") || signature.StartsWith("UnityRaw"))) return null;

                var versionStr = ReadNullTerminated(reader);
                var unityVersionStr = ReadNullTerminated(reader);
                var formatVersion = reader.ReadInt32();
                var fileSize = reader.ReadInt64();
                var metadataSize = reader.ReadInt32();
                var compressedFlag = reader.ReadInt32();

                return new UnityFSHeader
                {
                    Signature = signature,
                    VersionString = unityVersionStr,
                    UnityVersion = int.TryParse(unityVersionStr.Split('.')[0], out int uv) ? uv : 0,
                    FormatVersion = formatVersion,
                    FileSize = fileSize,
                    MetadataSize = metadataSize,
                    IsCompressed = (compressedFlag & 0x80) != 0
                };
            }
            catch
            {
                return null;
            }
        }

        public static List<UnityBlockInfo> ParseMetadataBlocks(byte[] metadata, int startOffset)
        {
            var blocks = new List<UnityBlockInfo>();
            try
            {
                using var stream = new MemoryStream(metadata, startOffset, metadata.Length - startOffset);
                using var reader = new BinaryReader(stream);

                int blockCount = reader.ReadInt32();
                if (blockCount < 0 || blockCount > 1000)
                    return blocks; // mantıksızsa direkt boş döner

                for (int i = 0; i < blockCount; i++)
                {
                    int decompressedSize = reader.ReadInt32();
                    int compressedSize = reader.ReadInt32();
                    byte flags = reader.ReadByte();
                    blocks.Add(new UnityBlockInfo
                    {
                        DecompressedSize = decompressedSize,
                        CompressedSize = compressedSize,
                        Flags = flags
                    });
                }

            }
            catch
            {
                // ignore malformed offset
            }
            return blocks;
        }

        public static string ReadNullTerminated(BinaryReader reader)
        {
            var sb = new StringBuilder();
            byte b;
            while ((b = reader.ReadByte()) != 0)
            {
                sb.Append((char)b);
            }
            return sb.ToString();
        }
    }

    public static class UnityBlockDecompressor
    {
        public static byte[] DecompressBlocks(List<UnityBlockInfo> blocks, BinaryReader reader)
        {
            using var output = new MemoryStream();

            foreach (var block in blocks)
            {
                byte[] data = reader.ReadBytes(block.CompressedSize);

                if ((block.Flags & 0x80) != 0) // LZ4 compressed
                {
                    using var compressedStream = new MemoryStream(data);
                    using var lz4Stream = new LZ4DecoderStream(compressedStream, null, leaveOpen: true);
                    lz4Stream.CopyTo(output);
                }
                else // uncompressed
                {
                    output.Write(data, 0, data.Length);
                }
            }

            return output.ToArray();
        }

        public static (byte[]?, bool, string) TryUnityFSDecompress(byte[] fileBytes)
        {
            try
            {
                using var stream = new MemoryStream(fileBytes);
                using var reader = new BinaryReader(stream);

                // Parse header
                var signature = Encoding.ASCII.GetString(reader.ReadBytes(8)).TrimEnd('\0');
                if (!(signature.StartsWith("UnityFS") || signature.StartsWith("UnityRaw"))) return (null, false, "UnityFSDecompress");

                var versionStr = UnityFSHeaderParser.ReadNullTerminated(reader);
                var unityVersionStr = UnityFSHeaderParser.ReadNullTerminated(reader);
                var formatVersion = reader.ReadInt32();
                var fileSize = reader.ReadInt64();
                var metadataSize = reader.ReadInt32();
                var compressedFlag = reader.ReadInt32();

                bool isCompressed = (compressedFlag & 0x80) != 0;

                byte[] metadataBytes = reader.ReadBytes(metadataSize);
                if (isCompressed)
                {
                    using var compStream = new MemoryStream(metadataBytes);
                    using var lz4Stream = new LZ4DecoderStream(compStream, null, leaveOpen: true);
                    using var ms = new MemoryStream();
                    lz4Stream.CopyTo(ms);
                    metadataBytes = ms.ToArray();
                }

                List<UnityBlockInfo> blocks = new();
                for (int offset = 0; offset < Math.Min(4096, metadataBytes.Length - 9); offset += 4)
                {
                    var candidate = UnityFSHeaderParser.ParseMetadataBlocks(metadataBytes, offset);
                    if (candidate.Count > 0)
                    {
                        blocks = candidate;
                        break;
                    }
                }

                if (blocks.Count == 0)
                    return (null, false, "UnityFSDecompress");

                var decompressedData = UnityBlockDecompressor.DecompressBlocks(blocks, reader);
                return (decompressedData, true, "UnityFSDecompress");
            }
            catch
            {
                return (null, false, "UnityFSDecompress");
            }
        }
    }
}
