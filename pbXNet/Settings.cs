using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#if PLUGIN_PBXSETTINGS
namespace Plugin.pbXSettings.pbXNet
#else
namespace pbXNet
#endif
{
	/// <summary>
	/// <para>
	///	Class that makes it easy to handle all sorts of settings, 
	///	more precisely, values of any type that is accessed through a key.
	/// </para>
	/// </summary>
	/// <remarks>
	/// <para>The class is fully ready for use in binding systems because it inherits after <see cref="Observable"/>.</para>
	/// <para>The class can also be used as a regular collection, that is, it can be enumerated :)</para>
	/// </remarks>
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
	///			// Assuming that the data has been saved, that is you implemented 
	///			// virtual functions LoadAsync and SaveAsync or
	///			// used <see cref="SettingsInStorage"/> as base class for MySettings.
	///			//
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
	///		public override async Task LoadAsync()
	///		{
	///			// Load your settings the way you like.
	///			
	///			// Or use as a base class one of the predefined classes 
	///			// in pbXNet that support read and write settings.
	///		}
	///		
	///		public override async Task SaveAsync(string changedValueKey = null)
	///		{
	///			// Save your settings as you like.
	///			
	///			// Or use as a base class one of the predefined classes 
	///			// in pbXNet that support read and write settings.
	///		}
	/// }
	/// </code>
	/// </example>
	/// <seealso cref="SettingsInStorage"/>
	public class Settings : Observable, IEnumerable<KeyValuePair<string, object>>
	{
		/// <summary>
		/// Thread-safe container for keys and values.
		/// </summary>
		protected ConcurrentDictionary<string, object> KeysAndValues = new ConcurrentDictionary<string, object>();

		/// <summary>
		/// Attribute used to decorate and provide default value for properties in classes that inherit from <see cref="Settings"/>.
		/// </summary>
		/// <example>
		/// <code>
		///	[Default(true)]
		///	public bool BoolSetting
		///	{
		///		get => Get&lt;bool&gt;();
		///		set => Set(value);
		///	}
		/// </code>
		/// </example>
		[AttributeUsage(AttributeTargets.Property)]
		public class DefaultAttribute : Attribute
		{
			public DefaultAttribute(object defaultValue)
			{ }
		}

		/// <summary>
		/// Sets the current value for a <paramref name="key"/>.
		/// </summary>
		public virtual void Set(object value, [CallerMemberName]string key = null) => this[key] = value;

		/// <summary>
		/// Gets the current value (as an object) for a <paramref name="key"/> or if <paramref name="key"/> doesn't exist
		/// the default value: specified in <see cref="DefaultAttribute"/> attribute for property or 
		/// provided by <see cref="GetDefault(string)" /> or 
		/// specified in parameter <paramref name="def"/> or
		/// null.
		/// </summary>
		public virtual object Get([CallerMemberName]string key = null, object def = null)
		{
			if (!KeysAndValues.TryGetValue(key, out object value))
				value = GetDefault(key);

			try
			{
				PropertyInfo property = this.GetType().GetRuntimeProperties().First((_p) => _p.Name == key);
				return ConvertTo(value, property.PropertyType, def);
			}
			catch { }

			return value;
		}

		/// <summary>
		/// Gets the current value (as an object type <typeparamref name="T"/>) for a <paramref name="key"/> or if <paramref name="key"/> doesn't exist
		/// the default value: specified in <see cref="DefaultAttribute"/> attribute for property or
		/// provided by <see cref="GetDefault(string)" /> or 
		/// specified in parameter <paramref name="def"/> or 
		/// default(T).
		/// </summary>
		public virtual T Get<T>([CallerMemberName]string key = null, T def = default(T))
		{
			if (!KeysAndValues.TryGetValue(key, out object value))
				value = GetDefault(key);

			return (T)(ConvertTo(value, typeof(T), def) ?? default(T));
		}

		/// <summary>
		/// Gets the current value (as an object) for a <paramref name="key"/> or if <paramref name="key"/> doesn't exist
		/// the default value: specified in <see cref="DefaultAttribute"/> attribute for property or
		/// provided by <see cref="GetDefault(string)" /> or 
		/// null.
		/// </summary>
		public virtual object this[string key]
		{
			get => Get(key, null);
			set => SetAsync(value, key);
		}

		/// <summary>
		/// Gets default value, specified in <see cref="DefaultAttribute"/> attribute, for a property whose name matches the content of a <paramref name="key"/>.
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
					return null;
				}
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Checks whether there is a value described by <paramref name="key"/>.
		/// </summary>
		public virtual bool Contains(string key)
		{
			if (KeysAndValues.ContainsKey(key))
				return true;
			try
			{
				PropertyInfo property = this.GetType().GetRuntimeProperties().First((_p) => _p.Name == key);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Removes (or turns to default value for properties) a <paramref name="key"/> and the corresponding value.
		/// </summary>
		public virtual void Remove(string key)
		{
			if (KeysAndValues.TryRemove(key, out object _))
				SaveAsync(null);
		}

		/// <summary>
		/// Removes all keys and corresponding values from settings set.
		/// </summary>
		public virtual void Clear()
		{
			KeysAndValues.Clear();
			SaveAsync(null);
		}

		// At first glance it looks like LoadAsync and SaveAsync should be abstract 
		// but then to use the settings as a fast container for temporary data 
		// would require creating a new class and inheriting.
		// That is why (for convenience) these functions are not abstract ;).

		/// <summary>
		/// By default it does nothing.
		/// When overridden should load entire collection of keys and values into <see cref="KeysAndValues"/>.
		/// </summary>
		public virtual async Task LoadAsync()
		{ }

		/// <summary>
		/// By default it does nothing.
		/// When overridden should save <paramref name="changedValueKey"/> key and corresponding value 
		/// or save entire collection when <paramref name="changedValueKey"/> is set to null.
		/// </summary>
		public virtual async Task SaveAsync(string changedValueKey = null)
		{ }

		/// <summary>
		/// Synchronous shortcut for <see cref="LoadAsync"/>.
		/// </summary>
		public void Load() => LoadAsync().GetAwaiter().GetResult();

		/// <summary>
		/// Synchronous shortcut for <see cref="SaveAsync"/>.
		/// </summary>
		public void Save(string changedValueKey = null) => SaveAsync(changedValueKey).GetAwaiter().GetResult();

		/// <summary>
		/// Returns an enumerator IDictionaryEnumerator that iterates through the entire settings set.
		/// </summary>
		/// <remarks>
		/// The enumerator is safe to use concurrently with reads and writes to the settings collection, 
		/// however it does not represent a moment-in-time snapshot.
		/// The contents exposed through the enumerator may contain modifications made 
		/// to the settings collection after GetEnumerator was called.
		/// </remarks>
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			PrepareEnumerator();
			return KeysAndValues.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator IDictionaryEnumerator that iterates through the entire settings set.
		/// </summary>
		/// <remarks>
		/// The enumerator is safe to use concurrently with reads and writes to the settings collection, 
		/// however it does not represent a moment-in-time snapshot.
		/// The contents exposed through the enumerator may contain modifications made 
		/// to the settings collection after GetEnumerator was called.
		/// </remarks>
		IEnumerator IEnumerable.GetEnumerator()
		{
			PrepareEnumerator();
			return KeysAndValues.GetEnumerator();
		}

		#region Tools

		/// <summary>
		/// Should return true for properties which are not settings and should not be seen when enumerating settings set.
		/// </summary>
		protected virtual bool IsInternalProperty(string name)
		{
			if (name == "Item") // -> public virtual object this[string key]
				return true;
			return false;
		}

		void PrepareEnumerator()
		{
			foreach (var p in this.GetType().GetRuntimeProperties())
			{
				if (!IsInternalProperty(p.Name) && !KeysAndValues.ContainsKey(p.Name))
				{
					try
					{
						object o = p.GetMethod?.Invoke(this, new object[] { });
						KeysAndValues[p.Name] = o;
					}
					catch { }
				}
			}
		}

		async Task SetAsync(object newValue, string key)
		{
			if (!KeysAndValues.TryGetValue(key, out object value))
				value = GetDefault(key);

			bool valueChanged = SetValue(ref value, newValue, key);

			KeysAndValues[key] = value;

			if (valueChanged)
				await SaveAsync(key);
		}

		object ConvertTo(object value, Type type, object def)
		{
			if (value == null)
				value = def;

			// TODO: przeniesc do ConvertEx

			Type valueType = value == null ? typeof(object) : value.GetType();
			// valueType = Nullable.GetUnderlyingType(valueType) ?? valueType;
			// valueType = valueType.GetTypeInfo().IsEnum ? Enum.GetUnderlyingType(valueType) : valueType;

			if (!type.Equals(valueType)
#if !NETSTANDARD1_6 && !__PCL__
				&& !type.IsAssignableFrom(valueType)
#endif
			)
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

		#endregion
	}
}
