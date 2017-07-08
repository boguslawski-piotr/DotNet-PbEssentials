using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using pbXNet;
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
			public string Path { get; set; }
			public string Name { get; set; }
			public bool IsDirectory { get; set; }
		}

		[Fact]
		public async Task SimpleDatabaseInMemoryBasicTest()
		{
			IDatabase db = new SimpleDatabaseInMemory();
			await IDatabaseBasicTest(db);
		}

		public async Task IDatabaseBasicTest(IDatabase db)
		{
			var t = await db.CreateTableAsync<Row>("Tests");
			//await t.CreatePrimaryKeyAsync("Path");

			for (int i = 0; i < 10000; i++)
			{
				await t.InsertAsync(new Row {
					Path = $"ftest{i}",
					Name = $"dane{i.ToString().PadLeft(5, '0')}",
					IsDirectory = i % 2 == 0
				})
				.ConfigureAwait(false);
			}

			var q = await
				db.Table<Row>("Tests")
					.Rows
						.Where((r) => Regex.IsMatch(r.Name, "35$"))
						.OrderByDescending(r => r.Name)
							.PrepareAsync()
								.ConfigureAwait(false);

			Assert.True(q.Count() == 10000 / 100);

			Assert.True(q.First().Name == "dane09935");

			Assert.True(q.Last().Name == "dane00035");

			//foreach (var r in q)
			//{
			//	_output.WriteLine($"{r.Path}/{r.Name}");
			//}
		}
	}
}
