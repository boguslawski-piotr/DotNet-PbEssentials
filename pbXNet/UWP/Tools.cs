#if WINDOWS_UWP

using System;
using System.IO;

namespace pbXNet
{
	public static partial class Tools
	{
		static string _Uaqpid
		{
			get {
				// TODO: DeviceEx.Id for UWP
				string id = "b3fea4b6-0f44-466e-96e0-ba25324671fc";
				string id2 = "UWP";
				return id + id2;
			}
		}
	}

	//[AttributeUsageAttribute(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
	//public sealed class SerializableAttribute : Attribute { }

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

#endif