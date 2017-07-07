using System;

namespace pbXNet
{
	public interface ISerializer
	{
		string Serialize<T>(T o, string id = null);
		T Deserialize<T>(string d, string id = null);
	}
}
