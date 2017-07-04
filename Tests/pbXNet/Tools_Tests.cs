using System;
using System.Text.RegularExpressions;
using Xunit;

namespace pbXNet.Tests
{
	public class Tools_Tests
	{
		[Fact]
		public void MakeIdenticalIfDifferent()
		{
			int a = 1;
			int b = 1;
			Assert.False(Tools.MakeIdenticalIfDifferent(a, ref b), "a == b");
			a = 0;
			Assert.True(Tools.MakeIdenticalIfDifferent(a, ref b), "a != b");
			Assert.True(b == 0, "b == 0");
			Assert.True(b == a, "b == a");
		}

		[Fact]
		public void CreateGuid()
		{
			string g = Tools.CreateGuid();
			Assert.True(g.Length == 32, "length");
			Assert.True(Regex.IsMatch(g, "[a-z0-9]{32}"), "content");
		}
	}
}
