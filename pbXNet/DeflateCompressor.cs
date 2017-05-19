using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace pbXNet
{
    public class DeflateCompressor : ICompressor
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

        public async Task<MemoryStream> CompressAsync(Stream from)
            => await Task.Run(() => Compress(from));

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

        public async Task<MemoryStream> DecompressAsync(Stream from) 
            => await Task.Run(() => Decompress(from));

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

        public async Task<string> CompressAsync(string d) 
            => await Task.Run(() => Compress(d));

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

        public async Task<string> DecompressAsync(string d) 
            => await Task.Run(() => Decompress(d));

    }
}

