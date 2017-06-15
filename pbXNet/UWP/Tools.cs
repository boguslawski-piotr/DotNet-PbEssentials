#if WINDOWS_UWP

using System;

namespace pbXNet
{
	public static partial class Tools
	{
		static string _Uaqpid
		{
			get {
				// TODO: _Uaqpid for UWP
				string id = "b3fea4b6-0f44-466e-96e0-ba25324671fc";
				string id2 = "UWP";
				return id + id2;
			}
		}
	}

	[AttributeUsageAttribute(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
	public sealed class SerializableAttribute : Attribute { }
}

#endif