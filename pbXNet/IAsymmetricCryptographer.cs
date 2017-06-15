using System.Threading.Tasks;

namespace pbXNet
{
	public interface IAsymmetricCryptographer
	{
		(IByteBuffer pbl, IByteBuffer prv) GenerateKeyPair(int length = -1);

		IByteBuffer Encrypt(IByteBuffer msg, IByteBuffer pblKey);

		IByteBuffer Decrypt(IByteBuffer msg, IByteBuffer prvKey);

		IByteBuffer Sign(IByteBuffer msg, IByteBuffer prvKey);

		bool Verify(IByteBuffer msg, IByteBuffer signature, IByteBuffer pblKey);
	}
}
