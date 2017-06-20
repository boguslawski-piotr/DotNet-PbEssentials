using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace pbXNet
{
	/// Id will be used as directory name in the root of file system. 
	/// The data in this directory (and in all subdirectories) will be encrypted. 
	/// The data in another directories (unless the Id was set to null or empty string) will not be encrypted.
	public class EncryptedFileSystem : IFileSystem, IDisposable
	{
		public FileSystemType Type => _fs.Type;

		public string Id { get; private set; }

		public string Name => _fs.Name;

		ICryptographer _cryptographer = new AesCryptographer();

		IFileSystem _fs;

		IPassword _passwd;
		IByteBuffer _ckey;
		IByteBuffer _iv;

		protected EncryptedFileSystem(string id, IFileSystem fs)
		{
			Id = id;
			_fs = fs;
		}

		/// IMPORTANT NOTE: 
		/// Passed ckey can be completely cleaned (zeros) at an unspecified moment.
		/// You should not use this data anymore after it was passed to the EncryptedFileSystem class constructor.
		public EncryptedFileSystem(string id, IFileSystem fs, IByteBuffer ckey) : this(id, fs)
		{
			_ckey = ckey;
		}

		/// IMPORTANT NOTE: 
		/// Passed passwd is completely cleaned (zeros) as soon as possible.
		/// You should not use this data anymore after it was passed to the EncryptedFileSystem class constructor.
		public EncryptedFileSystem(string id, IFileSystem fs, IPassword passwd) : this(id, fs)
		{
			_passwd = passwd;
		}

		public virtual async Task InitializeAsync()
		{
			if (_ckey != null && _iv != null)
				return;

			await _fs.SaveStateAsync().ConfigureAwait(false);

			await _fs.SetCurrentDirectoryAsync(null).ConfigureAwait(false);
			await _fs.CreateDirectoryAsync(Id).ConfigureAwait(false);

			const string _configFileName = ".929b653c172a4002b3072e4f5dacf955";

			if (await _fs.FileExistsAsync(_configFileName).ConfigureAwait(false))
			{
				_iv = SecureBuffer.NewFromHexString(await _fs.ReadTextAsync(_configFileName).ConfigureAwait(false));
			}
			else
			{
				_iv = _cryptographer.GenerateIV();
				await _fs.WriteTextAsync(_configFileName, _iv.ToHexString()).ConfigureAwait(false);
			}

			if (_ckey == null)
			{
				if (_passwd == null)
					throw new ArgumentNullException(nameof(_passwd));
				_ckey = _cryptographer.GenerateKey(_passwd, _iv);
			}

			_passwd?.Dispose();

			await _fs.RestoreStateAsync().ConfigureAwait(false);
		}

		public void Dispose()
		{
			_passwd?.Dispose();
			_ckey?.Dispose();
			_iv?.Dispose();
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

		public async Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "") => await _fs.GetDirectoriesAsync(pattern).ConfigureAwait(false);

		public async Task<bool> DirectoryExistsAsync(string dirname) => await _fs.DirectoryExistsAsync(dirname).ConfigureAwait(false);

		public async Task CreateDirectoryAsync(string dirname) => await _fs.CreateDirectoryAsync(dirname).ConfigureAwait(false);

		public async Task DeleteDirectoryAsync(string dirname) => await _fs.DeleteDirectoryAsync(dirname).ConfigureAwait(false);


		public async Task<IEnumerable<string>> GetFilesAsync(string pattern = "") => await _fs.GetFilesAsync(pattern).ConfigureAwait(false);

		public async Task<bool> FileExistsAsync(string filename) => await _fs.FileExistsAsync(filename).ConfigureAwait(false);

		public async Task DeleteFileAsync(string filename) => await _fs.DeleteFileAsync(filename).ConfigureAwait(false);

		public async Task SetFileModifiedOnAsync(string filename, DateTime modifiedOn) => await _fs.SetFileModifiedOnAsync(filename, modifiedOn).ConfigureAwait(false);

		public async Task<DateTime> GetFileModifiedOnAsync(string filename) => await _fs.GetFileModifiedOnAsync(filename).ConfigureAwait(false);

		public virtual async Task<string> ReadTextAsync(string filename)
		{
			// TODO: dodac sprawdzanie czy jestesmy poza katalogiem o nazwie Id i jezeli tak, to nie odszyfrowywac
			string text = await _fs.ReadTextAsync(filename).ConfigureAwait(false);

			if (_ckey == null || _iv == null)
				await InitializeAsync();

			ByteBuffer dtext = _cryptographer.Decrypt(ByteBuffer.NewFromHexString(text), _ckey, _iv);
			return dtext.ToString(Encoding.UTF8);
		}

		public virtual async Task WriteTextAsync(string filename, string text)
		{
			// TODO: dodac sprawdzanie czy jestesmy poza katalogiem o nazwie Id i jezeli tak, to nie szyfrowac

			if (_ckey == null || _iv == null)
				await InitializeAsync();

			ByteBuffer etext = _cryptographer.Encrypt(new ByteBuffer(text, Encoding.UTF8), _ckey, _iv);
			await _fs.WriteTextAsync(filename, etext.ToHexString()).ConfigureAwait(false);
		}
	}
}
