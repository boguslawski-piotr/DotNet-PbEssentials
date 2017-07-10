#if !NETSTANDARD1_6

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

#if PLUGIN_PBXSETTINGS
namespace Plugin.pbXSettings.pbXNet
#else
namespace pbXNet
#endif
{
	/// <summary>
	/// 
	/// </summary>
	public class DataContractSerializer : ISerializer
	{
		public string Serialize<T>(T o, string id = null)
		{
			string d = _Serialize<T>(o);

			if (id != null)
			{
				d = id + "_" + d + "_";
			}

			return d;
		}

		public T Deserialize<T>(string d, string id = null)
		{
			if (id != null)
			{
				Match m = Regex.Match(d, id + "_[0-9A-F]*_");
				if (m.Success)
					d = d.Substring(m.Index + id.Length + 1, m.Length - (id.Length + 2));
			}

			return _Deserialize<T>(d);
		}

		string _Serialize<T>(T o)
		{
			using (var stream = new MemoryStream())
			{
				var dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(T));
				dcs.WriteObject(stream, o);
				stream.Flush();
				return stream.ToArray().ToHexString();
			}
		}

		T _Deserialize<T>(string d)
		{
			using (var stream = new MemoryStream(d.FromHexString()))
			{
				var dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(T));
				return (T)dcs.ReadObject(stream);
			}
		}
	}
}

#endif