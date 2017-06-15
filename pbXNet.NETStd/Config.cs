using System;

namespace pbXNet
{
	[AttributeUsageAttribute(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate, Inherited = false)]
	public sealed class SerializableAttribute : Attribute { }
}
