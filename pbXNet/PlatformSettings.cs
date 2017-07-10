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
			Serializer = serializer ?? new NewtonsoftJsonSerializer();
		}

		public static async Task<PlatformSettings> NewAsync(string id, ISerializer serializer = null)
		{
			PlatformSettings s = new PlatformSettings(id, serializer);
			await s.LoadAsync();
			return s;
		}

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
