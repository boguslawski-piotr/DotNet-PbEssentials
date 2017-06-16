using System;
using System.Collections;
using System.Collections.Generic;

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