﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

		string _userDefinedRootPath;

		protected DeviceFileSystem(DeviceFileSystemRoot root = DeviceFileSystemRoot.Local, string userDefinedRootPath = null)
		{
			Root = root;
			_userDefinedRootPath = userDefinedRootPath;
		}

		public static IFileSystem New(DeviceFileSystemRoot root = DeviceFileSystemRoot.Local, string userDefinedRootPath = null)
		{
			IFileSystem fs = new DeviceFileSystem(root, userDefinedRootPath);
			fs.Initialize();
			return fs;
		}

		public class State
		{
			class Rec
			{
				public string RootPath;
				public string CurrentPath;
				public Stack<string> VisitedPaths;
			}

			Stack<Rec> _stack = new Stack<Rec>();

			public void Save(string rootPath, string currentPath, Stack<string> visitedPaths)
			{
				_stack.Push(new Rec
				{
					RootPath = rootPath,
					CurrentPath = currentPath,
					VisitedPaths = new Stack<string>(visitedPaths.AsEnumerable()),
				});
			}

			public bool Restore(ref string rootPath, ref string currentPath, ref Stack<string> visitedPaths)
			{
				if (_stack.Count > 0)
				{
					Rec entry = _stack.Pop();
					rootPath = entry.RootPath;
					currentPath = entry.CurrentPath;
					visitedPaths = entry.VisitedPaths;
					return true;
				}
				return false;
			}
		}

		// Remaining implementation in:
		//
		// UWP: pbXNet\UWP\
		// iOS, macOS, Android, .NET: pbXNet\NETStd2\
	}
}
