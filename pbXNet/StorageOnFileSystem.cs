using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace pbXNet
{
    public class StorageOnFileSystem<T> : ISearchableStorage<T> where T : class
    {
        public string Id { get; private set; }

        public virtual string Name => Fs.Name;

        protected IFileSystem Fs;

        public StorageOnFileSystem(string id, IFileSystem fs)
        {
            Id = id;
            Fs = fs;
        }

		/// <summary>
		/// Initializes internally this instance.
		/// It is designed to be called (from user code) 
		/// right after constructor but asynchronously.
		/// </summary>
		public virtual async Task InitializeAsync()
        {
            await Fs.SetCurrentDirectoryAsync(null);
            await Fs.CreateDirectoryAsync(Id); // TODO: opoznic tworzenie katalogu do momentu gdy cos naprawde sie dzieje (zapis, odczyt, itp.)
        }

        public virtual async Task<IEnumerable<string>> FindIdsAsync(string pattern)
        {
            return await Fs.GetFilesAsync(pattern);
        }

        const string ModifiedOnSeparator = "@";

        public virtual async Task StoreAsync(string id, T data, DateTime modifiedOn)
        {
            string d;
            if (typeof(T).Equals(typeof(string)))
                d = data.ToString();
            else
                d = JsonConvert.SerializeObject(data, pbXNet.Settings.JsonSerializer);

            string mod = JsonConvert.SerializeObject(modifiedOn, pbXNet.Settings.JsonSerializer);

            await Fs.WriteTextAsync(id, mod + ModifiedOnSeparator + d);
		}
		
        public virtual async Task<DateTime> GetModifiedOnAsync(string id)
		{
            if (!await ExistsAsync(id))
                return DateTime.MinValue;

			string sd = await Fs.ReadTextAsync(id);
			sd = sd.Substring(0, sd.IndexOf(ModifiedOnSeparator, StringComparison.Ordinal));

            return JsonConvert.DeserializeObject<DateTime>(sd, pbXNet.Settings.JsonSerializer);
		}

		public virtual async Task<T> GetACopyAsync(string id)
		{
			if (!await ExistsAsync(id))
                return null;
			
            // Get data from file system and skip saved modification date (is not needed here)
			string sd = await Fs.ReadTextAsync(id);
            sd = sd.Substring(sd.IndexOf(ModifiedOnSeparator, StringComparison.Ordinal) + ModifiedOnSeparator.Length);

            // Restore object
            object d = sd;
            if (typeof(T).Equals(typeof(string)))
                return (T)d;
            else
                return JsonConvert.DeserializeObject<T>((string)d, pbXNet.Settings.JsonSerializer);
		}
		
        public virtual async Task<bool> ExistsAsync(string id)
        {
            return await Fs.FileExistsAsync(id);
        }

        public virtual async Task<T> RetrieveAsync(string id)
        {
			T data = await GetACopyAsync(id);
            if(data != null)
			    await DiscardAsync(id);
			return data;
		}

        public virtual async Task DiscardAsync(string id)
        {
            await Fs.DeleteFileAsync(id);
        }

    }

}
