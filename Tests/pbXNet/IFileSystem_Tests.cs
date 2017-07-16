using System;
using System.Linq;
using System.Threading.Tasks;
using pbXNet;
using pbXNet.Database;
using Xunit;
using Xunit.Abstractions;

namespace pbXNet
{
	public class IFileSystem_Tests
	{
		readonly ITestOutputHelper _output;

		public IFileSystem_Tests(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public async Task FileSystemInDatabaseStressTest()
		{
			SimpleDatabaseInMemory db = new SimpleDatabaseInMemory();

			IFileSystem fs = await FileSystemInDatabase.NewAsync("Tests", db);

			await Prepare(fs);

			for (int i = 0; i < 10000; i++)
			{
				await fs.WriteTextAsync($"ftest{i}", $"dane{i}");
			}

			//await db.Dump("FileSystemInDatabaseStressTest.json", DeviceFileSystem.New(DeviceFileSystemRoot.UserDefined, "~"));

			string d = await fs.ReadTextAsync("ftest7865");

			Assert.True(d == "dane7865");

			var l = await fs.GetFilesAsync("5$");

			Assert.True(l.Count() == 10000 / 10);

			await Cleanup(fs);
		}

		[Fact]
		public async Task DeviceFileSystemDefaultBasicTest()
		{
			IFileSystem fs = DeviceFileSystem.New();
			await IFileSystemBasicTest(fs);
		}

		[Fact]
		public async Task DeviceFileSystemConfigBasicTest()
		{
			IFileSystem fs = DeviceFileSystem.New(DeviceFileSystem.RootType.LocalConfig);
			await IFileSystemBasicTest(fs);
		}

		[Fact]
		public async Task FileSystemInDatabaseBasicTest()
		{
			IFileSystem fs = await FileSystemInDatabase.NewAsync("Tests", new SimpleDatabaseInMemory());
			await IFileSystemBasicTest(fs);
		}

		async Task Prepare(IFileSystem fs)
		{
			await fs.CreateDirectoryAsync("pbXNet Tests");
		}

		async Task Cleanup(IFileSystem fs)
		{
			var l = await fs.GetFilesAsync("");
			foreach (var f in l.ToArray())
				await fs.DeleteFileAsync(f);
			await fs.SetCurrentDirectoryAsync("..");
			await fs.DeleteDirectoryAsync("pbXNet Tests");
		}

		async Task IFileSystemBasicTest(IFileSystem fs)
		{
			await Prepare(fs);

			await fs.CreateDirectoryAsync("test");

			await fs.SetCurrentDirectoryAsync("..");

			await fs.CreateDirectoryAsync("test2");

			await fs.SetCurrentDirectoryAsync("..");

			var l = await fs.GetDirectoriesAsync("");

			Assert.Contains(l, (e) => e == "test" || e == "test2");

			Assert.True(l.Count() == 2);

			Assert.True(await fs.DirectoryExistsAsync("test"));

			Assert.True(await fs.DirectoryExistsAsync("test2"));

			await fs.DeleteDirectoryAsync("test2");

			Assert.False(await fs.DirectoryExistsAsync("test2"));

			await fs.DeleteDirectoryAsync("test");

			for (int i = 0; i < 10; i++)
			{
				await fs.WriteTextAsync($"ftest{i}", $"dane{i}");
			}

			Assert.True(await fs.FileExistsAsync("ftest3"));

			Assert.False(await fs.FileExistsAsync("ftest33"));

			await fs.DeleteFileAsync("ftest3");

			Assert.False(await fs.FileExistsAsync("ftest3"));

			await fs.WriteTextAsync("ftest2", "ala ma kota");

			string d = await fs.ReadTextAsync("ftest2");

			Assert.Equal("ala ma kota", d);

			await fs.SetFileModifiedOnAsync("ftest2", new DateTime(2009, 11, 3, 12, 33, 55));

			DateTime dt = await fs.GetFileModifiedOnAsync("ftest2");

			Assert.Equal(dt.ToLocalTime(), new DateTime(2009, 11, 3, 12, 33, 55));

			await fs.SetFileModifiedOnAsync("ftest2", new DateTime(2017, 1, 7, 15, 11, 22, DateTimeKind.Utc));

			dt = await fs.GetFileModifiedOnAsync("ftest2");

			Assert.Equal(dt, new DateTime(2017, 1, 7, 15, 11, 22, DateTimeKind.Utc));

			l = await fs.GetFilesAsync("5$");

			Assert.NotEmpty(l);

			Assert.Single(l);

			Assert.True(l.First() == "ftest5");

			l = await fs.GetFilesAsync("15$");

			Assert.Empty(l);

			await Cleanup(fs);
		}
	}
}