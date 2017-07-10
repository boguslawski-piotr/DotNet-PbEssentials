#if PLUGIN_PBXSETTINGS
namespace Plugin.pbXSettings.pbXNet
#else
namespace pbXNet
#endif
{
	public static class Obfuscator
	{
		public static string Obfuscate(string d)
		{
			ICompressor compressor = new DeflateCompressor();
			return compressor.Compress(d, true);
		}

		public static string DeObfuscate(string d)
		{
			ICompressor compressor = new DeflateCompressor();
			return compressor.Decompress(d, true);
		}
	}
}
