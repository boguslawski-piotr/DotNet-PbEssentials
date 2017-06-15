using System;
using System.Security.Cryptography;
using System.Text;

namespace pbXNet {
	public class RSAServices
	{
		static RSA GetRSA()
		{
			RSA rsa = null;
			try
			{
				rsa = new RSACryptoServiceProvider();
			}
			catch (Exception ex)
			{
				rsa = RSA.Create();
			}

			return rsa;
		}

		public static void TestRSA()
		{
			RSA rsa = RSAServices.GetRSA();

			try
			{
				RSAParameters S_RSAParams_Public;
				RSAParameters S_RSAParams;
				RSAParameters C_RSAParams_Public;
				RSAParameters C_RSAParams;

				//Create a new RSACryptoServiceProvider object.
				//Export the key information to an RSAParameters object.
				//Pass false to export the public key information or pass
				//true to export public and private key information.

				// Server
				using (rsa = RSAServices.GetRSA())
				{
					S_RSAParams = rsa.ExportParameters(true);
					S_RSAParams_Public = rsa.ExportParameters(false);
				}

				// Client
				using (rsa = RSAServices.GetRSA())
				{
					C_RSAParams = rsa.ExportParameters(true);
					C_RSAParams_Public = rsa.ExportParameters(false);
				}

				//

				using (rsa = RSAServices.GetRSA())
				{
					// Client

					string data = "ala ma kota";
					byte[] bdata = Encoding.UTF8.GetBytes(data);

					rsa.ImportParameters(S_RSAParams_Public);
					byte[] bedata = rsa.Encrypt(bdata, RSAEncryptionPadding.Pkcs1);

					rsa.ImportParameters(C_RSAParams);
					byte[] bedata_sign = rsa.SignData(bedata, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

					//

					//bedata[6] = 3;

					// Server

					rsa.ImportParameters(C_RSAParams_Public);
					bool ok = rsa.VerifyData(bedata, bedata_sign, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

					rsa.ImportParameters(S_RSAParams);
					byte[] bddata = rsa.Decrypt(bedata, RSAEncryptionPadding.Pkcs1);

					string ddata = Encoding.UTF8.GetString(bddata);
				}
			}
			catch (CryptographicException e)
			{
				//Catch this exception in case the encryption did
				//not succeed.
				Log.E(e.Message);
			}
		}		
	}

	//
	// Sample from MSDN

	class MyMainClass
	{
		public static void Test()
		{
			byte[] toEncrypt;
			byte[] encrypted;
			byte[] signature;
			//Choose a small amount of data to encrypt.
			string original = "Hello";
			ASCIIEncoding myAscii = new ASCIIEncoding();

			//Create a sender and receiver.
			Sender mySender = new Sender();
			Receiver myReceiver = new Receiver();

			//Convert the data string to a byte array.
			toEncrypt = myAscii.GetBytes(original);

			//Encrypt data using receiver's public key.
			encrypted = mySender.EncryptData(myReceiver.PublicParameters, toEncrypt);

			//Hash the encrypted data and generate a signature on the hash
			// using the sender's private key.
			signature = mySender.HashAndSign(encrypted);

			Console.WriteLine("Original: {0}", original);

			//Verify the signature is authentic using the sender's public key.
			if (myReceiver.VerifyHash(mySender.PublicParameters, encrypted, signature))
			{
				//Decrypt the data using the receiver's private key.
				myReceiver.DecryptData(encrypted);
			}
			else
			{
				Console.WriteLine("Invalid signature");
			}
		}
	}

	class Sender
	{
		RSAParameters rsaPubParams;
		RSAParameters rsaPrivateParams;

		public Sender()
		{
			RSACryptoServiceProvider rsaCSP = new RSACryptoServiceProvider();

			//Generate public and private key data.
			rsaPrivateParams = rsaCSP.ExportParameters(true);
			rsaPubParams = rsaCSP.ExportParameters(false);
		}

		public RSAParameters PublicParameters
		{
			get {
				return rsaPubParams;
			}
		}

		//Manually performs hash and then signs hashed value.
		public byte[] HashAndSign(byte[] encrypted)
		{
			RSACryptoServiceProvider rsaCSP = new RSACryptoServiceProvider();
			SHA256 hash = SHA256.Create();
			byte[] hashedData;

			rsaCSP.ImportParameters(rsaPrivateParams);

			hashedData = hash.ComputeHash(encrypted);
			return rsaCSP.SignHash(hashedData, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		}

		//Encrypts using only the public key data.
		public byte[] EncryptData(RSAParameters rsaParams, byte[] toEncrypt)
		{
			RSACryptoServiceProvider rsaCSP = new RSACryptoServiceProvider();

			rsaCSP.ImportParameters(rsaParams);
			return rsaCSP.Encrypt(toEncrypt, false);
		}
	}

	class Receiver
	{
		RSAParameters rsaPubParams;
		RSAParameters rsaPrivateParams;

		public Receiver()
		{
			RSACryptoServiceProvider rsaCSP = new RSACryptoServiceProvider();

			//Generate public and private key data.
			rsaPrivateParams = rsaCSP.ExportParameters(true);
			rsaPubParams = rsaCSP.ExportParameters(false);
		}

		public RSAParameters PublicParameters
		{
			get {
				return rsaPubParams;
			}
		}

		//Manually performs hash and then verifies hashed value.
		public bool VerifyHash(RSAParameters rsaParams, byte[] signedData, byte[] signature)
		{
			RSACryptoServiceProvider rsaCSP = new RSACryptoServiceProvider();
			SHA256 hash = SHA256.Create();
			byte[] hashedData;

			rsaCSP.ImportParameters(rsaParams);
			bool dataOK = rsaCSP.VerifyData(signedData, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			hashedData = hash.ComputeHash(signedData);
			return rsaCSP.VerifyHash(hashedData, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		}

		//Decrypt using the private key data.
		public void DecryptData(byte[] encrypted)
		{
			byte[] fromEncrypt;
			string roundTrip;
			ASCIIEncoding myAscii = new ASCIIEncoding();
			RSACryptoServiceProvider rsaCSP = new RSACryptoServiceProvider();

			rsaCSP.ImportParameters(rsaPrivateParams);
			fromEncrypt = rsaCSP.Decrypt(encrypted, false);
			roundTrip = myAscii.GetString(fromEncrypt);

			Console.WriteLine("RoundTrip: {0}", roundTrip);
		}
	}
}