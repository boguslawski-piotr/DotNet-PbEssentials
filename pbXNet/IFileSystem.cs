using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Based on book Creating Mobile Apps with Xamarin.Forms by Charles Petzold

namespace pbXNet
{
	public enum FileSystemType
	{
		Local,
		Remote,
	}

	/// <summary>
	/// </summary>
	public interface IFileSystem : IDisposable
	{
		FileSystemType Type { get; }

		string Id { get; }

		string Name { get; }

		string RootPath { get; }

		string CurrentPath { get; }

		/// <summary>
		/// Should be super, super fast!
		/// </summary>
		Task<IFileSystem> CloneAsync();

		Task SaveStateAsync();
		Task RestoreStateAsync();

		/// <summary>
		/// Should handle: null == root, .. == one level back.
		/// </summary>
		Task SetCurrentDirectoryAsync(string dirname);

		// All below should work in current directory.

		Task<IEnumerable<string>> GetDirectoriesAsync(string pattern = "");

		Task<bool> DirectoryExistsAsync(string dirname);

		/// <summary>
		/// Should set current directory to one created.
		/// </summary>
		Task CreateDirectoryAsync(string dirname);

		Task DeleteDirectoryAsync(string dirname);


		Task<IEnumerable<string>> GetFilesAsync(string pattern = "");

		Task<bool> FileExistsAsync(string filename);

		Task DeleteFileAsync(string filename);

		/// Should set UTC date/time for a file.
		Task SetFileModifiedOnAsync(string filename, DateTime modifiedOn);

		/// Gets UTC modified date/time.
		Task<DateTime> GetFileModifiedOnAsync(string filename);

		Task WriteTextAsync(string filename, string text);

		Task<string> ReadTextAsync(string filename);
	}
}
