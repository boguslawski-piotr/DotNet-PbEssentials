using System;
using System.Threading.Tasks;
using pbXNet;
using Xunit;

namespace Tests
{
	public class IFileSystem_Tests
	{
		[Fact]
		public async Task FileSystemInDatabaseBasicTest()
		{
			IFileSystem fs = await FileSystemInDatabase.NewAsync(new SimpleDatabaseInMemory(), "Tests");

			await fs.CreateDirectoryAsync("test");

			await fs.SetCurrentDirectoryAsync("..");

			await fs.CreateDirectoryAsync("test2");

			await fs.SetCurrentDirectoryAsync("..");

			Assert.Equal("/", fs.CurrentPath);

			Assert.True(await fs.DirectoryExistsAsync("test"));

			Assert.True(await fs.DirectoryExistsAsync("test2"));

			await fs.DeleteDirectoryAsync("test2");

			Assert.False(await fs.DirectoryExistsAsync("test2"));

			await fs.SetCurrentDirectoryAsync("test");

			Assert.Equal("/test", fs.CurrentPath);

			for (int i = 0; i < 10; i++)
			{
				await fs.WriteTextAsync($"test{i}", $"dane{i}");
			}

			Assert.True(await fs.FileExistsAsync("test3"));

			Assert.False(await fs.FileExistsAsync("test33"));

			await fs.DeleteFileAsync("test3");

			Assert.False(await fs.FileExistsAsync("test3"));

			await fs.WriteTextAsync("test2", "ala ma kota");

			string d = await fs.ReadTextAsync("test2");

			Assert.Equal("ala ma kota", d);

			var l = await fs.GetFilesAsync("5$");

			Assert.NotEmpty(l);

			Assert.Single(l);

			l = await fs.GetFilesAsync("15$");

			Assert.Empty(l);
		}
	}
}
