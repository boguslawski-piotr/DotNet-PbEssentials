using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace pbXNet
{
	public class SettingsInStorage : Settings
	{
		public string Id { get; private set; }

		protected readonly IStorage<string> Storage;

		protected readonly ISerializer Serializer;

		public SettingsInStorage(string id, IStorage<string> storage, ISerializer serializer = null)
		{
			Id = id ?? throw new ArgumentNullException(nameof(id));
			Storage = storage ?? throw new ArgumentNullException(nameof(storage));
			Serializer = serializer ?? new NewtonsoftJsonSerializer();
		}

		public static async Task<SettingsInStorage> NewAsync(string id, IStorage<string> storage, ISerializer serializer = null)
		{
			SettingsInStorage s = new SettingsInStorage(id, storage, serializer);
			await s.LoadAsync();
			return s;
		}

		public override async Task LoadAsync()
		{
			try
			{
				string d = await Storage.GetACopyAsync(Id);
				if (!string.IsNullOrWhiteSpace(d))
					KeysAndValues = Serializer.Deserialize<ConcurrentDictionary<string, object>>(d);
				else
					KeysAndValues?.Clear();
			}
			catch (StorageThingNotFoundException) { }
		}

		public override async Task SaveAsync(string changedValueKey = null)
		{
			string d = Serializer.Serialize(KeysAndValues);
			if (d != null)
				await Storage.StoreAsync(Id, d, DateTime.Now);
		}
	}
}
