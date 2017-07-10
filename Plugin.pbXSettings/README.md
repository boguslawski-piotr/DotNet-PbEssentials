# pbX Settings Plugin for UWP, iOS, macOS, tvOS, watchOS, Android and .NET

Plugin (actually simple class `Plugin.pbXSettings.Settings` and `Plugin.pbXSettings.SettingsStorage` pair) that makes it easy to handle all sorts of settings, more precisely, values of any type that is accessed through a key. 

This plugin was inspired by another similar component written by James Montemagno, but I used a different approach for data handling, interface and usage.

This plugin uses the native settings storage, which means all settings are persisted across app updates, saved natively, and on some platforms synchronized between devices.

- Android: SharedPreferences
- Apple: NSUserDefaults
- UWP: ApplicationDataContainer
- _.NET_ / _.NET_ Core 2: UserStore -> IsolatedStorageFile

The main plugin class `Plugin.pbXSettings.Settings` is fully ready for use in any binding systems because implements interface INotifyPropertyChanged. Can also be used as a regular collection, that is, it can be enumerated :)

This plugin uses _.NET_ `DataContractSerializer` for saving and restoring keys and values, which means you can put and retrieve practically any valid _.NET_ object as long as it meets the requirements of this serialization technolgy: [https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.datacontractserializer?view=netframework-4.5.1](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.datacontractserializer?view=netframework-4.5.1)
	
## Installation

Just install NuGet package [`Xam.Plugins.pbXSettings`](https://www.nuget.org/packages/Xam.Plugins.pbXSettings) [![NuGet](https://img.shields.io/nuget/v/Xam.Plugins.pbXSettings.svg?label=NuGet)](https://www.nuget.org/packages/Xam.Plugins.pbXSettings) into your PCL project (if you have any)) and all application projects (for each platform there is at least one project).

## Platform Support

- Universal Windows Platform
- Xamarin.iOS
- Xamarin.Mac
- Xamarin.watchOS
- Xamarin.tvOS
- Xamarin.Android (compiled for API 19)
- .NET 4.5 (and later versions)
- .NET Core 2 / .NET STandard 2 (soon)

Also
- Plugin works OK with Xamarin.Forms (PCL or Shared)

## Getting started

### Basic use

```csharp
using Plugin.pbXSettings;

// on first run it should return: 0
// on next runs it should return: 1
int i = Settings.Current.Get<int>("test");
Settings.Current.Set(1, "test");

// it should return: 13
i = Settings.Current.Get<int>("second test", 13);

// on first run it should return: "" (empty string)
// on next runs it should return: smile, the wold will be better 
string s = Settings.Current.Get<string>("my string");
Settings.Current.Set("smile, the wold will be better", "my string");

// And so on, with all the basic types of data.
```
### You can use your class that inherits after `Plugin.pbXSettings.Settings`

```csharp
class MySettings : Settings
{
	[Default(17)]
	public int IntValue
	{
		// You don't need to provide key name :), compiler will do it for you.
		get => Get<int>();
		set => Set(value);
	}

	// [Default(new DateTime(2010, 10, 10))] 
	// can't use attribute for non constants :(
	public DateTime DateTimeValue
	{
		get => Get<DateTime>();
		set => Set(value);
	}

	// but you can use GetDefault method :)
	protected override object GetDefault(string key)
	{
		if (key == "DateTimeValue")
			return new DateTime(2010, 10, 10);
		return base.GetDefault(key);
	}

	// or provide default value other way
	public DateTime DateTimeValue2
	{
		get => Get<DateTime>(nameof(DateTimeValue2), new DateTime(2005, 10, 10));
		set => Set(value);
	}
}

MySettings mySet = new MySettings();

// on first run it should return: 17
// on next runs it should return: 34
i = mySet.IntValue;
mySet.IntValue = 34;

// on first run it should return: 10/10/2010
// on next runs it should return: 10/10/2000
DateTime dt = mySet.DateTimeValue;
mySet.DateTimeValue = new DateTime(2000, 10, 10);

// should return: 10/10/2005
dt = mySet.DateTimeValue2;

// defines value that is not a property
mySet.Set(true, "BoolValue");
```

### You can use as many as you want set of settings

```csharp
// this uses default settings
int i = Settings.Current.Get<int>("test");
Settings.Current.Set(1, "test");

// this uses 'my set 2' set, completely separate from others
Settings otherSet = new Settings("my set 2");
i = otherSet.Get<int>("test");
otherSet.Set(2, "test");
```
### You can use set of settings as collection

```csharp
foreach (var kv in mySet)
{
	Console.WriteLine($"{kv.Key} = {kv.Value.ToString()}");
}
```

### You can delete keys/values or clear entire collection

```csharp
bool exists = mySet.Contains("IntValue"); // true
exists = mySet.Contains("IntValue2");     // false

// removes value from storage
mySet.Remove("BoolValue");

// clears entire set of settings
mySet.Clear();
```

### You can use low level access to platform settings storage

```csharp
public interface ISettingsStorage
{
	Task<string> GetStringAsync(string id);
	Task SetStringAsync(string id, string d);
}

// and use

SettingsStorage.Current

// object which is type SettingsStorage that implements ISettingsStorage

```

## More documentation

[Here: https://boguslawski-piotr.github.io/pbX/api/pbXNet.Settings.html](https://boguslawski-piotr.github.io/pbX/api/pbXNet.Settings.html) you will find full documentation for `pbXNet.Settings` class which is a base class for `Plugin.pbXSettings.Settings`.

## Contributions

Contributions are welcome. If you find a bug please report it and if you want a feature please describe it clearly and place it in Issues with '_enhancement_' label.

If you want to contribute code please file an issue and create a branch off of the current dev branch and file a pull request.

## License

Under MIT, see LICENSE file.