using System.IO;
using System.IO.Compression;

namespace pbXNet
{
    public class Compressor : ICompressor
    {
        public MemoryStream Compress(Stream from)
        {
            MemoryStream to = new MemoryStream((int)from.Length);
            using (DeflateStream via = new DeflateStream(to, CompressionMode.Compress, true))
            {
                from.Position = 0;
                from.CopyTo(via);
            }
            return to;
        }

        public MemoryStream Decompress(Stream from)
        {
            MemoryStream to = new MemoryStream((int)from.Length * 2);
            using (DeflateStream via = new DeflateStream(from, CompressionMode.Decompress, true))
            {
                from.Position = 0;
                via.CopyTo(to);
            }
            return to;
        }

        public string Compress(string d)
        {
            // Compress stream...
            MemoryStream dcs = Compress(ConvertEx.ToMemoryStream(d));
            dcs.Position = 0;
            byte[] dca = dcs.ToArray();

            // Free memory...
            dcs.Dispose();

            // Build string from byte array...
            d = ConvertEx.ToHexString(dca);
            return d;
        }

        public string Decompress(string d)
        {
            // Convert string produced by Compress into byte array...
            // Decompress...
            MemoryStream dms = new MemoryStream(ConvertEx.FromHexString(d));
            MemoryStream dcs = Decompress(dms);

            // Build string to return...
            dcs.Position = 0;
            d = ConvertEx.ToString(dcs);

            // Free memory...
            dcs.Dispose();
            dms.Dispose();

            return d;
        }
    }
}

// String extension

//public static Stream ToStream(this string str)
//{
//    MemoryStream stream = new MemoryStream();
//    StreamWriter writer = new StreamWriter(stream);
//    writer.Write(str);
//    writer.Flush();
//    stream.Position = 0;
//    return stream;
//}

