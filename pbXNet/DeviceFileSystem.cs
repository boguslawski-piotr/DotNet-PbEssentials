using System;

// Based on book Creating Mobile Apps with Xamarin.Forms by Charles Petzold

namespace pbXNet
{
	public enum DeviceFileSystemRoot
	{
		Local,
		LocalConfig,
		Roaming,
		RoamingConfig,
		UserDefined,
	}

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
					case DeviceFileSystemRoot.Local:
						return T.Localized("DeviceFileSystem.Root.Local");

					case DeviceFileSystemRoot.LocalConfig:
						return T.Localized("DeviceFileSystem.Root.LocalConfig");

					case DeviceFileSystemRoot.Roaming:
						return T.Localized("DeviceFileSystem.Root.Roaming");

					case DeviceFileSystemRoot.RoamingConfig:
						return T.Localized("DeviceFileSystem.Root.RoamingConfig");

					default:
						return RootPath;
				}
			}
		}

		// TODO: dodac Description

		public DeviceFileSystem(DeviceFileSystemRoot root = DeviceFileSystemRoot.Local, string userDefinedRootPath = null)
		{
			Root = root;
			Initialize(userDefinedRootPath);
		}

		// Implementation in:
		//
		// UWP: pbXNet\UWP\
		// iOS, macOS, Android, .NET: pbXNet\NETStd2\
	}
}
