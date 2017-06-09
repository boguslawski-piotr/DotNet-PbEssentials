using System;

//
// Based on book Creating Mobile Apps with Xamarin.Forms by Charles Petzold

namespace pbXNet
{
	public enum DeviceFileSystemRoot
	{
		Personal,
		Documents,
		Desktop,
		Config,
		External, // SDCard, USB drive, etc.
	}

	// TODO: implement support for DeviceFileSystemRoot.External 

	public partial class DeviceFileSystem : IFileSystem, IDisposable
	{
		public FileSystemType Type { get; } = FileSystemType.Local;

		public DeviceFileSystemRoot Root { get; }

		public string Id { get; } = Tools.CreateGuid();

		public string Name
		{
			get {
				switch (Root)
				{
					case DeviceFileSystemRoot.Personal:
#if __MACOS__ || WINDOWS_UWP
                        return T.Localized("DeviceFileSystem.Root.Personal.Desktop");
#else
						return T.Localized("DeviceFileSystem.Root.Personal");
#endif
					case DeviceFileSystemRoot.Documents:
						return T.Localized("DeviceFileSystem.Root.Documents");
					case DeviceFileSystemRoot.Desktop:
						return T.Localized("DeviceFileSystem.Root.Desktop");
					case DeviceFileSystemRoot.Config:
						return T.Localized("DeviceFileSystem.Root.Config");
					case DeviceFileSystemRoot.External:
						return T.Localized("DeviceFileSystem.Root.External");
					default:
						return $"{Root.ToString()} {T.Localized("folder")}";

				}
			}
		}

		public DeviceFileSystem(DeviceFileSystemRoot root = DeviceFileSystemRoot.Personal)
		{
			Root = root;
			Initialize();
		}

		// You will find the rest of implementation in the platform directories...

	}
}
