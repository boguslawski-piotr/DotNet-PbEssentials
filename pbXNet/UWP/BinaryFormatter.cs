using System;
using System.IO;

namespace pbXNet
{
	public interface IFormatter
	{
		void Serialize(Stream serializationStream, object graph);
		object Deserialize(Stream serializationStream);
	}

	public class BinaryFormatter : IFormatter
	{
		public object Deserialize(Stream serializationStream)
		{
			throw new NotImplementedException();
		}

		public void Serialize(Stream serializationStream, object graph)
		{
			throw new NotImplementedException();
		}
	}
}
