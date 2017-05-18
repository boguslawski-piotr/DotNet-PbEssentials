using System;
using System.Collections.Generic;
using System.Threading.Tasks;

//
// Based on book Creating Mobile Apps with Xamarin.Forms by Charles Petzold
//

namespace pbXNet
{
	public enum DeviceFileSystemRoot
	{
		Documents,
        Config,
        External, // SDCard, USB drive, etc.
	}

	// TODO: implement support for DeviceFileSystemRoot.External 
	
    public partial class DeviceFileSystem : IFileSystem, IDisposable
    {
        public FileSystemType Type { get; } = FileSystemType.Local;
		
        public DeviceFileSystem(DeviceFileSystemRoot root = DeviceFileSystemRoot.Documents)
        {
            Initialize(null, root);
        }

        public DeviceFileSystem(string dirname, DeviceFileSystemRoot root = DeviceFileSystemRoot.Documents)
		{
            Initialize(dirname, root);
		}

		// You will find the rest of implementation in the platform directories...

	}
}
