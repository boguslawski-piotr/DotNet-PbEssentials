using System;
using System.IO;
using System.Threading.Tasks;
using pbXNet;

namespace pbXSecurity
{
    public static class Tools
    {
        public static string Obfuscate(string d)
        {
            ICompressor compressor = new DeflateCompressor();
            d = compressor.Compress(d);
            using (MemoryStream s = d.ToMemoryStream())
            {
                byte[] a = s.ToArray();
                return Convert.ToBase64String(a);
            }
        }

        public static async Task<string> ObfuscateAsync(string d)
            => await Task.Run(() => Obfuscate(d));

        public static string DeObfuscate(string d)
        {
            byte[] a = Convert.FromBase64String(d);
            using (MemoryStream s = new MemoryStream(a))
            {
                d = ConvertEx.ToString(s);
                ICompressor compressor = new DeflateCompressor();
                return compressor.Decompress(d);
            }
        }

        static public async Task<string> DeObfuscateAsync(string d)
            => await Task.Run(() => DeObfuscate(d));
    }
}
