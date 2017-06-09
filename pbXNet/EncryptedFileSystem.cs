using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace pbXNet
{
	public class EncryptedFileSystem : IFileSystem
	{
		public FileSystemType Type => _fs.Type;

		public string Id { get; private set; }

		public string Name => _fs.Name;

		IFileSystem _fs;
		ICryptographer _cryptographer;

		byte[] _pwd;
		byte[] _ckey;
		byte[] _iv;

		public enum CKeyType
		{
			Password,
			CKey,
		}

		/// Id will be used as directory name in the root of file system. 
		/// The data in this directory (and in all subdirectories) will be encrypted. 
		/// The data in another directories (unless the Id was set to null or empty string) will not be encrypted.
		/// 
		/// IMPORTANT NOTE: 
		/// Passed pwd or ckey is / can be completely cleaned (zeros) as soon as possible / at an unspecified moment.
		/// You should not use this data anymore after it was passed to the EncryptedFileSystem class constructor.
		public EncryptedFileSystem(string id, IFileSystem fs, ICryptographer cryptographer, CKeyType cKeyType, byte[] ckey)
		{
			Id = id;
			if (cKeyType == CKeyType.Password)
				_pwd = ckey;
			else
				_ckey = ckey;
			_fs = fs;
			_cryptographer = cryptographer;
		}

		public async Task InitializeAsync()
		{
			if (_ckey != null && _iv != null)
				return;

			const string _configFileName = ".929b653c172a4002b3072e4f5dacf955";
			string d;

			await _fs.SaveStateAsync();

			await _fs.SetCurrentDirectoryAsync(null);
			await _fs.CreateDirectoryAsync(Id);

			if (await _fs.FileExistsAsync(_configFileName))
			{
				d = await _fs.ReadTextAsync(_configFileName);
				d = Obfuscator.DeObfuscate(d);
				_iv = d.FromHexString();
			}
			else
			{
				_iv = _cryptographer.GenerateIV();
				d = ConvertEx.ToHexString(_iv);
				d = Obfuscator.Obfuscate(d);
				await _fs.WriteTextAsync(_configFileName, d);
			}

			if (_ckey == null)
				_ckey = _cryptographer.GenerateKey(_pwd, _iv);

			_pwd?.FillWithDefault();

			await _fs.RestoreStateAsync();
		}

		public void Dispose()
		{
			_pwd?.FillWithDefault();
			_ckey?.FillWithDefault();
			_iv?.FillWithDefault();
			_pwd = _ckey = _iv = null;
			_fs = null;
			_cryptographer = null;
		}

		public async Task<IFileSystem> MakeCopyAsync()
		{
			throw new NotSupportedException();
		}

		public virtual async Task SaveStateAsync()
		{
			await _fs.SaveStateAsync();
		}

		public virtual async Task RestoreStateAsync()
		{
			await _fs.RestoreStateAsync();
		}

		public async Task SetCurrentDirectoryAsync(string dirname)
		{
			await _fs.SetCurrentDirectoryAsync(dirname);
		}

		public async Task<bool> DirectoryExistsAsync(string dirname)
		{
			return await _fs.DirectoryExistsAsync(dirname);
		}

		public async Task<bool> FileExistsAsync(string filename)
		{
			return await _fs.FileExistsAsync(filename);
		}

		public async Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "")
		{
			return await _fs.GetDirectoriesAsync(pattern);
		}

		public async Task<IEnumerable<string>> GetFilesAsync(string pattern = "")
		{
			return await _fs.GetFilesAsync(pattern);
		}

		public async Task CreateDirectoryAsync(string dirname)
		{
			await _fs.CreateDirectoryAsync(dirname);
		}

		public async Task DeleteDirectoryAsync(string dirname)
		{
			await _fs.DeleteDirectoryAsync(dirname);
		}

		public async Task DeleteFileAsync(string filename)
		{
			await _fs.DeleteFileAsync(filename);
		}

		public async Task<string> ReadTextAsync(string filename)
		{
			// TODO: dodac sprawdzanie czy jestesmy poza katalogiem o nazwie Id i jezeli tak, to nie odszyfrowywac
			string text = await _fs.ReadTextAsync(filename);

			if (_ckey == null || _iv == null)
				await InitializeAsync();

			byte[] dtext = _cryptographer.Decrypt(text.FromHexString(), _ckey, _iv);
			return Encoding.UTF8.GetString(dtext, 0, dtext.Length);
		}

		public async Task WriteTextAsync(string filename, string text)
		{
			// TODO: dodac sprawdzanie czy jestesmy poza katalogiem o nazwie Id i jezeli tak, to nie szyfrowac

			if (_ckey == null || _iv == null)
				await InitializeAsync();

			byte[] etext = _cryptographer.Encrypt(Encoding.UTF8.GetBytes(text), _ckey, _iv);
			await _fs.WriteTextAsync(filename, ConvertEx.ToHexString(etext));
		}
	}
}
