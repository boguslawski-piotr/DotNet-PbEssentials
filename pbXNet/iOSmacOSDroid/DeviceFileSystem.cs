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
#if __MACOS__
            DeviceFileSystemRoot.Documents,
            DeviceFileSystemRoot.Desktop,
#endif
#if DEBUG
            //DeviceFileSystemRoot.Config, // only for testing
#endif
        };

        string _root;
        string _current;
        Stack<string> _previous = new Stack<string>();

        protected virtual void Initialize(string dirname = null)
        {
            switch (Root)
            {
                case DeviceFileSystemRoot.Documents:
                    _root = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    _root = Path.Combine(_root, "Documents");
                    break;
                case DeviceFileSystemRoot.Desktop:
                    _root = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    break;
                case DeviceFileSystemRoot.Config:
                    _root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    break;
                default:
                    _root = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    break;
            }

            SetCurrentDirectoryAsync(dirname);
        }

        public virtual void Dispose()
        {
            _previous.Clear();
            _root = _current = null;
        }

        public virtual Task<IFileSystem> MakeCopyAsync()
        {
            DeviceFileSystem fs = new DeviceFileSystem(this.Root)
            {
                _root = this._root,
                _current = this._current,
                _previous = new Stack<string>(_previous.AsEnumerable()),
            };
            return Task.FromResult<IFileSystem>(fs);
        }

        public virtual Task SetCurrentDirectoryAsync(string dirname)
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

        public virtual Task<bool> DirectoryExistsAsync(string dirname)
        {
            string dirpath = GetFilePath(dirname);
            bool exists = Directory.Exists(dirpath);
            return Task.FromResult(exists);
        }

        public virtual Task<bool> FileExistsAsync(string filename)
        {
            string filepath = GetFilePath(filename);
            bool exists = File.Exists(filepath);
            return Task.FromResult(exists);
        }

        public virtual Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "")
        {
            IEnumerable<string> dirnames =
                from dirpath in Directory.EnumerateDirectories(_current)
                let dirname = Path.GetFileName(dirpath)
                where Regex.IsMatch(dirname, pattern)
                select dirname;
            return Task.FromResult(dirnames);
        }

        public virtual Task<IEnumerable<string>> GetFilesAsync(string pattern = "")
        {
            IEnumerable<string> filenames =
                from filepath in Directory.EnumerateFiles(_current)
                let filename = Path.GetFileName(filepath)
                where Regex.IsMatch(filename, pattern)
                select filename;
            return Task.FromResult(filenames);
        }

        public virtual Task CreateDirectoryAsync(string dirname)
        {
            string dirpath = GetFilePath(dirname);
            DirectoryInfo dir = Directory.CreateDirectory(GetFilePath(dirpath));
            _previous.Push(_current);
            _current = dirpath;
            return Task.FromResult(true);
        }

        public virtual Task DeleteDirectoryAsync(string dirname)
        {
            Directory.Delete(GetFilePath(dirname));
            return Task.FromResult(true);
        }

        public virtual Task DeleteFileAsync(string filename)
        {
            File.Delete(GetFilePath(filename));
            return Task.FromResult(true);
        }


        public virtual async Task WriteTextAsync(string filename, string text)
        {
            string filepath = GetFilePath(filename);
            using (StreamWriter writer = File.CreateText(filepath))
            {
                await writer.WriteAsync(text);
            }
        }

        public virtual async Task<string> ReadTextAsync(string filename)
        {
            string filepath = GetFilePath(filename);
            using (StreamReader reader = File.OpenText(filepath))
            {
                return await reader.ReadToEndAsync();
            }
        }

        protected string GetFilePath(string filename)
        {
            return Path.Combine(_current, filename);
        }
    }
}

#endif