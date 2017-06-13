using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pbXNet
{
	public sealed partial class SecretsManager : ISecretsManager
	{
		public string Id { get; }

		ICryptographer _cryptographer { get; }

		IStorage<string> _storage { get; }

		ISerializer _serializer { get; }

		public SecretsManager(string id, ICryptographer cryptographer, IStorage<string> storage = null, ISerializer serializer = null)
		{
			Id = id;
			_cryptographer = cryptographer;
			_storage = storage;
			_serializer = serializer;
		}


		// Basic device owner authentication (pin, passkey, biometrics, etc.)
		// You will find the implementation in the platform directories...


		// Basic authentication based on passwords
		// No password is ever written anywhere

		// TODO: jakas lepsza nazwa(y)
		//[Serializable]
		class EncryptedPassword
		{
			public byte[] iv;
			public byte[] data;
		};

		IDictionary<string, EncryptedPassword> _passwords = new Dictionary<string, EncryptedPassword>();

		// TODO: Exceptions/Errors handling

		const string _passwordsDataId = ".12b77038229d4f8f941b8465cb132c03";

		void LoadPasswords()
		{
			if (_passwords.Count > 0 || _storage == null || _serializer == null)
				return;

			// TODO: async as sync -> sprawdzic wyjatki i czy na pewno jest to ok rozwiazanie
			string d = Task.Run(async () => await _storage.GetACopyAsync(_passwordsDataId)).GetAwaiter().GetResult();
			//string d = await _storage.GetACopyAsync(_passwordsDataId);
			if (!string.IsNullOrEmpty(d))
			{
				d = Obfuscator.DeObfuscate(d);
				_passwords = _serializer.FromString<Dictionary<string, EncryptedPassword>>(d);
			}
		}

		void SavePasswords()
		{
			if (_storage == null || _serializer == null)
				return;

			string d = _serializer.ToString(_passwords);
			d = Obfuscator.Obfuscate(d);

			_storage.StoreAsync(_passwordsDataId, d, DateTime.UtcNow);
		}

		const string _phrase = "Life is short. Smile while you still have teeth :)";
		readonly byte[] _salt = { 0x43, 0x87, 0x23, 0x72, 0x45, 0x56, 0x68, 0x14, 0x62, 0x84 };

		public bool PasswordExists(string id)
		{
			LoadPasswords();
			return _passwords.ContainsKey(id);
		}

		public void AddOrUpdatePassword(string id, Password passwd)
		{
			if (id == null)
				return;

			LoadPasswords();

			EncryptedPassword _password;
			if (!_passwords.TryGetValue(id, out _password))
			{
				_password = new EncryptedPassword()
				{
					iv = _cryptographer.GenerateIV()
				};
			}

			byte[] ckey = _cryptographer.GenerateKey(passwd, _salt);
			_password.data = _cryptographer.Encrypt(Encoding.UTF8.GetBytes(_phrase), ckey, _password.iv);
			ckey.FillWithDefault();

			_passwords[id] = _password;

			SavePasswords();
		}

		public void DeletePassword(string id)
		{
			if (PasswordExists(id))
			{
				_passwords.Remove(id);
				SavePasswords();
			}
		}

		public bool ComparePassword(string id, Password passwd)
		{
			LoadPasswords();

			EncryptedPassword _password;
			if (!_passwords.TryGetValue(id, out _password))
				return false;

			byte[] ckey = _cryptographer.GenerateKey(passwd, _salt);
			byte[] ddata = _cryptographer.Decrypt(_password.data, ckey, _password.iv);
			ckey.FillWithDefault();

			return ddata.SequenceEqual(Encoding.UTF8.GetBytes(_phrase));
		}


		// Cryptographic keys, encryption and decryption

		//[Serializable]
		class TemporaryCKey
		{
			public CKeyLifeTime lifeTime;
			public byte[] ckey;
		}

		IDictionary<string, byte[]> _ckeys = new Dictionary<string, byte[]>();
		IDictionary<string, TemporaryCKey> _temporaryCKeys = new Dictionary<string, TemporaryCKey>();

		const string _ckeysDataId = ".d46ee950276f4665aefa06cb2ee6b35e";

		void LoadCKeys()
		{
			if (_ckeys.Count > 0 || _storage == null || _serializer == null)
				return;

			// TODO: async as sync -> sprawdzic wyjatki i czy na pewno jest to ok rozwiazanie
			string d = Task.Run(async () => await _storage.GetACopyAsync(_ckeysDataId)).GetAwaiter().GetResult();
			//string d = await _storage.GetACopyAsync(_ckeysDataId);
			if (!string.IsNullOrEmpty(d))
			{
				d = Obfuscator.DeObfuscate(d);
				_ckeys = _serializer.FromString<IDictionary<string, byte[]>>(d);
			}
		}

		void SaveCKeys()
		{
			if (_storage == null || _serializer == null)
				return;

			string d = _serializer.ToString(_ckeys);
			d = Obfuscator.Obfuscate(d);

			// TODO: dodac szyfrowanie; haslem powinno byc cos co mozna pobrac z systemu, jest niezmienne i nie da sie wyczytac z kodu programu bez doglebnego debugowania

			_storage.StoreAsync(_ckeysDataId, d, DateTime.UtcNow);
		}

		public byte[] CreateCKey(string id, CKeyLifeTime lifeTime, Password passwd)
		{
			if (id == null)
				return null;

			byte[] ckey = _cryptographer.GenerateKey(passwd, _salt);

			if (lifeTime == CKeyLifeTime.Infinite)
			{
				LoadCKeys();

				_ckeys[id] = ckey;

				SaveCKeys();
			}
			else
			{
				if (lifeTime != CKeyLifeTime.OneTime)
					_temporaryCKeys[id] = new TemporaryCKey() { lifeTime = lifeTime, ckey = ckey };
			}

			return ckey;
		}

		public byte[] GetCKey(string id)
		{
			if (_temporaryCKeys.TryGetValue(id, out TemporaryCKey ckey))
				return ckey.ckey;

			LoadCKeys();

			ckey = new TemporaryCKey();
			if (_ckeys.TryGetValue(id, out ckey.ckey))
				return ckey.ckey;

			return null;
		}

		public void DeleteCKey(string id)
		{
			if (_temporaryCKeys.ContainsKey(id))
			{
				_temporaryCKeys[id].ckey?.FillWithDefault();
				_temporaryCKeys.Remove(id);
			}

			LoadCKeys();

			if (_ckeys.ContainsKey(id))
			{
				_ckeys[id]?.FillWithDefault();
				_ckeys.Remove(id);
				SaveCKeys();
			}
		}

		public string Encrypt(string data, byte[] ckey, byte[] iv)
		{
			byte[] edata = _cryptographer.Encrypt(Encoding.UTF8.GetBytes(data), ckey, iv);
			return ConvertEx.ToHexString(edata);
		}

		public string Decrypt(string data, byte[] ckey, byte[] iv)
		{
			byte[] ddata = _cryptographer.Decrypt(data.FromHexString(), ckey, iv);
			return Encoding.UTF8.GetString(ddata, 0, ddata.Length);
		}

		public byte[] GenerateIV()
		{
			return _cryptographer.GenerateIV();
		}
	}
}
