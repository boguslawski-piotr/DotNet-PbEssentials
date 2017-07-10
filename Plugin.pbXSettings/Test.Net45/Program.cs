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

		//[Default(new DateTime(2010, 10, 10))]
		public DateTime DateTimeValue
		{
			get => Get<DateTime>();
			set => Set(value);
		}

		protected override object GetDefault(string key)
		{
			if (key == "DateTimeValue")
				return new DateTime(2010, 10, 10);
			return base.GetDefault(key);
		}

		public DateTime DateTimeValue2
		{
			get => Get<DateTime>(nameof(DateTimeValue2), new DateTime(2005, 10, 10));
			set => Set(value);
		}

		public DateTime DateTimeValue3
		{
			get => (DateTime)(Get(nameof(DateTimeValue3), (object)new DateTime(1995, 10, 10)));
			set => Set(value);
		}
	}

	class Program
	{
		static void Main(string[] args)
		{
			int i = Settings.Current.Get<int>("test");
			Settings.Current.Set(1, "test");

			i = Settings.Current.Get<int>("second test", 13);

			string s = Settings.Current.Get<string>("my string");
			Settings.Current.Set("smile, the wold will be better", "my string");

			Settings otherSet = new Settings("Plugin.pbXSettings.Test");
			i = otherSet.Get<int>("test");
			otherSet.Set(2, "test");

			MySettings mySet = new MySettings();

			i = mySet.IntValue;
			mySet.IntValue = 34;

			DateTime dt = mySet.DateTimeValue;
			mySet.DateTimeValue = new DateTime(2000, 10, 10);

			dt = mySet.DateTimeValue2;

			dt = mySet.DateTimeValue3;

			mySet.Set(true, "BoolValue");

			foreach (var kv in mySet)
			{
				Console.WriteLine($"{kv.Key} = {kv.Value.ToString()}");
			}

			bool exists = mySet.Contains("IntValue");
			exists = mySet.Contains("IntValue2");

			mySet.Remove("IntValue");
		}
	}
}
