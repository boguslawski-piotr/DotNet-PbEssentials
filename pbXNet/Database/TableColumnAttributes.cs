using System;

namespace pbXNet.Database
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class PrimaryKeyAttribute : Attribute
	{ }

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class IndexAttribute : Attribute
	{
		public IndexAttribute(string name, bool desc = false)
		{ }
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class NotNullAttribute : Attribute
	{ }

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
	public class LengthAttribute : Attribute
	{
		public LengthAttribute(int width)
		{ }
	}
}
