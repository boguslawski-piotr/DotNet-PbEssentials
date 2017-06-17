using System;
using System.Text;
using NUnit.Framework;

namespace pbXNet.Tests
{
	[TestFixture]
	public class AesCryptographer_Tests
	{
		[Test]
		public void BasicEncryptDecrypt()
		{
			string smsg = "jakis test do zaszyfrowania, ąęśćłó, !@#$%^&*()_+, jakis test do zaszyfrowania, jakis test do zaszyfrowania, ąęśćłó, jakis test do zaszyfrowania";
			ByteBuffer msg = new ByteBuffer(smsg, Encoding.UTF8);

			string spwd = "ala ma kota";
			Password pwd = new Password(spwd);

			ByteBuffer salt = new ByteBuffer(new byte[] { 0x43, 0x87, 0x23, 0x72, 0x45, 0x56, 0x68, 0x14, 0x62, 0x84 });

			//

			AesCryptographer C = new AesCryptographer();
			IByteBuffer iv = C.GenerateIV();

			//

			IByteBuffer ekey = C.GenerateKey(pwd, salt);
			string sekey = ekey.ToHexString();
			ByteBuffer emsg = C.Encrypt(msg, ekey, iv);

			//

			IByteBuffer dkey = C.GenerateKey(pwd, salt);
			string sdkey = dkey.ToHexString();
			ByteBuffer dmsg = C.Decrypt(emsg, dkey, iv);
			string sdmsg = dmsg.ToString(Encoding.UTF8);

			//

			Assert.AreEqual(ekey.GetBytes(), dkey.GetBytes(), "key");
			Assert.AreEqual(sekey, sdkey, "skey");

			Assert.AreEqual(msg, dmsg, "msg");
			Assert.AreEqual(smsg, sdmsg, "smsg");
		}
	}
}
