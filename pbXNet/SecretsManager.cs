using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace pbXNet
{
	public sealed class SecretsManager : ISecretsManager, IDisposable
	{
		public string Id { get; }

		ICryptographer _cryptographer = new AesCryptographer();

		ISerializer _serializer;

		IStorage<string> _storage;

		/// IMPORTANT NOTE: 
		/// Passed passwd is completely cleaned (zeros) as soon as possible.
		/// You should not use this data anymore after it was passed to the EncryptedFileSystem class constructor.
		public SecretsManager(string id, ISerializer serializer = null, IStorage<string> storage = null, IPassword passwd = null)
		{
			if (id == null)
				throw new ArgumentNullException(nameof(id));

			Id = id;
			_serializer = new StringOptimizedSerializer(serializer ?? new NewtonsoftJsonSerializer());
			_storage = storage;
			_CreateCKey(passwd);
		}

		// Common code for passwords, ckeys and other secrets

		[Serializable]
		class TemporarySecret
		{
			public SecretLifeTime lifeTime;
			public string secret;
		}

		readonly byte[] _salt = { 0x43, 0x87, 0x23, 0x72, 0x45, 0x56, 0x68, 0x14, 0x62, 0x84 };

		IDictionary<string, string> _secrets = new Dictionary<string, string>();

		IDictionary<string, TemporarySecret> _temporarySecrets = new Dictionary<string, TemporarySecret>();

		const string _secretsDataId = ".d46ee950276f4665aefa06cb2ee6b35e";

		SecureBuffer _secretsDataIV = SecureBuffer.NewFromHexString("F36A0B7F6D95F16089EBB4A3EC196F0D960300");

		IByteBuffer _secretsDataCKey;

		void _CreateCKey(IPassword passwd = null)
		{
			if (passwd != null)
			{
				_secretsDataCKey = _cryptographer.GenerateKey(passwd, new ByteBuffer(_salt));
				passwd.Dispose();
			}
		}

		void _LoadSecrets()
		{
			if (_secrets.Count > 0 || _storage == null)
				return;

			// TODO: async as sync -> sprawdzic wyjatki i czy na pewno jest to ok rozwiazanie
			string d = Task.Run(async () => await _storage.GetACopyAsync(_secretsDataId)).GetAwaiter().GetResult();
			//string d = await _storage.GetACopyAsync(_ckeysDataId);

			if (!string.IsNullOrEmpty(d))
			{
				if (_secretsDataCKey != null)
					d = _cryptographer.Decrypt(ByteBuffer.NewFromHexString(d), _secretsDataCKey, _secretsDataIV).ToString(Encoding.UTF8);

				d = Obfuscator.DeObfuscate(d);

				_secrets = _serializer.Deserialize<IDictionary<string, string>>(d);
			}
		}

		void _SaveSecrets()
		{
			if (_storage == null)
				return;

			string d = _serializer.Serialize(_secrets);

			d = Obfuscator.Obfuscate(d);

			if (_secretsDataCKey != null)
				d = _cryptographer.Encrypt(new ByteBuffer(d, Encoding.UTF8), _secretsDataCKey, _secretsDataIV).ToHexString();

			_storage.StoreAsync(_secretsDataId, d, DateTime.UtcNow);
		}

		void _AddOrUpdateSecret(string id, SecretLifeTime lifeTime, string s)
		{
			s = Obfuscator.Obfuscate(s);

			if (lifeTime == SecretLifeTime.Infinite)
			{
				if (_storage == null)
					throw new ArgumentException(T.Localized("SM_StorageNotProvided"));

				_LoadSecrets();
				_secrets[id] = s;
				_SaveSecrets();
			}
			else
			{
				_temporarySecrets[id] = new TemporarySecret()
				{
					lifeTime = lifeTime,
					secret = s
				};
			}
		}

		bool _SecretExists(string id)
		{
			if (_temporarySecrets.ContainsKey(id))
				return true;

			_LoadSecrets();
			return _secrets.ContainsKey(id);
		}

		string _GetSecret(string id)
		{
			string __GetSecret()
			{
				if (_temporarySecrets.TryGetValue(id, out TemporarySecret s))
					return s.secret;

				_LoadSecrets();

				s = new TemporarySecret();
				if (_secrets.TryGetValue(id, out s.secret))
					return s.secret;

				throw new KeyNotFoundException(T.Localized("SM_KeyNotFound", id));
			}

			return Obfuscator.DeObfuscate(__GetSecret());
		}

		void _DeleteSecret(string id)
		{
			if (_temporarySecrets.ContainsKey(id))
				_temporarySecrets.Remove(id);

			_LoadSecrets();
			if (_secrets.ContainsKey(id))
			{
				_secrets.Remove(id);
				_SaveSecrets();
			}
		}

		// Basic authentication based on passwords
		// No password is ever written anywhere

		[Serializable]
		class EncryptedPassword
		{
			public string iv;
			public string data;
		};

		const string _phrase = "Life is short. Smile while you still have teeth :)";

		const string _passwordIdPrefix = "_p_";

		public void AddOrUpdatePassword(string id, SecretLifeTime lifeTime, IPassword passwd)
		{
			if (id == null)
				return;

			EncryptedPassword epasswd = null;
			try
			{
				epasswd = _serializer.Deserialize<EncryptedPassword>(_GetSecret(_passwordIdPrefix + id));
			}
			catch (KeyNotFoundException)
			{
				epasswd = new EncryptedPassword()
				{
					iv = new SecureBuffer(_cryptographer.GenerateIV(), true).ToHexString(),
				};
			}

			IByteBuffer ckey = _cryptographer.GenerateKey(passwd, new ByteBuffer(_salt));
			epasswd.data = _cryptographer.Encrypt(new ByteBuffer(_phrase, Encoding.UTF8), ckey, SecureBuffer.NewFromHexString(epasswd.iv)).ToHexString();
			ckey.Dispose();

			_AddOrUpdateSecret(_passwordIdPrefix + id, lifeTime, _serializer.Serialize<EncryptedPassword>(epasswd));
		}

		public bool PasswordExists(string id) => _SecretExists(_passwordIdPrefix + id);

		public void DeletePassword(string id) => _DeleteSecret(_passwordIdPrefix + id);

		public bool ComparePassword(string id, IPassword passwd)
		{
			EncryptedPassword epasswd = null;
			try
			{
				epasswd = _serializer.Deserialize<EncryptedPassword>(_GetSecret(_passwordIdPrefix + id));
			}
			catch (KeyNotFoundException) { }
			if (epasswd == null)
				return false;

			IByteBuffer ckey = _cryptographer.GenerateKey(passwd, new ByteBuffer(_salt));
			ByteBuffer ddata = _cryptographer.Decrypt(ByteBuffer.NewFromHexString(epasswd.data), ckey, SecureBuffer.NewFromHexString(epasswd.iv));
			ckey.Dispose();

			return ddata.Equals(new ByteBuffer(_phrase, Encoding.UTF8));
		}

		// Cryptographic keys, encryption and decryption

		public void CreateCKey(string id, SecretLifeTime lifeTime, IPassword passwd)
		{
			if (id == null)
				return;

			IByteBuffer ckeyb = new SecureBuffer(_cryptographer.GenerateKey(passwd, new ByteBuffer(_salt)), true);
			string ckey = ckeyb.ToHexString();
			ckeyb.Dispose();

			_AddOrUpdateSecret(id, lifeTime, ckey);
		}

		public bool CKeyExists(string id) => _SecretExists(id);

		public void DeleteCKey(string id) => _DeleteSecret(id);

		IByteBuffer GetCKey(string id) => SecureBuffer.NewFromHexString(_GetSecret(id));

		public IByteBuffer GenerateIV() => _cryptographer.GenerateIV();

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

		// Common secrets

		const string _secretIdPrefix = "_s_";

		public void AddOrUpdateSecret<T>(string id, SecretLifeTime lifeTime, T data)
		{
			if (id == null)
				return;

			string serializedData = _serializer.Serialize<T>(data);

			_AddOrUpdateSecret(_secretIdPrefix + id, lifeTime, serializedData);
		}

		public bool SecretExists(string id) => _SecretExists(_secretIdPrefix + id);

		public T GetSecret<T>(string id) => _serializer.Deserialize<T>(_GetSecret(_secretIdPrefix + id));

		public void DeleteSecret(string id) => _DeleteSecret(_secretIdPrefix + id);

		//

		public void Dispose()
		{
			_cryptographer = null;
			_serializer = null;
			_storage = null;

			_secretsDataCKey?.Dispose();
			_secretsDataIV.Dispose();

			_temporarySecrets.Clear();
			_temporarySecrets = null;
			_secrets.Clear();
			_secrets = null;
		}
	}
}
