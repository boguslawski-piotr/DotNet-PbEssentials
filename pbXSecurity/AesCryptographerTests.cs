using System;
using System.Diagnostics;
using System.Text;

//using NUnit.Framework;
//using NUnit.Framework.Constraints;

namespace pbXSecurity
{
    //[TestFixture]
    public class AesCryptographerTests /*: BaseTestFixture*/
    {
        //[Test]
        public void BasicEncryptDecrypt()
        {
            AesCryptographer C = new AesCryptographer();

            string spwd = "ala ma kota";
            byte[] pwd = Encoding.UTF8.GetBytes(spwd);
            byte[] salt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            //

            byte[] key = C.GenerateKey(pwd, salt);
            string skey = Convert.ToBase64String(key);
            // Android: tcpMMu4qUiMVQxWsnUcOq29pY2dfzQfuNfwo2NKzxZY=
            // UWP:     tcpMMu4qUiMVQxWsnUcOq29pY2dfzQfuNfwo2NKzxZY=

            //

            byte[] iv = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6 };

            string smsg = "jakis test do zaszyfrowania, ąęśćłó, !@#$%^&*()_+, jakis test do zaszyfrowania, jakis test do zaszyfrowania, ąęśćłó, jakis test do zaszyfrowania";
            byte[] msg = Encoding.UTF8.GetBytes(smsg);

            byte[] msgEncrypted = C.Encrypt(msg, key, iv);
            string smsgEncrypted = Convert.ToBase64String(msgEncrypted);
            // Android: EXqP/U0PP7YObOnE1U/Osbev9GhdscV3tuE1feN6gZo/tlxh5vvay3HF/kXwxRonsy3QMAhZRSdnJF7a66ic2G3gRHVM/THR9PkkVaWb3jUDK2X65cdNSRu0e7nqHDoTJol9oDlBUgufmfBtifdg0Q7UhJG416fZ6V+WCWa1ttVAdzS4Asgf/XLlw9XKJQFl0sAMJUlmELq8IAezSDkeOQ==
            // UWP:     EXqP/U0PP7YObOnE1U/Osbev9GhdscV3tuE1feN6gZo/tlxh5vvay3HF/kXwxRonsy3QMAhZRSdnJF7a66ic2G3gRHVM/THR9PkkVaWb3jUDK2X65cdNSRu0e7nqHDoTJol9oDlBUgufmfBtifdg0Q7UhJG416fZ6V+WCWa1ttVAdzS4Asgf/XLlw9XKJQFl0sAMJUlmELq8IAezSDkeOQ==

            string smsgEncrypted2 = "EXqP/U0PP7YObOnE1U/Osbev9GhdscV3tuE1feN6gZo/tlxh5vvay3HF/kXwxRonsy3QMAhZRSdnJF7a66ic2G3gRHVM/THR9PkkVaWb3jUDK2X65cdNSRu0e7nqHDoTJol9oDlBUgufmfBtifdg0Q7UhJG416fZ6V+WCWa1ttVAdzS4Asgf/XLlw9XKJQFl0sAMJUlmELq8IAezSDkeOQ==";
            byte[] msgEncrypted2 = Convert.FromBase64String(smsgEncrypted2);

            byte[] msgDecrypted = C.Decrypt(msgEncrypted2, key, iv);
            string smsgDecrypted = Encoding.UTF8.GetString(msgDecrypted, 0, msgDecrypted.Length);

            //Assert.AreEqual(smsg, msgDecrypted);
            Debug.Assert(smsg == smsgDecrypted, "CryptographerTests.BasicEncryptDecrypt");
        }
    }
}
