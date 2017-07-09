using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml;

namespace Plugin.pbXSettings
{
	/// <summary>
	/// <para>
	///		Class that makes it easy to handle all sorts of settings, 
	///		more precisely, values of any type that is accessed through a key.
	/// </para>
	/// <para>
	///		The class is fully ready for use in binding systems because it inherits after <see cref="Observable"/>.
	/// </para>
	/// </summary>
	/// <example>
	/// <code>
	/// public class Main
	/// {
	///		public static void Main(string[] args)
	///		{
	///			MySettings s = new MySettings();
	///			s.Load();
	///			
	///			Console.WriteLine($"{s.BoolSetting}, {s.StringSetting}");
	///			Console.WriteLine($"---{s.Get&lt;string&gt;("test")}---");
	///			
	///			s["test"] = "Hello!";
	///			s.BoolSetting = false;
	///			s.StringSetting = "another value";
	///			
	///			// The first time you start the program, you should see:
	///			//	
	///			// True, some value
	///			// ------
	///			//
	///			// The next time you run the program, you should see:
	///			//
	///			// False, another value
	///			// ---Hello!---
	///		}
	/// }
	/// 
	/// public class MySettings : Settings
	/// {
	///		[Default(true)]
	///		public bool BoolSetting
	///		{
	///			get => Get&lt;bool&gt;();
	///			set => Set(value);
	///		}
	///		
	///		[Default("some value")]
	///		public string StringSetting
	///		{
	///			get => Get&lt;string&gt;();
	///			set => Set(value);
	///		}
	///		
	///		...
	///		
	/// }
	/// </code>
	/// </example>
	public class Settings : pbXNet.Observable
	{
		public string Id { get; private set; }

		/// <summary>
		/// Thread-safe container for keys and values.
		/// </summary>
		protected ConcurrentDictionary<string, object> KeysAndValues = new ConcurrentDictionary<string, object>();

		/// <summary>
		/// Attribute used to decorate and provide default value for properties in classes that inherit from <see cref="Settings"/>.
		/// </summary>
		/// <example>
		/// <code>
		///		[Default(true)]
		///		public bool BoolSetting
		///		{
		///			get => Get&lt;bool&gt;();
		///			set => Set(value);
		///		}
		/// </code>
		/// </example>
		[AttributeUsage(AttributeTargets.Property)]
		public class DefaultAttribute : Attribute
		{
			public DefaultAttribute(object defaultValue)
			{ }
		}

		const string _defaultId = ".a0d40b25942b4788904532af03886608";

		public Settings()
		{
			Id = _defaultId;
		}

		public Settings(string id)
		{
			Id = id ?? _defaultId;
		}

		/// <summary>
		/// Sets the current value for a <paramref name="key"/>.
		/// </summary>
		public virtual void Set(object value, [CallerMemberName]string key = null) => this[key] = value;

		/// <summary>
		/// Gets the current value (as an object) for a <paramref name="key"/> or if <paramref name="key"/> doesn't exist
		/// the default value: specified in <see cref="DefaultAttribute"/> attribute, provided by <see cref="GetDefault(string)" /> or null.
		/// </summary>
		public virtual object Get([CallerMemberName]string key = null) => this[key];

		/// <summary>
		/// Gets the current value (as an object type <typeparamref name="T"/>) for a <paramref name="key"/> or if <paramref name="key"/> doesn't exist
		/// the default value: specified in <see cref="DefaultAttribute"/> attribute, provided by <see cref="GetDefault(string)" /> or default(T).
		/// </summary>
		public virtual T Get<T>([CallerMemberName]string key = null) => (T)(ConvertTo(this[key], typeof(T)) ?? default(T));

		/// <summary>
		/// Gets the current value (as an object) for a <paramref name="key"/> or if <paramref name="key"/> doesn't exist
		/// the default value: specified in <see cref="DefaultAttribute"/> attribute, provided by <see cref="GetDefault(string)" /> or null.
		/// </summary>
		public virtual object this[string key]
		{
			get {
				if (!KeysAndValues.TryGetValue(key, out object value))
					value = GetDefault(key);

				try
				{
					PropertyInfo property = this.GetType().GetRuntimeProperties().First((_p) => _p.Name == key);
					return ConvertTo(value, property.PropertyType);
				}
				catch { }

				return value;
			}

			set {
				SetAsync(value, key);
			}
		}

		/// <summary>
		/// Checks whether there is a value described by <paramref name="key"/>.
		/// </summary>
		public virtual bool Contains(string key)
		{
			return KeysAndValues.ContainsKey(key);
		}

		/// <summary>
		/// Removes a <paramref name="key"/> and the corresponding value.
		/// </summary>
		public virtual void Remove(string key)
		{
			if (KeysAndValues.TryRemove(key, out object _))
				SaveAsync(null);
		}

		/// <summary>
		/// Removes all keys and corresponding values from settings.
		/// </summary>
		public virtual void Clear()
		{
			KeysAndValues.Clear();
			SaveAsync(null);
		}

		/// <summary>
		/// Gets default value, specified in <see cref="DefaultAttribute"/> attribute, for a property whose name matches the content of <paramref name="key"/>.
		/// <para>The function can be overrided and used to provide more complex default values that can not be passed with the <see cref="DefaultAttribute"/> attribute.</para>
		/// </summary>
		protected virtual object GetDefault(string key)
		{
			// Try to find the property corresponding to key and its default value.
			try
			{
				PropertyInfo property = this.GetType().GetRuntimeProperties().First((_p) => _p.Name == key);
				try
				{
					CustomAttributeData defaultAttribute = property.CustomAttributes.First((_a) => _a.AttributeType.Name == nameof(DefaultAttribute));
					return defaultAttribute.ConstructorArguments[0].Value;
				}
				catch (Exception)
				{
					return Activator.CreateInstance(property.PropertyType);
				}
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// </summary>
		public virtual async Task LoadAsync()
		{
			string d = await SettingsStorage.Current.GetStringAsync(Id);
			if (!string.IsNullOrWhiteSpace(d))
			{
				//d = Obfuscator.DeObfuscate(d);
				KeysAndValues = Deserialize(d);
			}
			else
				KeysAndValues?.Clear();
		}

		/// <summary>
		/// </summary>
		public virtual async Task SaveAsync(string changedValueKey = null)
		{
			string d = Serialize();
			if (d != null)
			{
				//d = Obfuscator.Obfuscate(d);
				await SettingsStorage.Current.SetStringAsync(Id, d);
			}
		}

		/// <summary>
		/// Synchronous shortcut for <see cref="LoadAsync"/>.
		/// </summary>
		public void Load() => LoadAsync().GetAwaiter().GetResult();

		/// <summary>
		/// Synchronous shortcut for <see cref="SaveAsync"/>.
		/// </summary>
		public void Save(string changedValueKey = null) => SaveAsync(changedValueKey).GetAwaiter().GetResult();

		#region Tools

		async Task SetAsync(object newValue, string key)
		{
			if (!KeysAndValues.TryGetValue(key, out object value))
				value = GetDefault(key);

			bool valueChanged = SetValue(ref value, newValue, key);

			KeysAndValues[key] = value;

			if (valueChanged)
				await SaveAsync(key);
		}

		object ConvertTo(object value, Type type)
		{
			Type valueType = value == null ? typeof(object) : value.GetType();

			if (!type.Equals(valueType))
			{
				if (type == typeof(bool)) return Convert.ToBoolean(value);
				else if (type == typeof(char)) return Convert.ToChar(value);
				else if (type == typeof(string)) return Convert.ToString(value);
				else if (type == typeof(DateTime)) return Convert.ToDateTime(value);
				else if (type == typeof(sbyte)) return Convert.ToSByte(value);
				else if (type == typeof(byte)) return Convert.ToByte(value);
				else if (type == typeof(short)) return Convert.ToInt16(value);
				else if (type == typeof(int)) return Convert.ToInt32(value);
				else if (type == typeof(long)) return Convert.ToInt64(value);
				else if (type == typeof(ushort)) return Convert.ToUInt16(value);
				else if (type == typeof(uint)) return Convert.ToUInt32(value);
				else if (type == typeof(ulong)) return Convert.ToUInt64(value);
				else if (type == typeof(float)) return Convert.ToSingle(value);
				else if (type == typeof(double)) return Convert.ToDouble(value);
				else if (type == typeof(decimal)) return Convert.ToDecimal(value);
				else
				{
					if (value != null)
						return Activator.CreateInstance(type, value);
				}
			}

			return value;
		}

		string Serialize()
		{
			using (var dwriter = new StringWriter())
			using (var xmlwriter = XmlWriter.Create(dwriter))
			{
				try
				{
					var dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(ConcurrentDictionary<string, object>));
					dcs.WriteObject(xmlwriter, KeysAndValues);
					xmlwriter.Flush();
					return dwriter.ToString();
				}
				catch (Exception ex)
				{
					return null;
				}
			}
		}

		ConcurrentDictionary<string, object> Deserialize(string d)
		{
			using (var dreader = new StringReader(d))
			using (var xmlreader = XmlReader.Create(dreader))
			{
				try
				{
					var dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(ConcurrentDictionary<string, object>));
					return (ConcurrentDictionary<string, object>)dcs.ReadObject(xmlreader);
				}
				catch (Exception ex)
				{
					return null;
				}
			}
		}

		#endregion
	}
}
