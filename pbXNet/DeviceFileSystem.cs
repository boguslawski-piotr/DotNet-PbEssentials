using System;
using System.Collections.Generic;
using System.Threading.Tasks;

//
// Based on book Creating Mobile Apps with Xamarin.Forms by Charles Petzold

namespace pbXNet
{
    public enum DeviceFileSystemRoot
    {
        Personal,
        Desktop,
        Shared,
        Config,
        Roaming,
        External, // SDCard, USB drive, etc.
    }

    // TODO: implement support for DeviceFileSystemRoot.External 

    public partial class DeviceFileSystem : IFileSystem, IDisposable
    {
        public FileSystemType Type { get; } = FileSystemType.Local;

        public DeviceFileSystemRoot Root { get; }

        public string Name
        {
            get {
                // TODO: DeviceFileSystem.Name: jak to rozwiazac aby nie uzywac lokalizowanych tekstow?
                return $"{Root.ToString()} folder";
            }
        }

        public DeviceFileSystem(DeviceFileSystemRoot root = DeviceFileSystemRoot.Personal)
        {
            Root = root;
            Initialize(null);
        }

        public DeviceFileSystem(string dirname, DeviceFileSystemRoot root = DeviceFileSystemRoot.Personal)
        {
			Root = root;
			Initialize(dirname);
        }

        // You will find the rest of implementation in the platform directories...

    }
}
