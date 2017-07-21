using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace pbXNet
{
	public sealed class SecretsManager : ISecretsManager, IDisposable
	{
		public string Id { get; }

		readonly ICryptographer _cryptographer = new AesCryptographer();

		readonly ISerializer _serializer;

		readonly IStorage<string> _storage;

		/// IMPORTANT NOTE: 
		/// Passed passwd is completely cleaned (zeros) as soon as possible.
		/// You should not use this data anymore after it was passed to the EncryptedFileSystem class constructor.
		public SecretsManager(string id, ISerializer serializer = null, IStorage<string> storage = null, IPassword passwd = null)
		{
			Id = id ?? throw new ArgumentNullException(nameof(id));
			_serializer = new StringOptimizedSerializer(serializer ?? new NewtonsoftJsonSerializer());
			_storage = storage;
			_CreateCKey(passwd);
		}

		public void Dispose()
		{
			_secretsDataCKey?.Dispose();
			_secretsDataIV.Dispose();

			_temporarySecrets.Clear();
			_secrets.Clear();
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

		readonly IDictionary<string, TemporarySecret> _temporarySecrets = new Dictionary<string, TemporarySecret>();

		const string _secretsDataId = ".d46ee950276f4665aefa06cb2ee6b35e";

		readonly SecureBuffer _secretsDataIV = SecureBuffer.NewFromHexString("F36A0B7F6D95F16089EBB4A3EC196F0D960300");

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

			string d = null;
			try
			{
				d = Task.Run(async () => await _storage.GetACopyAsync(_secretsDataId)).GetAwaiter().GetResult();
			}
			catch (StorageThingNotFoundException) { }

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
			Check.Empty(id, nameof(id));

			s = Obfuscator.Obfuscate(s);

			if (lifeTime == SecretLifeTime.Infinite)
			{
				if (_storage == null)
					throw new ArgumentException(Localized.T("SM_StorageNotProvided"));

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
			Check.Empty(id, nameof(id));

			if (_temporarySecrets.ContainsKey(id))
				return true;

			_LoadSecrets();
			return _secrets.ContainsKey(id);
		}

		string _GetSecret(string id)
		{
			Check.Empty(id, nameof(id));

			string __GetSecret()
			{
				if (_temporarySecrets.TryGetValue(id, out TemporarySecret s))
					return s.secret;

				_LoadSecrets();

				s = new TemporarySecret();
				if (_secrets.TryGetValue(id, out s.secret))
					return s.secret;

				throw new KeyNotFoundException(Localized.T("SM_KeyNotFound", id));
			}

			return Obfuscator.DeObfuscate(__GetSecret());
		}

		void _DeleteSecret(string id)
		{
			Check.Empty(id, nameof(id));

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
			Check.Empty(id, nameof(id));
			Check.Null(passwd, nameof(passwd));

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

		public bool PasswordExists(string id)
		{
			Check.Empty(id, nameof(id));

			return _SecretExists(_passwordIdPrefix + id);
		}

		public void DeletePassword(string id)
		{
			Check.Empty(id, nameof(id));

			_DeleteSecret(_passwordIdPrefix + id);
		}

		public bool ComparePassword(string id, IPassword passwd)
		{
			Check.Empty(id, nameof(id));
			Check.Null(passwd, nameof(passwd));

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
			Check.Empty(id, nameof(id));
			Check.Null(passwd, nameof(passwd));

			IByteBuffer ckeyb = new SecureBuffer(_cryptographer.GenerateKey(passwd, new ByteBuffer(_salt)), true);
			string ckey = ckeyb.ToHexString();
			ckeyb.Dispose();

			_AddOrUpdateSecret(id, lifeTime, ckey);
		}

		public bool CKeyExists(string id)
		{
			return _SecretExists(id);
		}

		public void DeleteCKey(string id)
		{
			_DeleteSecret(id);
		}

		IByteBuffer GetCKey(string id)
		{
			return SecureBuffer.NewFromHexString(_GetSecret(id));
		}

		public IByteBuffer GenerateIV()
		{
			return _cryptographer.GenerateIV();
		}

		public string Encrypt(string data, string id, IByteBuffer iv)
		{
			Check.Null(data, nameof(data));
			Check.Empty(id, nameof(id));
			Check.Null(iv, nameof(iv));

			IByteBuffer ckey = GetCKey(id);
			ByteBuffer edata = _cryptographer.Encrypt(new ByteBuffer(data, Encoding.UTF8), ckey, iv);
			ckey.Dispose();
			return edata.ToHexString();
		}

		public string Decrypt(string data, string id, IByteBuffer iv)
		{
			Check.Empty(data, nameof(data));
			Check.Empty(id, nameof(id));
			Check.Null(iv, nameof(iv));

			IByteBuffer ckey = GetCKey(id);
			ByteBuffer ddata = _cryptographer.Decrypt(ByteBuffer.NewFromHexString(data), ckey, iv);
			ckey.Dispose();
			return ddata.ToString(Encoding.UTF8);
		}

		// Common secrets

		const string _secretIdPrefix = "_s_";

		public void AddOrUpdateSecret<T>(string id, SecretLifeTime lifeTime, T data)
		{
			Check.Empty(id, nameof(id));

			string serializedData = _serializer.Serialize<T>(data);

			_AddOrUpdateSecret(_secretIdPrefix + id, lifeTime, serializedData);
		}

		public bool SecretExists(string id)
		{
			Check.Empty(id, nameof(id));

			return _SecretExists(_secretIdPrefix + id);
		}

		public T GetSecret<T>(string id)
		{
			Check.Empty(id, nameof(id));

			return _serializer.Deserialize<T>(_GetSecret(_secretIdPrefix + id));
		}

		public void DeleteSecret(string id)
		{
			Check.Empty(id, nameof(id));

			_DeleteSecret(_secretIdPrefix + id);
		}
	}
}
