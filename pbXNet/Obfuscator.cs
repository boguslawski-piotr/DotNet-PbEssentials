using System;
using System.IO;
using System.Threading.Tasks;
using pbXNet;

namespace pbXNet
{
	public static class Obfuscator
	{
		public static string Obfuscate(string d)
		{
			ICompressor compressor = new DeflateCompressor();
			return compressor.Compress(d, true);
		}

		public static async Task<string> ObfuscateAsync(string d)
			=> await Task.Run(() => Obfuscate(d));

		public static string DeObfuscate(string d)
		{
			ICompressor compressor = new DeflateCompressor();
			return compressor.Decompress(d, true);
		}

		static public async Task<string> DeObfuscateAsync(string d)
			=> await Task.Run(() => DeObfuscate(d));
	}
}
