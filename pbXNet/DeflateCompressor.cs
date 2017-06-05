using System;
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

		public string Compress(string d, bool returnAsBase64 = false)
		{
			// Compress stream...
			MemoryStream dcs = Compress(ConvertEx.ToMemoryStream(d));
			dcs.Position = 0;
			byte[] dca = dcs.ToArray();
			dcs.Dispose();

			// Build string from byte array...
			if (!returnAsBase64)
				d = ConvertEx.ToHexString(dca);
			else
				d = Convert.ToBase64String(dca);
			return d;
		}

		public async Task<string> CompressAsync(string d, bool returnAsBase64 = false)
			=> await Task.Run(() => Compress(d, returnAsBase64));

		public string Decompress(string d, bool fromBase64 = false)
		{
			// Decompress...
			MemoryStream dms = new MemoryStream(!fromBase64 ? ConvertEx.FromHexString(d) : Convert.FromBase64String(d));
			MemoryStream dcs = Decompress(dms);

			// Build string to return...
			dcs.Position = 0;
			d = ConvertEx.ToString(dcs);

			dcs.Dispose();
			dms.Dispose();
			return d;
		}

		public async Task<string> DecompressAsync(string d, bool fromBase64 = false)
			=> await Task.Run(() => Decompress(d, fromBase64));

	}
}

