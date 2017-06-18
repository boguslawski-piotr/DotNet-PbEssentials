using System;
using System.Collections.Generic;
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

		public void Initialize(object param)
		{
			_Initialize(param);
		}

		// Basic device owner authentication (pin, passkey, biometrics, etc.)
		// You will find the implementation in the platform directories...

		public DOAuthentication AvailableDOAuthentication
		{
			get => _AvailableDOAuthentication;
		}

		public bool StartDOAuthentication(string msg, Action Succes, Action<string, bool> ErrorOrHint)
		{
			return _StartDOAuthentication(msg, Succes, ErrorOrHint);
		}

		public bool CanDOAuthenticationBeCanceled()
		{
			return _CanDOAuthenticationBeCanceled();
		}

		public bool CancelDOAuthentication()
		{
			return _CancelDOAuthentication();
		}

		// Basic authentication based on passwords
		// No password is ever written anywhere

		[Serializable]
		class EncryptedPassword
		{
			public string iv;
			public string data;
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

		public void AddOrUpdatePassword(string id, IPassword passwd)
		{
			if (id == null)
				return;

			LoadPasswords();

			EncryptedPassword epasswd;
			if (!_passwords.TryGetValue(id, out epasswd))
			{
				epasswd = new EncryptedPassword()
				{
					iv = new SecureBuffer(_cryptographer.GenerateIV(), true).ToHexString(),
				};
			}

			IByteBuffer ckey = _cryptographer.GenerateKey(passwd, new ByteBuffer(_salt));
			epasswd.data = _cryptographer.Encrypt(new ByteBuffer(_phrase, Encoding.UTF8), ckey, SecureBuffer.NewFromHexString(epasswd.iv)).ToHexString();
			ckey.Dispose();

			_passwords[id] = epasswd;

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

		public bool ComparePassword(string id, IPassword passwd)
		{
			LoadPasswords();

			EncryptedPassword epasswd;
			if (!_passwords.TryGetValue(id, out epasswd))
				return false;

			IByteBuffer ckey = _cryptographer.GenerateKey(passwd, new ByteBuffer(_salt));
			ByteBuffer ddata = _cryptographer.Decrypt(ByteBuffer.NewFromHexString(epasswd.data), ckey, SecureBuffer.NewFromHexString(epasswd.iv));
			ckey.Dispose();

			return ddata.Equals(new ByteBuffer(_phrase, Encoding.UTF8));
		}


		// Cryptographic keys, encryption and decryption

		[Serializable]
		class TemporaryCKey
		{
			public CKeyLifeTime lifeTime;
			public string ckey;
		}

		IDictionary<string, string> _ckeys = new Dictionary<string, string>();
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
				_ckeys = _serializer.FromString<IDictionary<string, string>>(d);
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

		public void CreateCKey(string id, CKeyLifeTime lifeTime, IPassword passwd)
		{
			if (id == null)
				return;

			IByteBuffer ckeyb = new SecureBuffer(_cryptographer.GenerateKey(passwd, new ByteBuffer(_salt)), true);
			string ckey = ckeyb.ToHexString();
			ckeyb.Dispose();

			if (lifeTime == CKeyLifeTime.Infinite)
			{
				LoadCKeys();

				_ckeys[id] = ckey;

				SaveCKeys();
			}
			else
			{
				_temporaryCKeys[id] = new TemporaryCKey() { lifeTime = lifeTime, ckey = ckey };
			}
		}

		public bool CKeyExists(string id)
		{
			if (_temporaryCKeys.ContainsKey(id))
				return true;

			LoadCKeys();

			return _ckeys.ContainsKey(id);
		}

		public void DeleteCKey(string id)
		{
			if (_temporaryCKeys.ContainsKey(id))
			{
				_temporaryCKeys.Remove(id);
			}

			LoadCKeys();

			if (_ckeys.ContainsKey(id))
			{
				_ckeys.Remove(id);
				SaveCKeys();
			}
		}

		IByteBuffer GetCKey(string id)
		{
			if (_temporaryCKeys.TryGetValue(id, out TemporaryCKey ckey))
				return SecureBuffer.NewFromHexString(ckey.ckey);

			LoadCKeys();

			ckey = new TemporaryCKey();
			if (_ckeys.TryGetValue(id, out ckey.ckey))
				return SecureBuffer.NewFromHexString(ckey.ckey);

			return null;
		}

		public IByteBuffer GenerateIV()
		{
			return _cryptographer.GenerateIV();
		}

		public string Encrypt(string data, string id, IByteBuffer iv)
		{
			IByteBuffer ckey = GetCKey(id);
			ByteBuffer edata = _cryptographer.Encrypt(new ByteBuffer(data, Encoding.UTF8), ckey, iv);
			ckey.Dispose();
			return edata.ToHexString();
		}

		public string Decrypt(string data, string id, IByteBuffer iv)
		{
			IByteBuffer ckey = GetCKey(id);
			ByteBuffer ddata = _cryptographer.Decrypt(ByteBuffer.NewFromHexString(data), ckey, iv);
			ckey.Dispose();
			return ddata.ToString(Encoding.UTF8);
		}
	}
}
