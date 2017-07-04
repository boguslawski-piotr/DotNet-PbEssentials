using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace pbXNet
{
	/// <summary>
	/// Decorator for classes that implement the <see cref="IFileSystem"/> interface 
	/// which provides encryption of stored files and of course decrypting when reading.
	/// <para>Id passed to constructor will be used as directory name in the root of file system. 
	/// The data in this directory (and in all subdirectories) will be encrypted. 
	/// The data in another directories (unless the Id was set to null or empty string) will not be encrypted.
	/// </para>
	/// </summary>
	public class EncryptedFileSystem : IFileSystem, IDisposable
	{
		public FileSystemType Type => _fs.Type;

		public string Id { get; private set; }

		public string Name => _fs.Name;

		public string RootPath => _fs.RootPath;

		public string CurrentPath => _fs.CurrentPath;

		ICryptographer _cryptographer;

		IFileSystem _fs;

		IPassword _passwd;
		IByteBuffer _ckey;
		IByteBuffer _iv;

		protected EncryptedFileSystem(string id, IFileSystem fs, ICryptographer cryptographer = null)
		{
			if (id == null || fs == null)
				throw new ArgumentException($"{nameof(id)} and {nameof(fs)} must be valid objects.");

			Id = id;
			_fs = fs;
			_cryptographer = cryptographer ?? new AesCryptographer();
		}

		/// <summary>
		/// IMPORTANT NOTE: 
		/// Passed ckey can be completely cleaned (zeros) at an unspecified moment.
		/// You should not use this data anymore after it was passed to the EncryptedFileSystem class constructor.
		/// </summary>
		public EncryptedFileSystem(string id, IFileSystem fs, IByteBuffer ckey, ICryptographer cryptographer = null)
			: this(id, fs, cryptographer)
		{
			if (ckey == null)
				throw new ArgumentException($"{nameof(ckey)} must be valid object.");

			_ckey = ckey;
		}

		/// <summary>
		/// IMPORTANT NOTE: 
		/// Passed passwd is completely cleaned (zeros) as soon as possible.
		/// You should not use this data anymore after it was passed to the EncryptedFileSystem class constructor.
		/// </summary>
		public EncryptedFileSystem(string id, IFileSystem fs, IPassword passwd, ICryptographer cryptographer = null)
			: this(id, fs, cryptographer)
		{
			if (passwd == null)
				throw new ArgumentException($"{nameof(passwd)} must be valid object.");

			_passwd = passwd;
		}

		public void Initialize()
		{ }

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
				if (_iv as SecureBuffer == null)
					_iv = new SecureBuffer((IByteBuffer)_iv, true);
				await _fs.WriteTextAsync(_configFileName, _iv.ToHexString()).ConfigureAwait(false);
			}

			if (_ckey == null)
			{
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

		public Task<IFileSystem> CloneAsync()
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

		bool InEncryptedDirectory()
		{
			string currentPath = _fs.CurrentPath.Replace(_fs.RootPath, "");
			if (currentPath.StartsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
				currentPath = currentPath.Substring(1);
			return currentPath.StartsWith(Id);
		}

		public virtual async Task<string> ReadTextAsync(string filename)
		{
			string text = await _fs.ReadTextAsync(filename).ConfigureAwait(false);

			if (!InEncryptedDirectory())
				return text;

			if (_ckey == null || _iv == null)
				await InitializeAsync();

			ByteBuffer dtext = _cryptographer.Decrypt(ByteBuffer.NewFromHexString(text), _ckey, _iv);
			return dtext.ToString(Encoding.UTF8);
		}

		public virtual async Task WriteTextAsync(string filename, string text)
		{
			if (!InEncryptedDirectory())
				await _fs.WriteTextAsync(filename, text);
			else
			{
				if (_ckey == null || _iv == null)
					await InitializeAsync();

				ByteBuffer etext = _cryptographer.Encrypt(new ByteBuffer(text, Encoding.UTF8), _ckey, _iv);
				await _fs.WriteTextAsync(filename, etext.ToHexString()).ConfigureAwait(false);
			}
		}
	}
}
