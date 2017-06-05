using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace pbXNet
{
	public partial class SecretsManager : ISecretsManager
	{
		public string Id { get; }

		protected ICryptographer Cryptographer { get; }

		protected IStorage<string> Storage { get; }

		public SecretsManager(string id, ICryptographer cryptographer, IStorage<string> storage = null)
		{
			Id = id;
			Cryptographer = cryptographer;
			Storage = storage;
		}


		// Basic device owner authentication (pin, passkey, biometrics, etc.)
		// You will find the implementation in the platform directories...


		// Basic authentication based on passwords
		// No password is ever written anywhere

		[Serializable]
		protected class Password
		{
			public byte[] iv;
			public byte[] data;
		};

		protected IDictionary<string, Password> Passwords = new Dictionary<string, Password>();

		// TODO: Exceptions/Errors handling

		const string _PasswordsDataId = "12b77038229d4f8f941b8465cb132c03";

		protected virtual async Task LoadPasswordsAsync()
		{
			if (Passwords.Count > 0 || Storage == null)
				return;

			string d = await Storage.GetACopyAsync(_PasswordsDataId);
			if (!string.IsNullOrEmpty(d))
			{
				d = Obfuscator.DeObfuscate(d);
				Passwords = JsonConvert.DeserializeObject<Dictionary<string, Password>>(d);
			}
		}

		protected virtual async Task SavePasswordsAsync()
		{
			if (Storage == null)
				return;

			string d = JsonConvert.SerializeObject(Passwords);
			d = Obfuscator.Obfuscate(d);

			await Storage.StoreAsync(_PasswordsDataId, d, DateTime.UtcNow);
		}

		const string _phrase = "Life is short. Smile while you still have teeth :)";
		readonly byte[] _salt = { 0x43, 0x87, 0x23, 0x72, 0x45, 0x56, 0x68, 0x14, 0x62, 0x84 };

		public async Task<bool> PasswordExistsAsync(string id)
		{
			await LoadPasswordsAsync();
			return Passwords.ContainsKey(id);
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
			if (!Passwords.TryGetValue(id, out _password))
			{
				_password = new Password()
				{
					iv = Cryptographer.GenerateIV()
				};
			}

			byte[] ckey = Cryptographer.GenerateKey(passwd, _salt);
			_password.data = Cryptographer.Encrypt(Encoding.UTF8.GetBytes(_phrase), ckey, _password.iv);

			Passwords[id] = _password;

			SavePasswordsAsync();
		}

		public async Task DeletePasswordAsync(string id)
		{
			if (await PasswordExistsAsync(id))
			{
				Passwords.Remove(id);
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
			if (!Passwords.TryGetValue(id, out _password))
				return false;

			byte[] ckey = Cryptographer.GenerateKey(passwd, _salt);
			byte[] ddata = Cryptographer.Decrypt(_password.data, ckey, _password.iv);

			return ddata.SequenceEqual(Encoding.UTF8.GetBytes(_phrase));
		}


		// Cryptographic keys, encryption and decryption

		protected class TemporaryCKey
		{
			public CKeyLifeTime lifeTime;
			public byte[] ckey;
		}

		protected IDictionary<string, byte[]> CKeys = new Dictionary<string, byte[]>();
		protected IDictionary<string, TemporaryCKey> TemporaryCKeys = new Dictionary<string, TemporaryCKey>();

		const string _CKeysDataId = "d46ee950276f4665aefa06cb2ee6b35e";

		protected virtual async Task LoadCKeysAsync()
		{
			if (CKeys.Count > 0 || Storage == null)
				return;

			string d = await Storage.GetACopyAsync(_CKeysDataId);
			if (!string.IsNullOrEmpty(d))
			{
				d = Obfuscator.DeObfuscate(d);
				CKeys = JsonConvert.DeserializeObject<IDictionary<string, byte[]>>(d);
			}
		}

		protected virtual async Task SaveCKeysAsync()
		{
			if (Storage == null)
				return;

			string d = JsonConvert.SerializeObject(CKeys);
			d = Obfuscator.Obfuscate(d);
			// TODO: dodac szyfrowanie; haslem powinno byc cos co mozna pobrac z systemu, jest niezmienne i nie da sie wyczytac z kodu programu bez doglebnego debugowania

			await Storage.StoreAsync(_CKeysDataId, d, DateTime.UtcNow);
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

			byte[] ckey = Cryptographer.GenerateKey(passwd, _salt);

			if (lifeTime == CKeyLifeTime.Infinite)
			{
				await LoadCKeysAsync();

				CKeys[id] = ckey;

				await SaveCKeysAsync();
			}
			else
			{
				if (lifeTime != CKeyLifeTime.OneTime)
					TemporaryCKeys[id] = new TemporaryCKey() { lifeTime = lifeTime, ckey = ckey };
			}

			return ckey;
		}

		public async Task<byte[]> GetCKeyAsync(string id)
		{
			if (TemporaryCKeys.TryGetValue(id, out TemporaryCKey ckey))
				return ckey.ckey;

			await LoadCKeysAsync();

			ckey = new TemporaryCKey();
			if (CKeys.TryGetValue(id, out ckey.ckey))
				return ckey.ckey;

			return null;
		}

		public async Task DeleteCKeyAsync(string id)
		{
			if (TemporaryCKeys.ContainsKey(id))
				TemporaryCKeys.Remove(id);

			await LoadCKeysAsync();

			if (CKeys.ContainsKey(id))
			{
				CKeys.Remove(id);
				await SaveCKeysAsync();
			}
		}

		public async Task<string> EncryptAsync(string data, byte[] ckey, byte[] iv)
		{
			byte[] edata = Cryptographer.Encrypt(Encoding.UTF8.GetBytes(data), ckey, iv);
			return ConvertEx.ToHexString(edata);
		}

		public async Task<string> DecryptAsync(string data, byte[] ckey, byte[] iv)
		{
			byte[] ddata = Cryptographer.Decrypt(data.FromHexString(), ckey, iv);
			return Encoding.UTF8.GetString(ddata, 0, ddata.Length);
		}

		public byte[] GenerateIV()
		{
			return Cryptographer.GenerateIV();
		}
	}
}
