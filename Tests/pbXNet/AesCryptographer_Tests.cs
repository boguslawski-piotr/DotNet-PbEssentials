using System;
using System.Diagnostics;
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
			byte[] msg = Encoding.UTF8.GetBytes(smsg);

			string spwd = "ala ma kota";
			byte[] pwd = Encoding.UTF8.GetBytes(spwd);

			byte[] salt = new byte[] { 0x43, 0x87, 0x23, 0x72, 0x45, 0x56, 0x68, 0x14, 0x62, 0x84 };

			//

			AesCryptographer C = new AesCryptographer();
			byte[] iv = C.GenerateIV();

			//

			byte[] key = C.GenerateKey(pwd, salt);
			string skey = Convert.ToBase64String(key);
			byte[] msgEncrypted = C.Encrypt(msg, key, iv);
			string smsgEncrypted = Convert.ToBase64String(msgEncrypted);

			//

			byte[] dkey = C.GenerateKey(pwd, salt);
			string sdkey = Convert.ToBase64String(dkey);

			byte[] msgEncrypted2 = Convert.FromBase64String(smsgEncrypted);

			byte[] msgDecrypted = C.Decrypt(msgEncrypted2, dkey, iv);
			string smsgDecrypted = Encoding.UTF8.GetString(msgDecrypted, 0, msgDecrypted.Length);

			//

			Assert.AreEqual(smsg, smsgDecrypted);
			//Assert.True(smsg == smsgDecrypted);
		}
	}
}
