using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace pbXNet
{
	public class NewtonsoftJsonSerializer : ISerializer
	{
		JsonSerializerSettings _settings;

		/// <summary>
		/// Settings to use with <see cref="NewtonsoftJsonSerializer.Serialize{T}"/>and <see cref="NewtonsoftJsonSerializer.Deserialize{T}"/> methods.
		/// </summary>
		public virtual JsonSerializerSettings Settings
		{
			get {
				if (_settings == null)
				{
					_settings = new JsonSerializerSettings()
					{
						DateTimeZoneHandling = DateTimeZoneHandling.Utc,
						NullValueHandling = NullValueHandling.Ignore,
#if DEBUG
						Formatting = Formatting.Indented,
#else
						Formatting = Formatting.None,
#endif
					};
				}
				return _settings;
			}

			set {
				_settings = value;
			}
		}

		/// <summary>
		/// Serializes object <typeparamref name="T"/> <paramref name="o"/> to json string and optionally giving it a given <paramref name="id"/>.
		/// </summary>
		public string Serialize<T>(T o, string id = null)
		{
			string d = JsonConvert.SerializeObject(o, Settings);
			if (id != null)
			{
				d = "'" + id + "':" + d + ",";
			}
			return d;
		}

		/// <summary>
		/// Deserializes <typeparamref name="T"/> object with optional <paramref name="id"/> from json string <paramref name="d"/>.
		/// </summary>
		public T Deserialize<T>(string d, string id = null)
		{
			if (id != null)
			{
				JObject pd = JObject.Parse("{" + d + "}");
				d = pd[id].ToString();
			}

			var o = JsonConvert.DeserializeObject<T>(d, Settings);
			return o;
		}

	}
}
