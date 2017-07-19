using System.Data.SqlClient;
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
	public static class SqliteTestDb
	{
		public static SqliteConnection Connection
		{
			get {
				IFileSystem fs = DeviceFileSystem.New();
				string cs = $"Data Source={fs.RootPath}\\SqliteBasicTest.db";
				return new SqliteConnection(cs);
			}
		}
	}

	public static class SqlServerTestDb
	{
		public static SqlConnection Connection
		{
			get {
				string cs = "Server=(localdb)\\mssqllocaldb;Database=Test;Trusted_Connection=True;MultipleActiveResultSets=True";
				return new SqlConnection(cs);
			}
		}

		public static IDatabase Db
		{
			get {
				return new SDCDatabase(SqlServerTestDb.Connection, new SDCDatabase.Options
				{
					SqlBuilder = new SqlServerSqlBuilder()
				});
			}
		}
	}

	public class IDatabase_Tests
	{
		readonly ITestOutputHelper _output;

		string www;
		string pwww { get; set; }

		public IDatabase_Tests(ITestOutputHelper output)
		{
			_output = output;
			www = "qqq";
			pwww = "pqqq";
		}

		class Row
		{
			[PrimaryKey] [Length(256)] public string Path { get; set; }
			[PrimaryKey] [Length(256)] public string Name { get; set; }
			public bool? IsDirectory { get; set; }

			public override string ToString()
			{
				return $"{Path}/{Name}/{IsDirectory}";
			}
		}

		class Row2
		{
			public string Path { get; set; }
			public string Name { get; set; }
		}

		[Fact]
		public async Task SimpleDatabaseInMemory_BasicTest()
		{
			IDatabase db = new SimpleDatabaseInMemory();
			await IDatabaseBasicTest(db);
		}

		[Fact]
		public async Task Sqlite_BasicTest()
		{
			IDatabase db = new SDCDatabase(SqliteTestDb.Connection);
			await IDatabaseBasicTest(db);
		}

		[Fact]
		public async Task SqlServer_BasicTest()
		{
			IDatabase db = SqlServerTestDb.Db;
			await IDatabaseBasicTest(db);
		}

		[Fact]
		public async Task SimpleDatabaseInMemory_OrderByTest()
		{
			IDatabase db = new SimpleDatabaseInMemory();
			await IDatabaseOrderByTest(db);
		}

		[Fact]
		public async Task Sqlite_OrderByTest()
		{
			IDatabase db = new SDCDatabase(SqliteTestDb.Connection);
			await IDatabaseOrderByTest(db);
		}

		[Fact]
		public async Task SqlServer_OrderByTest()
		{
			IDatabase db = SqlServerTestDb.Db;
			await IDatabaseOrderByTest(db);
		}

		public void temp()
		{
			//int iii = 20 * 30;
			//var q0 = t.Rows.Where(r => (iii & 20) == 0);
			//int ccc = await q0.CountAsync();

			//q0 = t.Rows.Where(r => r.Path.StartsWith("ftest10"));
			//ccc = await q0.CountAsync();

			//q0 = t.Rows.Where(r => r.Path.EndsWith("10"));
			//ccc = await q0.CountAsync();

			//q0 = t.Rows.Where(r => !r.IsDirectory).Where(r => r.Name != null).Where(r => null != r.Name);
			//using (await q0.QueryAsync()) { }
			//ccc = await q0.CountAsync();

			//q0 = t.Rows.Where(r => r.Path == pwww);
			//using (await q0.QueryAsync()) { }

			//q0 = t.Rows.Where(r => r.Path == www);
			//using (await q0.QueryAsync()) { }

			//string xxx = "zzz";
			//q0 = t.Rows.Where(r => r.Name == xxx);
			//using (await q0.QueryAsync()) { }

			//q0 = t.Rows.Where(r => (r.Path == "cos" || r.Name == xxx && (Regex.IsMatch(r.Name, "35$"))) && r.IsDirectory);
			//using (await q0.QueryAsync()) { }

		}

		public async Task IDatabaseBasicTest(IDatabase db)
		{
			await db.DropTableAsync("Tests");
			var t = await db.TableAsync<Row>("Tests");

			for (int i = 0; i < 10000; i++)
			{
				await t.InsertOrUpdateAsync(new Row
				{
					Path = $"ftest{i}",
					Name = $"dane{i.ToString().PadLeft(5, '0')}",
					IsDirectory = i % 2 == 0
				})
				.ConfigureAwait(false);
			}

			Assert.True(await t.Rows.AnyAsync());

			Assert.True(await t.Rows.CountAsync() == 10000);

			var t2 = await db.TableAsync<Row>("Tests2");

			Assert.False(await t2.Rows.AnyAsync());

			var q1 = t.Rows.Where(r => r.Name.EndsWith("35"));

			Assert.True(await q1.AnyAsync());

			Assert.True(await q1.CountAsync() == 100);

			using (var q = await
				db.Table<Row>("Tests").Rows
					.Where(r => Regex.IsMatch(r.Name, "35$"))
					.OrderByDescending(r => r.Name)
					.ResultAsync().ConfigureAwait(false)
				)
			{

				foreach (var r in q)
				{
					_output.WriteLine($"{r.Path}/{r.Name}");
				}

				int ccc = q.Count();
				Assert.True(q.Count() == 100);

				Assert.True(q.First().Name == "dane09935");

				Assert.True(q.Last().Name == "dane00035");
			}

			int dr = await t.DeleteAsync(r => r.Name.EndsWith("35"));

			Assert.True(dr == 100);
		}

		public async Task IDatabaseOrderByTest(IDatabase db)
		{
			await db.DropTableAsync("OrderByTests");
			var t = await db.TableAsync<Row>("OrderByTests");

			for (int i = 0; i < 5; i++)
			{
				string path = $"path{i.ToString().PadLeft(5, '0')}";
				for (int n = 0; n < 5; n++)
				{
					await t.InsertOrUpdateAsync(new Row
					{
						Path = path,
						Name = $"name{n.ToString().PadLeft(5, '0')}",
						IsDirectory = i % 2 == 0
					})
					.ConfigureAwait(false);
				}
			}

			var q = await t.Rows
					.OrderByDescending(r => r.Path)
					.OrderByDescending(r => r.Name)
					.ResultAsync().ConfigureAwait(false);

			using (q)
			{
				foreach (var r in q)
				{
					_output.WriteLine(r.ToString());
				}

				Assert.True(q.Count() == 25);

				Assert.True(q.First().ToString() == "path00004/name00004/True");

				Assert.True(q.Last().ToString() == "path00000/name00000/True");

				Assert.True(q.Skip(3).First().ToString() == "path00004/name00001/True");

				Assert.True(q.SkipLast(5).Last().ToString() == "path00001/name00000/False");
			}
		}
	}
}
