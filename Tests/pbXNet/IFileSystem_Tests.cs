using System;
using pbXNet;
using Xunit;

namespace Tests
{
	public class IFileSystem_Tests
	{
		[Fact]
		public void FileSystemInDatabaseBasicTest()
		{
			//IFileSystem fs = FileSystemInDatabase.New("SQlite;Data Source=?.db", "Tests");

			//var b = new DbContextOptionsBuilder<FileSystemInDatabase>()
			//	.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=?;Trusted_Connection=True;MultipleActiveResultSets=true");
			//IFileSystem fs2 = FileSystemInDatabase.New(b.Options, "Tests2");

			//fs.CreateDirectoryAsync("First");
		}
	}
}
