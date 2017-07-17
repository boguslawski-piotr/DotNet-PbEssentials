using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using pbXNet;
using pbXNet.Database;
using Xunit;
using Xunit.Abstractions;

namespace pbXNet
{
	public class IDatabase_Tests
    {
		readonly ITestOutputHelper _output;

		public IDatabase_Tests(ITestOutputHelper output)
		{
			_output = output;
		}

		class Row
		{
			[PrimaryKey] public string Path { get; set; }
			[PrimaryKey] public string Name { get; set; }
			public bool IsDirectory { get; set; }
		}

		[Fact]
		public async Task SimpleDatabaseInMemoryBasicTest()
		{
			IDatabase db = new SimpleDatabaseInMemory();
			await IDatabaseBasicTest(db);
		}

		[Fact]
		public async Task SqliteBasicTest()
		{
			IFileSystem fs = DeviceFileSystem.New();
			string ds = $"Data Source={fs.RootPath}\\SqliteBasicTest.db";
			IDatabase db = new SDCDatabase(new SqliteConnection(ds));
			await IDatabaseBasicTest(db);
		}

		public async Task IDatabaseBasicTest(IDatabase db)
		{
			string sql1 = new SqlBuilder().Create().Table("name");
			string sql2 = new SqlBuilder().Create().Index("name").On("tname");

			string sql3 = new SqlBuilder().Drop().Table("name");
			string sql4 = new SqlBuilder().Drop().Index("name").On("tname");


			var t = await db.TableAsync<Row>("Tests");

			//for (int i = 0; i < 10000; i++)
			//{
			//	await t.InsertOrUpdateAsync(new Row
			//	{
			//		Path = $"ftest{i}",
			//		Name = $"dane{i.ToString().PadLeft(5, '0')}",
			//		IsDirectory = i % 2 == 0
			//	})
			//	.ConfigureAwait(false);
			//}

			Assert.True(await t.Rows.AnyAsync());

			var t2 = await db.TableAsync<Row>("Tests2");

			Assert.False(await t2.Rows.AnyAsync());

			using (var q = await
				db.Table<Row>("Tests").Rows
					.Where(r => Regex.IsMatch(r.Name, "35$"))
					.OrderByDescending(r => r.Name)
					.PrepareAsync().ConfigureAwait(false)
				)
			{
				foreach (var r in q)
				{
					_output.WriteLine($"{r.Path}/{r.Name}");
				}

				Assert.True(q.Count() == 10000 / 100);

				Assert.True(q.First().Name == "dane09935");

				Assert.True(q.Last().Name == "dane00035");
			}
		}
	}
}
