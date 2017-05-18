using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//
// Based on book Creating Mobile Apps with Xamarin.Forms by Charles Petzold
//

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

        /// <summary>
        /// Should be super, super fast!
        /// </summary>
        Task<IFileSystem> MakeCopyAsync();

        /// <summary>
        /// Should handle: null == root, .. == one level back.
        /// </summary>
        Task SetCurrentDirectoryAsync(string dirname);

        // All below should work in current directory.

        Task<bool> DirectoryExistsAsync(string dirname);
        Task<bool> FileExistsAsync(string filename);

        Task<IEnumerable<string>> GetDirectoriesAsync();
        Task<IEnumerable<string>> GetFilesAsync();

        /// <summary>
        /// Should set current directory to one created.
        /// </summary>
        Task CreateDirectoryAsync(string dirname);

        Task DeleteDirectoryAsync(string dirname);
        Task DeleteFileAsync(string filename);
	
        Task WriteTextAsync(string filename, string text);
		Task<string> ReadTextAsync(string filename);
	}
}
