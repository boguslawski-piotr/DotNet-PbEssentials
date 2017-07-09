using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plugin.pbXSettings;

namespace Test.Net45
{
	class MySettings : Settings
	{
		[Default(17)]
		public int IntValue
		{
			get => Get<int>();
			set => Set(value);
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			int i = (int)Settings.Current.Get<int>("test");
			Settings.Current.Set(1, "test");

			Settings otherSet = new Settings("Plugin.pbXSettings.Test");
			i = otherSet.Get<int>("test");
			otherSet.Set(2, "test");

			MySettings mySet = new MySettings();
			i = mySet.IntValue;
			mySet.IntValue = 34;
		}
	}
}
