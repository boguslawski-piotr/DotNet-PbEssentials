using System;

namespace pbXNet
{
	public interface IByteBuffer : IDisposable 
	{
		uint Length { get; }
		byte[] GetBytes();
		void DisposeBytes();
	}
}