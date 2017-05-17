using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#if WINDOWS_UWP
using Windows.Storage;
#else
#endif

//
// Based on book Creating Mobile Apps with Xamarin.Forms by Charles Petzold
//

namespace pbXNet
{
    public class DeviceFileSystem : IFileSystem, IDisposable
    {
        public string Name
        {
            get
            {
                //return _type == FileSystemType.Local ? Translator.T("DeviceFileSystemLocalDisplayName") : Translator.T("DeviceFileSystemRoamingDisplayName");
                return _type == FileSystemType.Local? "local fs" : "roaming fs";
                
            }
        }

        public string Desc
        {
            get
            {
                //return _type == FileSystemType.Local ? Translator.T("DeviceFileSystemLocalDesc") : Translator.T("DeviceFileSystemRoamingDesc");
                return "";
            }
        }

        private FileSystemType _type = FileSystemType.Local;

        public DeviceFileSystem()
        {
            SetType(FileSystemType.Local);
        }

        public DeviceFileSystem(FileSystemType type)
        {
            SetType(type);
        }

#if WINDOWS_UWP

        public void Dispose()
        {
            _current = null;
            _root = null;
        }

        private StorageFolder _root;
        private StorageFolder _current;

        public IList<FileSystemType> SupportedTypes()
        {
            return new List<FileSystemType>()
            {
                FileSystemType.Local,
                FileSystemType.Roaming,
            };
        }

        public void SetType(FileSystemType type)
        {
            _type = type;
            switch (_type)
            {
                case FileSystemType.Local:
                    _root = ApplicationData.Current.LocalFolder;
                    break;
                case FileSystemType.Roaming:
                    _root = ApplicationData.Current.RoamingFolder;
                    //ulong quota = ApplicationData.Current.RoamingStorageQuota;
                    break;
            }
        }

        async public Task<IFileSystem> MakeCopy()
        {
            DeviceFileSystem cp = new DeviceFileSystem();
            cp.SetType(_type);
            if (_root != null)
                cp._root = await StorageFolder.GetFolderFromPathAsync(_root.Path);
            if (_current != null)
                cp._current = await StorageFolder.GetFolderFromPathAsync(_current.Path);
            return cp;
        }

        async public Task SetCurrentDirectory(string dirname)
        {
            if (dirname == null || dirname == "")
            {
                _current = _root;
            }
            else if (dirname == "..")
            {
                _current = await _current.GetParentAsync();
            }
            else
            {
                _current = await _current.GetFolderAsync(dirname);
            }
        }

        //

        async public Task<bool> DirectoryExistsAsync(string dirname)
        {
            return await _current.TryGetItemAsync(dirname) != null;
        }

        async public Task<bool> FileExistsAsync(string filename)
        {
            return await _current.TryGetItemAsync(filename) != null;
        }

        async public Task<IEnumerable<string>> GetDirectoriesAsync()
        {
            IEnumerable<string> filenames =
                from storageFile in await _current.GetFoldersAsync()
                select storageFile.Name;
            return filenames;
        }

        async public Task<IEnumerable<string>> GetFilesAsync()
        {
            IEnumerable<string> filenames =
                from storageFile in await _current.GetFilesAsync()
                select storageFile.Name;
            return filenames;
        }

        async public Task CreateDirectoryAsync(string dirname)
        {
            _current = await _current.CreateFolderAsync(dirname, CreationCollisionOption.OpenIfExists);
        }

        async public Task WriteTextAsync(string filename, string text)
        {
            IStorageFile storageFile = await _current.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(storageFile, text);
        }

        async public Task<string> ReadTextAsync(string filename)
        {
            IStorageFile storageFile = await _current.GetFileAsync(filename);
            return await FileIO.ReadTextAsync(storageFile);
        }

        public Task DeleteDirectoryAsync(string dirname)
        {
            throw new NotImplementedException();
        }

        public Task DeleteFileAsync(string filename)
        {
            throw new NotImplementedException();
        }

#else
        // TODO: implement DeviceFileSystem

        public void Dispose()
        {
            //throw new NotImplementedException();
        }

        public IList<FileSystemType> SupportedTypes()
        {
            throw new NotImplementedException();
        }

        public void SetType(FileSystemType type)
        {
            //throw new NotImplementedException();
        }

        public Task<IFileSystem> MakeCopy()
        {
            throw new NotImplementedException();
        }

        public Task SetCurrentDirectory(string dirname)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DirectoryExistsAsync(string dirname)
        {
            throw new NotImplementedException();
        }

        public Task<bool> FileExistsAsync(string filename)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetDirectoriesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetFilesAsync()
        {
            throw new NotImplementedException();
        }

        public Task CreateDirectoryAsync(string dirname)
        {
            throw new NotImplementedException();
        }

        public Task WriteTextAsync(string filename, string text)
        {
            throw new NotImplementedException();
        }

        public Task<string> ReadTextAsync(string filename)
        {
            throw new NotImplementedException();
        }

        public Task DeleteDirectoryAsync(string dirname)
        {
            throw new NotImplementedException();
        }

        public Task DeleteFileAsync(string filename)
        {
            throw new NotImplementedException();
        }

#endif
    }
}
