using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace pbXNet
{
    public class StorageOnFileSystem<T> : ISearchableStorage<T>
    {
        public string Id { get; private set; }

        public string Name => Fs.Name;

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
		/// For example:
		/// <example>
		/// <code>IStorage s = await new StorageOnFileSystem().Initialize();</code>
		/// </example>
		/// </summary>
		public async Task InitializeAsync()
        {
            await Fs.SetCurrentDirectoryAsync(null);
            await Fs.CreateDirectoryAsync(Id);
        }

        public Task<IEnumerable<string>> FindIdsAsync(string pattern)
        {
            return Fs.GetFilesAsync(pattern);
        }

        public async Task StoreAsync(string id, T data)
        {
            string d;
            if (typeof(T).Equals(typeof(string)))
                d = data.ToString();
            else
                d = JsonConvert.SerializeObject(data, pbXNet.Settings.JsonSerializer);

			await Fs.WriteTextAsync(id, d);
		}

		public async Task<T> GetACopyAsync(string id)
		{
            object d = await Fs.ReadTextAsync(id);
            if (typeof(T).Equals(typeof(string)))
                return (T)d;
            else
                return JsonConvert.DeserializeObject<T>((string)d, pbXNet.Settings.JsonSerializer);
		}
		
        public Task<bool> ExistsAsync(string id)
        {
            return Fs.FileExistsAsync(id);
        }

        public async Task<T> RetrieveAsync(string id)
        {
			T data = await GetACopyAsync(id);
			await DiscardAsync(id);
			return data;
		}

        public async Task DiscardAsync(string id)
        {
            await Fs.DeleteFileAsync(id);
        }

	}

}
