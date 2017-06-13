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

		Password _passwd;
		byte[] _ckey;
		byte[] _iv;

		/// Id will be used as directory name in the root of file system. 
		/// The data in this directory (and in all subdirectories) will be encrypted. 
		/// The data in another directories (unless the Id was set to null or empty string) will not be encrypted.
		/// 
		/// IMPORTANT NOTE: 
		/// Passed pwd or ckey is / can be completely cleaned (zeros) as soon as possible / at an unspecified moment.
		/// You should not use this data anymore after it was passed to the EncryptedFileSystem class constructor.
		public EncryptedFileSystem(string id, IFileSystem fs, ICryptographer cryptographer, byte[] ckey)
		{
			Id = id;
			_ckey = ckey;
			_fs = fs;
			_cryptographer = cryptographer;
		}

		public EncryptedFileSystem(string id, IFileSystem fs, ICryptographer cryptographer, Password passwd)
		{
			Id = id;
			_passwd = passwd;
			_fs = fs;
			_cryptographer = cryptographer;
		}

		public virtual async Task InitializeAsync()
		{
			if (_ckey != null && _iv != null)
				return;

			const string _configFileName = ".929b653c172a4002b3072e4f5dacf955";
			string d;

			await _fs.SaveStateAsync().ConfigureAwait(false);

			await _fs.SetCurrentDirectoryAsync(null).ConfigureAwait(false);
			await _fs.CreateDirectoryAsync(Id).ConfigureAwait(false);

			if (await _fs.FileExistsAsync(_configFileName).ConfigureAwait(false))
			{
				d = await _fs.ReadTextAsync(_configFileName).ConfigureAwait(false);
				d = Obfuscator.DeObfuscate(d);
				_iv = d.FromHexString();
			}
			else
			{
				_iv = _cryptographer.GenerateIV();
				d = ConvertEx.ToHexString(_iv);
				d = Obfuscator.Obfuscate(d);
				await _fs.WriteTextAsync(_configFileName, d).ConfigureAwait(false);
			}

			if (_ckey == null)
				_ckey = _cryptographer.GenerateKey(_passwd, _iv);

			_passwd?.Clear(true);

			await _fs.RestoreStateAsync().ConfigureAwait(false);
		}

		public void Dispose()
		{
			_passwd?.Clear(true);
			_ckey?.FillWithDefault();
			_iv?.FillWithDefault();
			_passwd = null;
			_ckey = _iv = null;
			_fs = null;
			_cryptographer = null;
		}

		public Task<IFileSystem> MakeCopyAsync()
		{
			throw new NotSupportedException();
		}

		public async Task SaveStateAsync() => await _fs.SaveStateAsync().ConfigureAwait(false);

		public async Task RestoreStateAsync() => await _fs.RestoreStateAsync().ConfigureAwait(false);

		public async Task SetCurrentDirectoryAsync(string dirname) => await _fs.SetCurrentDirectoryAsync(dirname).ConfigureAwait(false);

		public async Task<bool> DirectoryExistsAsync(string dirname) => await _fs.DirectoryExistsAsync(dirname).ConfigureAwait(false);

		public async Task<bool> FileExistsAsync(string filename) => await _fs.FileExistsAsync(filename).ConfigureAwait(false);

		public async Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "") => await _fs.GetDirectoriesAsync(pattern).ConfigureAwait(false);

		public async Task<IEnumerable<string>> GetFilesAsync(string pattern = "") => await _fs.GetFilesAsync(pattern).ConfigureAwait(false);

		public async Task CreateDirectoryAsync(string dirname) => await _fs.CreateDirectoryAsync(dirname).ConfigureAwait(false);

		public async Task DeleteDirectoryAsync(string dirname) => await _fs.DeleteDirectoryAsync(dirname).ConfigureAwait(false);

		public async Task DeleteFileAsync(string filename) => await _fs.DeleteFileAsync(filename).ConfigureAwait(false);

		public virtual async Task<string> ReadTextAsync(string filename)
		{
			// TODO: dodac sprawdzanie czy jestesmy poza katalogiem o nazwie Id i jezeli tak, to nie odszyfrowywac
			string text = await _fs.ReadTextAsync(filename).ConfigureAwait(false);

			if (_ckey == null || _iv == null)
				await InitializeAsync();

			byte[] dtext = _cryptographer.Decrypt(text.FromHexString(), _ckey, _iv);
			return Encoding.UTF8.GetString(dtext, 0, dtext.Length);
		}

		public virtual async Task WriteTextAsync(string filename, string text)
		{
			// TODO: dodac sprawdzanie czy jestesmy poza katalogiem o nazwie Id i jezeli tak, to nie szyfrowac

			if (_ckey == null || _iv == null)
				await InitializeAsync();

			byte[] etext = _cryptographer.Encrypt(Encoding.UTF8.GetBytes(text), _ckey, _iv);
			await _fs.WriteTextAsync(filename, ConvertEx.ToHexString(etext)).ConfigureAwait(false);
		}
	}
}
