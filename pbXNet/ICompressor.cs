using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pbXNet
{
    public interface ICompressor
    {
        MemoryStream Compress(Stream from);
        Task<MemoryStream> CompressAsync(Stream from);

        MemoryStream Decompress(Stream from);
        Task<MemoryStream> DecompressAsync(Stream from);

		string Compress(string d, bool returnAsBase64 = false);
        Task<string> CompressAsync(string d, bool returnAsBase64 = false);

		string Decompress(string d, bool fromBase64 = false);
        Task<string> DecompressAsync(string d, bool fromBase64 = false);
	}
}
