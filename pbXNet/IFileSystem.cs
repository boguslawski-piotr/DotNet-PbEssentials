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
        Roaming,
        Remote,
    }

    /// <summary>
    /// IMPLEMENTATION TIPS: It is not required for newly created objects to set correct 'CurrentDirectory'. The same applies to calls to 'SetType'.
    /// </summary>
    public interface IFileSystem : IDisposable
    {
        string Name { get; }
        string Desc { get; }

        IList<FileSystemType> SupportedTypes();

        /// <summary>
        /// Should be super fast!
        /// </summary>
        void SetType(FileSystemType type);

        /// <summary>
        /// Should be super, super fast!
        /// </summary>
        Task<IFileSystem> MakeCopy();

        /// <summary>
        /// Should handle: null == root, .. == one level back.
        /// </summary>
        Task SetCurrentDirectory(string dirname);

        // All below should work in current directory.

        Task<bool> DirectoryExistsAsync(string dirname);
        Task<bool> FileExistsAsync(string filename);

        Task<IEnumerable<string>> GetDirectoriesAsync();
        Task<IEnumerable<string>> GetFilesAsync();

        /// <summary>
        /// Should set current directory to one created.
        /// </summary>
        Task CreateDirectoryAsync(string dirname);

        Task WriteTextAsync(string filename, string text);
        Task<string> ReadTextAsync(string filename);

        Task DeleteDirectoryAsync(string dirname);
        Task DeleteFileAsync(string filename);
    }
}
