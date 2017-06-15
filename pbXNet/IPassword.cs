using System;

namespace pbXNet
{
	public interface IPassword : IDisposable 
	{
		int Length { get; }

		byte[] GetBytes();
		void DisposeBytes();

		void Append(char c);
		void RemoveLast();

		// Implementation should always implement (override) Equals(object) (!)
	}
}