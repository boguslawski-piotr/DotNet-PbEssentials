#if !WINDOWS_UWP

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace pbXNet
{
    public partial class DeviceFileSystem : IFileSystem, IDisposable
    {
        string _root;
        string _current;
        Stack<string> _previous = new Stack<string>();

        protected void Initialize(string dirname = null, DeviceFileSystemRoot root = DeviceFileSystemRoot.Documents)
        {
            if (root == DeviceFileSystemRoot.Documents)
                _root = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            else
                _root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            SetCurrentDirectoryAsync(dirname);
        }

        public void Dispose()
        {
            _previous.Clear();
            _root = _current = null;
        }

        public Task<IFileSystem> MakeCopyAsync()
        {
            DeviceFileSystem fs = new DeviceFileSystem()
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

        public Task<IEnumerable<string>> GetDirectoriesAsync()
        {
            IEnumerable<string> dirnames =
                from filepath in Directory.EnumerateDirectories(_current)
                select Path.GetFileName(filepath);
            return Task.FromResult(dirnames);
        }

        public Task<IEnumerable<string>> GetFilesAsync()
        {
            IEnumerable<string> filenames =
                from filepath in Directory.EnumerateFiles(_current)
                select Path.GetFileName(filepath);
            return Task.FromResult(filenames);
        }

        public Task CreateDirectoryAsync(string dirname)
        {
            string dirpath = GetFilePath(dirname);
            DirectoryInfo dir = Directory.CreateDirectory(GetFilePath(dirpath));
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