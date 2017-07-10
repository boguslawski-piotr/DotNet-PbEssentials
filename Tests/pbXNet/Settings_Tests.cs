using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using pbXNet;
using Xunit;

namespace pbXNet
{
	public class Settings_Tests
	{
		class MySettings : Settings
		{
			[Default(true)]
			public bool BoolSetting
			{
				get => Get<bool>();
				set => Set(value);
			}

			[Default("some value")]
			public string StringSetting
			{
				get => Get<string>();
				set => Set(value);
			}
		}

		[Fact]
		public async Task BasicValues()
		{
			MySettings s = new MySettings();

			Assert.True(s.BoolSetting);
			Assert.True(s.StringSetting == "some value");

			s.BoolSetting = false;
			s.StringSetting = "another value";

			Assert.False(s.BoolSetting);
			Assert.False(s.StringSetting == "some value");
		}
	}
}
