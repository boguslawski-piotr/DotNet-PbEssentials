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

        public static async Task<StorageOnFileSystem<T>> NewAsync(string id, IFileSystem fs)
        {
            StorageOnFileSystem<T> o = new StorageOnFileSystem<T>(id, fs);
            await o.InitializeAsync();
            return o;
        }

        public virtual async Task InitializeAsync()
        {
            await Fs.SetCurrentDirectoryAsync(null);
        }

        protected async Task<IFileSystem> GetFsAsync()
        {
            await Fs.SetCurrentDirectoryAsync(null);
            await Fs.CreateDirectoryAsync(Id);
            return Fs;
        }

        const string ModifiedOnSeparator = "@";

        public virtual async Task StoreAsync(string thingId, T data, DateTime modifiedOn)
        {
            string d;
            if (typeof(T).Equals(typeof(string)))
                d = data.ToString();
            else
                d = JsonConvert.SerializeObject(data, pbXNet.Settings.JsonSerializer);

            string mod = JsonConvert.SerializeObject(modifiedOn, pbXNet.Settings.JsonSerializer);

            IFileSystem fs = await GetFsAsync();
            await fs.WriteTextAsync(thingId, mod + ModifiedOnSeparator + d);
        }

        public virtual async Task<bool> ExistsAsync(string thingId)
        {
            // We do not use GetFsAsync() in order to delay directory creation as much as possible.

            await Fs.SetCurrentDirectoryAsync(null);
            if (!await Fs.DirectoryExistsAsync(Id))
                return false;

            await Fs.CreateDirectoryAsync(Id);
            return await Fs.FileExistsAsync(thingId);
        }

        public virtual async Task<DateTime> GetModifiedOnAsync(string thingId)
        {
            if (!await ExistsAsync(thingId))
                return DateTime.MinValue;

            IFileSystem fs = await GetFsAsync();
            string sd = await fs.ReadTextAsync(thingId);
            sd = sd.Substring(0, sd.IndexOf(ModifiedOnSeparator, StringComparison.Ordinal));

            return JsonConvert.DeserializeObject<DateTime>(sd, pbXNet.Settings.JsonSerializer);
        }

        public virtual async Task<T> GetACopyAsync(string thingId)
        {
            if (!await ExistsAsync(thingId))
                return null;

            // Get data from file system and skip saved modification date (is not needed here)
            IFileSystem fs = await GetFsAsync();
            string sd = await fs.ReadTextAsync(thingId);
            sd = sd.Substring(sd.IndexOf(ModifiedOnSeparator, StringComparison.Ordinal) + ModifiedOnSeparator.Length);

            // Restore object
            object d = sd;
            if (typeof(T).Equals(typeof(string)))
                return (T)d;
            else
                return JsonConvert.DeserializeObject<T>((string)d, pbXNet.Settings.JsonSerializer);
        }

        public virtual async Task<T> RetrieveAsync(string thingId)
        {
            T data = await GetACopyAsync(thingId);
            if (data != null)
                await DiscardAsync(thingId);
            return data;
        }

        public virtual async Task DiscardAsync(string thingId)
        {
            IFileSystem fs = await GetFsAsync();
            await fs.DeleteFileAsync(thingId);
        }


        //

        public virtual async Task<IEnumerable<string>> FindIdsAsync(string pattern)
        {
            // We do not use GetFsAsync() in order to delay directory creation as much as possible.

            await Fs.SetCurrentDirectoryAsync(null);
            if (!await Fs.DirectoryExistsAsync(Id))
                return null;

            await Fs.CreateDirectoryAsync(Id);
            return await Fs.GetFilesAsync(pattern);
        }
    }
}
