using System;
using System.Collections.Generic;
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

		[Serializable]
		class Password
		{
			public byte[] iv;
			public byte[] data;
		};

		IDictionary<string, Password> _passwords = new Dictionary<string, Password>();

		// TODO: Exceptions/Errors handling

		const string _passwordsDataId = ".12b77038229d4f8f941b8465cb132c03";

		async Task LoadPasswordsAsync()
		{
			if (_passwords.Count > 0 || _storage == null || _serializer == null)
				return;

			string d = await _storage.GetACopyAsync(_passwordsDataId);
			if (!string.IsNullOrEmpty(d))
			{
				d = Obfuscator.DeObfuscate(d);
				_passwords = _serializer.FromString<Dictionary<string, Password>>(d);
			}
		}

		async Task SavePasswordsAsync()
		{
			if (_storage == null || _serializer == null)
				return;

			string d = _serializer.ToString(_passwords);
			d = Obfuscator.Obfuscate(d);

			await _storage.StoreAsync(_passwordsDataId, d, DateTime.UtcNow);
		}

		const string _phrase = "Life is short. Smile while you still have teeth :)";
		readonly byte[] _salt = { 0x43, 0x87, 0x23, 0x72, 0x45, 0x56, 0x68, 0x14, 0x62, 0x84 };

		public async Task<bool> PasswordExistsAsync(string id)
		{
			await LoadPasswordsAsync();
			return _passwords.ContainsKey(id);
		}

		public async Task AddOrUpdatePasswordAsync(string id, char[] passwd)
		{
			byte[] bpasswd = Encoding.UTF8.GetBytes(passwd);
			await AddOrUpdatePasswordAsync(id, bpasswd);
			bpasswd.FillWithDefault();
		}

		public async Task AddOrUpdatePasswordAsync(string id, string passwd)
		{
			byte[] bpasswd = Encoding.UTF8.GetBytes(passwd);
			await AddOrUpdatePasswordAsync(id, bpasswd);
			bpasswd.FillWithDefault();
		}

		public async Task AddOrUpdatePasswordAsync(string id, byte[] passwd)
		{
			if (id == null)
				return;

			await LoadPasswordsAsync();

			Password _password;
			if (!_passwords.TryGetValue(id, out _password))
			{
				_password = new Password()
				{
					iv = _cryptographer.GenerateIV()
				};
			}

			byte[] ckey = _cryptographer.GenerateKey(passwd, _salt);
			_password.data = _cryptographer.Encrypt(Encoding.UTF8.GetBytes(_phrase), ckey, _password.iv);

			_passwords[id] = _password;

			SavePasswordsAsync();
		}

		public async Task DeletePasswordAsync(string id)
		{
			if (await PasswordExistsAsync(id))
			{
				_passwords.Remove(id);
				SavePasswordsAsync();
			}
		}

		public async Task<bool> ComparePasswordAsync(string id, char[] passwd)
		{
			byte[] bpasswd = Encoding.UTF8.GetBytes(passwd);
			bool b = await ComparePasswordAsync(id, bpasswd);
			bpasswd.FillWithDefault();
			return b;
		}

		public async Task<bool> ComparePasswordAsync(string id, string passwd)
		{
			byte[] bpasswd = Encoding.UTF8.GetBytes(passwd);
			bool b = await ComparePasswordAsync(id, bpasswd);
			bpasswd.FillWithDefault();
			return b;
		}

		public async Task<bool> ComparePasswordAsync(string id, byte[] passwd)
		{
			await LoadPasswordsAsync();

			Password _password;
			if (!_passwords.TryGetValue(id, out _password))
				return false;

			byte[] ckey = _cryptographer.GenerateKey(passwd, _salt);
			byte[] ddata = _cryptographer.Decrypt(_password.data, ckey, _password.iv);

			return ddata.SequenceEqual(Encoding.UTF8.GetBytes(_phrase));
		}


		// Cryptographic keys, encryption and decryption

		class TemporaryCKey
		{
			public CKeyLifeTime lifeTime;
			public byte[] ckey;
		}

		IDictionary<string, byte[]> _ckeys = new Dictionary<string, byte[]>();
		IDictionary<string, TemporaryCKey> _temporaryCKeys = new Dictionary<string, TemporaryCKey>();

		const string _ckeysDataId = ".d46ee950276f4665aefa06cb2ee6b35e";

		async Task LoadCKeysAsync()
		{
			if (_ckeys.Count > 0 || _storage == null || _serializer == null)
				return;

			string d = await _storage.GetACopyAsync(_ckeysDataId);
			if (!string.IsNullOrEmpty(d))
			{
				d = Obfuscator.DeObfuscate(d);
				_ckeys = _serializer.FromString<IDictionary<string, byte[]>>(d);
			}
		}

		async Task SaveCKeysAsync()
		{
			if (_storage == null || _serializer == null)
				return;

			string d = _serializer.ToString(_ckeys);
			d = Obfuscator.Obfuscate(d);

			// TODO: dodac szyfrowanie; haslem powinno byc cos co mozna pobrac z systemu, jest niezmienne i nie da sie wyczytac z kodu programu bez doglebnego debugowania

			await _storage.StoreAsync(_ckeysDataId, d, DateTime.UtcNow);
		}

		public async Task<byte[]> CreateCKeyAsync(string id, CKeyLifeTime lifeTime, string passwd)
		{
			byte[] bpasswd = Encoding.UTF8.GetBytes(passwd);
			byte[] ckey = await CreateCKeyAsync(id, lifeTime, bpasswd);
			bpasswd.FillWithDefault();
			return ckey;
		}

		public async Task<byte[]> CreateCKeyAsync(string id, CKeyLifeTime lifeTime, char[] passwd)
		{
			byte[] bpasswd = Encoding.UTF8.GetBytes(passwd);
			byte[] ckey = await CreateCKeyAsync(id, lifeTime, bpasswd);
			bpasswd.FillWithDefault();
			return ckey;
		}

		public async Task<byte[]> CreateCKeyAsync(string id, CKeyLifeTime lifeTime, byte[] passwd)
		{
			if (id == null)
				return null;

			byte[] ckey = _cryptographer.GenerateKey(passwd, _salt);

			if (lifeTime == CKeyLifeTime.Infinite)
			{
				await LoadCKeysAsync();

				_ckeys[id] = ckey;

				await SaveCKeysAsync();
			}
			else
			{
				if (lifeTime != CKeyLifeTime.OneTime)
					_temporaryCKeys[id] = new TemporaryCKey() { lifeTime = lifeTime, ckey = ckey };
			}

			return ckey;
		}

		public async Task<byte[]> GetCKeyAsync(string id)
		{
			if (_temporaryCKeys.TryGetValue(id, out TemporaryCKey ckey))
				return ckey.ckey;

			await LoadCKeysAsync();

			ckey = new TemporaryCKey();
			if (_ckeys.TryGetValue(id, out ckey.ckey))
				return ckey.ckey;

			return null;
		}

		public async Task DeleteCKeyAsync(string id)
		{
			if (_temporaryCKeys.ContainsKey(id))
				_temporaryCKeys.Remove(id);

			await LoadCKeysAsync();

			if (_ckeys.ContainsKey(id))
			{
				_ckeys.Remove(id);
				await SaveCKeysAsync();
			}
		}

		public async Task<string> EncryptAsync(string data, byte[] ckey, byte[] iv)
		{
			byte[] edata = _cryptographer.Encrypt(Encoding.UTF8.GetBytes(data), ckey, iv);
			return ConvertEx.ToHexString(edata);
		}

		public async Task<string> DecryptAsync(string data, byte[] ckey, byte[] iv)
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
