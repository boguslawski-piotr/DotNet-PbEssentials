using System;

namespace pbXNet
{
	public interface ISerializer
	{
		string ToString(object o, string id = null);
		T FromString<T>(string d, string id = null);
	}
}
