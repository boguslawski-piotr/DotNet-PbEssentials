using System.IO;
using System.Threading.Tasks;

#if PLUGIN_PBXSETTINGS
namespace Plugin.pbXSettings.pbXNet
#else
namespace pbXNet
#endif

{
	public interface ICompressor
	{
		T Compress<T>(Stream from) where T: Stream, new();
		T Decompress<T>(Stream from) where T : Stream, new();

		string Compress(string d, bool returnAsBase64 = false);
		string Decompress(string d, bool fromBase64 = false);
	}
}
