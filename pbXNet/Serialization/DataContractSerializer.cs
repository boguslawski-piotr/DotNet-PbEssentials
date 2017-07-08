#if !NETSTANDARD1_6

using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace pbXNet
{
	public class DataContractSerializer : ISerializer
	{
		string _Serialize<T>(T o)
		{
			using (var dwriter = new StringWriter())
			using (var xmlwriter = XmlWriter.Create(dwriter))
			{
				var dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(T));
				dcs.WriteObject(xmlwriter, o);
				xmlwriter.Flush();
				return dwriter.ToString();
			}
		}

		T _Deserialize<T>(string d)
		{
			using (var dreader = new StringReader(d))
			using (var xmlreader = XmlReader.Create(dreader))
			{
				var dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(T));
				return (T)dcs.ReadObject(xmlreader);
			}
		}

		public string Serialize<T>(T o, string id = null)
		{
			string d = _Serialize<T>(o).ToByteArray().ToHexString();

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

			return _Deserialize<T>(ConvertEx.ToString(ConvertEx.FromHexString(d)));
		}
	}
}

#endif