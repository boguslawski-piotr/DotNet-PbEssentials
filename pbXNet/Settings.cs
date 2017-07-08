using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace pbXNet
{
	/// <summary>
	/// <para>
	///		A class that allows you to conveniently define different types of settings, 
	///		more precisely, any value that is accessed through a key.
	/// </para>
	/// <para>
	///		The class is fully ready for use in binding systems because it inherits after <see cref="Observable"/>.
	/// </para>
	/// </summary>
	/// <example>
	/// <code>
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
	///			// Load your settings the way you like...
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
	/// <seealso cref="PlatformSettings"/>
	/// <seealso cref="SettingsInStorage"/>
	public class Settings : Observable
	{
		protected ConcurrentDictionary<string, object> KeysAndValues = new ConcurrentDictionary<string, object>();

		[AttributeUsage(AttributeTargets.Property)]
		public class DefaultAttribute : Attribute
		{
			public DefaultAttribute(object defaultValue)
			{ }
		}

		public virtual object Get([CallerMemberName]string key = null) => this[key];

		public virtual T Get<T>([CallerMemberName]string key = null) => (T)this[key];

		public virtual void Set(object value, [CallerMemberName]string key = null) => this[key] = value;

		public virtual void Set<T>(T value, [CallerMemberName]string key = null) => this[key] = value;

		public virtual object this[string key]
		{
			get {
				if (!KeysAndValues.TryGetValue(key, out object value))
					value = GetDefault(key);

				try
				{
					PropertyInfo property = this.GetType().GetRuntimeProperties().First((_p) => _p.Name == key);

					Type valueType = value.GetType();
					Type propertyType = property.PropertyType;

					if (!propertyType.Equals(valueType)
#if !NETSTANDARD1_6
						&& !propertyType.IsAssignableFrom(valueType)
#endif
					   )
					{
						if (propertyType == typeof(bool)) return Convert.ToBoolean(value);
						else if (propertyType == typeof(char)) return Convert.ToChar(value);
						else if (propertyType == typeof(string)) return Convert.ToString(value);
						else if (propertyType == typeof(DateTime)) return Convert.ToDateTime(value);
						else if (propertyType == typeof(sbyte)) return Convert.ToSByte(value);
						else if (propertyType == typeof(byte)) return Convert.ToByte(value);
						else if (propertyType == typeof(short)) return Convert.ToInt16(value);
						else if (propertyType == typeof(int)) return Convert.ToInt32(value);
						else if (propertyType == typeof(long)) return Convert.ToInt64(value);
						else if (propertyType == typeof(ushort)) return Convert.ToUInt16(value);
						else if (propertyType == typeof(uint)) return Convert.ToUInt32(value);
						else if (propertyType == typeof(ulong)) return Convert.ToUInt64(value);
						else if (propertyType == typeof(float)) return Convert.ToSingle(value);
						else if (propertyType == typeof(double)) return Convert.ToDouble(value);
						else if (propertyType == typeof(decimal)) return Convert.ToDecimal(value);
						else
							return Activator.CreateInstance(property.PropertyType, value);
					}

				}
				catch { }

				return value;
			}

			set {
				SetAsync(value, key);
			}
		}

		public virtual bool Contains(string key)
		{
			return KeysAndValues.ContainsKey(key);
		}

		public virtual void Remove(string key)
		{
			if (KeysAndValues.TryRemove(key, out object _))
				SaveAsync(null);
		}

		public virtual void Clear()
		{
			KeysAndValues.Clear();
			SaveAsync(null);
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

		// At first glance it looks like functions below should be abstract 
		// but then to use the settings as a fast container for temporary data 
		// would require creating a new class and inheriting.
		// That is why (for convenience) these functions are not abstract ;).

		public virtual async Task LoadAsync()
		{ }

		public virtual async Task SaveAsync(string changedValueKey = null)
		{ }
	}
}
