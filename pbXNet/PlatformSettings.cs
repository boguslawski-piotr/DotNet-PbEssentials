using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace pbXNet
{
	public partial class PlatformSettings : Settings
	{
		public string Id { get; private set; }

		protected readonly ISerializer Serializer;

		public PlatformSettings(string id, ISerializer serializer = null)
		{
			Id = id ?? throw new ArgumentNullException(nameof(id));
			Serializer = new StringOptimizedSerializer(serializer ?? new NewtonsoftJsonSerializer());
		}

		public static async Task<PlatformSettings> NewAsync(string id, ISerializer serializer = null)
		{
			PlatformSettings s = new PlatformSettings(id, serializer);
			await s.LoadAsync();
			return s;
		}

		//string Serialize()
		//{
		//	using (var dwriter = new StringWriter())
		//	using (var xmlwriter = XmlWriter.Create(dwriter))
		//	{
		//		try
		//		{
		//			var dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(ConcurrentDictionary<string, object>));
		//			dcs.WriteObject(xmlwriter, KeysAndValues);
		//			xmlwriter.Flush();
		//			return dwriter.ToString();
		//		}
		//		catch (Exception ex)
		//		{
		//			Log.E(ex, this);
		//			return null;
		//		}
		//	}
		//}

		//ConcurrentDictionary<string, object> Deserialize(string d)
		//{
		//	using(var dreader = new StringReader(d))
		//	using(var xmlreader = XmlReader.Create(dreader))
		//	{
		//		try
		//		{
		//			var dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(ConcurrentDictionary<string, object>));
		//			return (ConcurrentDictionary<string, object>)dcs.ReadObject(xmlreader);
		//		}
		//		catch (Exception ex)
		//		{
		//			Log.E(ex, this);
		//			return null;
		//		}
		//	}
		//}

		public override async Task LoadAsync()
		{
			string d = await _GetStringAsync(Id);
			if (!string.IsNullOrWhiteSpace(d))
			{
				d = Obfuscator.DeObfuscate(d);
				//KeysAndValues = Deserialize(d);
				KeysAndValues = Serializer.Deserialize<ConcurrentDictionary<string, object>>(d);
			}
			else
				KeysAndValues?.Clear();
		}

		public override async Task SaveAsync(string changedValueKey = null)
		{
			//string d = Serialize();
			string d = Serializer.Serialize(KeysAndValues);
			if (d != null)
			{
				d = Obfuscator.Obfuscate(d);
				await _SetStringAsync(Id, d);
			}
		}
	}
}
