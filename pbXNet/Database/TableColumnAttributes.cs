using System;

namespace pbXNet.Database
{
	[AttributeUsage(AttributeTargets.Property)]
	public class PrimaryKeyAttribute : Attribute
	{ }

	[AttributeUsage(AttributeTargets.Property)]
	public class IndexAttribute : Attribute
	{
		public IndexAttribute(string name, bool desc = false)
		{ }
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class NotNullAttribute : Attribute
	{ }

	[AttributeUsage(AttributeTargets.Property)]
	public class LengthAttribute : Attribute
	{
		public LengthAttribute(int width)
		{ }
	}
}
