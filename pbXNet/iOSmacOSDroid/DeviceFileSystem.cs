#if !WINDOWS_UWP

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace pbXNet
{
    public partial class DeviceFileSystem : IFileSystem, IDisposable
    {
        public static readonly IEnumerable<DeviceFileSystemRoot> AvailableRootsForEndUser = new List<DeviceFileSystemRoot>() {
            DeviceFileSystemRoot.Personal,
#if DEBUG
            //DeviceFileSystemRoot.Config, // only for testing
#endif
#if __UNIFIED__ && !__IOS__
            // macOS
            DeviceFileSystemRoot.Desktop,
            DeviceFileSystemRoot.Shared
#endif
#if __ANDROID__
#endif
		};

        string _root;
        string _current;
        Stack<string> _previous = new Stack<string>();

        protected void Initialize(string dirname = null)
        {
            switch (Root)
            {
                case DeviceFileSystemRoot.Personal:
                    _root = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    break;
                case DeviceFileSystemRoot.Desktop:
                    _root = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    break;
                case DeviceFileSystemRoot.Shared:
                    _root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    break;
                case DeviceFileSystemRoot.Config:
                    _root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    break;
                default:
                    // TODO: Obsluzyc reszte (o ile!?) typow Root w DeviceFileSystem
                    _root = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    break;
            }

            SetCurrentDirectoryAsync(dirname);
        }

        public void Dispose()
        {
            _previous.Clear();
            _root = _current = null;
        }

        public Task<IFileSystem> MakeCopyAsync()
        {
            DeviceFileSystem fs = new DeviceFileSystem(this.Root)
            {
                _root = this._root,
                _current = this._current,
                _previous = new Stack<string>(_previous.AsEnumerable()),
            };
            return Task.FromResult<IFileSystem>(fs);
        }

        public Task SetCurrentDirectoryAsync(string dirname)
        {
            if (string.IsNullOrEmpty(dirname))
            {
                _current = _root;
                _previous.Clear();
            }
            else if (dirname == "..")
            {
                _current = _previous.Count > 0 ? _previous.Pop() : _root;
            }
            else
            {
                _previous.Push(_current);
                _current = Path.Combine(_current, dirname);
            }
            return Task.FromResult(true);
        }

        public Task<bool> DirectoryExistsAsync(string dirname)
        {
            string dirpath = GetFilePath(dirname);
            bool exists = Directory.Exists(dirpath);
            return Task.FromResult(exists);
        }

        public Task<bool> FileExistsAsync(string filename)
        {
            string filepath = GetFilePath(filename);
            bool exists = File.Exists(filepath);
            return Task.FromResult(exists);
        }

        public Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "")
        {
            IEnumerable<string> dirnames =
                from dirpath in Directory.EnumerateDirectories(_current)
                let dirname = Path.GetFileName(dirpath)
                where Regex.IsMatch(dirname, pattern)
                select dirname;
            return Task.FromResult(dirnames);
        }

        public Task<IEnumerable<string>> GetFilesAsync(string pattern = "")
        {
            IEnumerable<string> filenames =
                from filepath in Directory.EnumerateFiles(_current)
                let filename = Path.GetFileName(filepath)
                where Regex.IsMatch(filename, pattern)
                select filename;
            return Task.FromResult(filenames);
        }

        public Task CreateDirectoryAsync(string dirname)
        {
            string dirpath = GetFilePath(dirname);
            DirectoryInfo dir = Directory.CreateDirectory(GetFilePath(dirpath));
            _previous.Push(_current);
            _current = dirpath;
            return Task.FromResult(true);
        }

        public Task DeleteDirectoryAsync(string dirname)
        {
            Directory.Delete(GetFilePath(dirname));
            return Task.FromResult(true);
        }

        public Task DeleteFileAsync(string filename)
        {
            File.Delete(GetFilePath(filename));
            return Task.FromResult(true);
        }


        public async Task WriteTextAsync(string filename, string text)
        {
            string filepath = GetFilePath(filename);
            using (StreamWriter writer = File.CreateText(filepath))
            {
                await writer.WriteAsync(text);
            }
        }

        public async Task<string> ReadTextAsync(string filename)
        {
            string filepath = GetFilePath(filename);
            using (StreamReader reader = File.OpenText(filepath))
            {
                return await reader.ReadToEndAsync();
            }
        }

        string GetFilePath(string filename)
        {
            return Path.Combine(_current, filename);
        }

    }
}

#endif