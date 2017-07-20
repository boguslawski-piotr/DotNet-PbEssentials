using System;
using System.Threading.Tasks;
using pbXNet;

namespace pbX.Tests
{
    class Program
    {
		static async Task Tests()
		{
			try
			{
				IDatabase_Tests tests = new IDatabase_Tests(null);
				await tests.temp();
			}
			catch (Exception ex)
			{
				Log.E(ex);
				throw ex;
			}
		}

		static void Main(string[] args)
        {
			Log.AddLogger(new ConsoleLogger());
			Tests();
			Console.ReadKey();
        }
    }
}
