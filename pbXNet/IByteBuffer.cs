using System;

namespace pbXNet
{
	public interface IByteBuffer : IDisposable
	{
		int Length { get; }

		byte[] GetBytes();

		void DisposeBytes();

		string ToHexString();
	}
}