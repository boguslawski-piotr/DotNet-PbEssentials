using System;

namespace pbXNet
{
	public class OptimizedForStringSerializer : ISerializer
	{
		ISerializer _serializer;

		public OptimizedForStringSerializer(ISerializer serializer)
		{
			_serializer = serializer;
		}

		public virtual string Serialize<T>(T data, string id = null)
		{
			string d;
			if (id == null && typeof(T).Equals(typeof(string)))
				d = data.ToString();
			else
				d = _serializer.Serialize(data, id);

			return d;
		}

		public virtual T Deserialize<T>(string d, string id = null)
		{
			if (id == null && typeof(T).Equals(typeof(string)))
				return (T)(object)d;
			else
				return _serializer.Deserialize<T>(d, id);
		}
	}
}
