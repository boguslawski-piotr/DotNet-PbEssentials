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

		ISerializer _serializer { get; }

		IStorage<string> _storage { get; }

		public SecretsManager(string id, ICryptographer cryptographer, ISerializer serializer, IStorage<string> storage = null)
		{
			if (id == null || cryptographer == null || serializer == null)
				throw new ArgumentException($"{nameof(id)}, {nameof(cryptographer)} and {nameof(serializer)} must be valid objects.");

			Id = id;
			_cryptographer = cryptographer;
			_serializer = new OptimizedForStringSerializer(serializer);
			_storage = storage;
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

		// Common code for passwords, ckeys and other secrets

		[Serializable]
		class TemporarySecret
		{
			public SecretLifeTime lifeTime;
			public string secret;
		}

		IDictionary<string, string> _secrets = new Dictionary<string, string>();

		IDictionary<string, TemporarySecret> _temporarySecrets = new Dictionary<string, TemporarySecret>();

		const string _secretsDataId = ".d46ee950276f4665aefa06cb2ee6b35e";

		void _LoadSecrets()
		{
			if (_secrets.Count > 0 || _storage == null)
				return;

			// TODO: async as sync -> sprawdzic wyjatki i czy na pewno jest to ok rozwiazanie
			string d = Task.Run(async () => await _storage.GetACopyAsync(_secretsDataId)).GetAwaiter().GetResult();
			//string d = await _storage.GetACopyAsync(_ckeysDataId);

			if (!string.IsNullOrEmpty(d))
			{
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

			// TODO: dodac szyfrowanie; haslem powinno byc cos co mozna pobrac z systemu, jest niezmienne i nie da sie wyczytac z kodu programu bez doglebnego debugowania

			_storage.StoreAsync(_secretsDataId, d, DateTime.UtcNow);
		}

		void _AddOrUpdateSecret(string id, SecretLifeTime lifeTime, string s)
		{
			if (lifeTime == SecretLifeTime.Infinite)
			{
				if (_storage == null)
					throw new ArgumentException("Attempt to add a secret with life time set to 'Infinite' without passing any data storage while constructing the object.");

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
			if (_temporarySecrets.TryGetValue(id, out TemporarySecret s))
				return s.secret;

			_LoadSecrets();

			s = new TemporarySecret();
			if (_secrets.TryGetValue(id, out s.secret))
				return s.secret;

			throw new KeyNotFoundException();
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
		readonly byte[] _salt = { 0x43, 0x87, 0x23, 0x72, 0x45, 0x56, 0x68, 0x14, 0x62, 0x84 };

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
	}
}
