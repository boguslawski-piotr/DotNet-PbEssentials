using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using pbXNet;

namespace pbXSecurity
{
    public partial class SecretsManager : ISecretsManager
    {
        protected readonly string _id;
        public string Id
        {
            get {
                return _id;
            }
        }

        protected ICryptographer _cryptographer;
        public ICryptographer Cryptographer
        {
            get {
                return _cryptographer;
            }

        }

		protected IStorage<string> _storage;
		public IStorage<string> Storage
		{
			get {
				return _storage;
			}
		}

        public SecretsManager(string id, ICryptographer cryptographer = null, IStorage<string> storage = null)
        {
            _id = id;
            _cryptographer = cryptographer ?? new AesCryptographer();
            _storage = storage;
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

        protected IDictionary<string, Password> _passwords = new Dictionary<string, Password>();

        // TODO: Exceptions/Errors handling

        const string _PasswordsDataId = "12B77038-229D-4F8F-941B-8465CB132C03";

        protected virtual async Task LoadPasswordsAsync()
        {
            if (_passwords.Count > 0 || _storage == null)
                return;

            if (await _storage.ExistsAsync(_PasswordsDataId))
            {
                string d = await _storage.GetACopyAsync(_PasswordsDataId);
                if (!string.IsNullOrEmpty(d))
                {
                    d = Tools.DeObfuscate(d);
                    using (MemoryStream s = new MemoryStream(ConvertEx.FromHexString(d)))
                    {
                        _passwords = (Dictionary<string, Password>)new BinaryFormatter().Deserialize(s);
                    }
                }
            }
        }

        protected virtual async Task SavePasswordsAsync()
        {
			if (_storage == null)
				return;
            
			using (MemoryStream s = new MemoryStream(1024))
            {
                new BinaryFormatter().Serialize(s, _passwords);

                string d = ConvertEx.ToHexString(s.ToArray());
                d = Tools.Obfuscate(d);

                await _storage.StoreAsync(_PasswordsDataId, d);
            }
        }

        const string _phrase = "Life is short. Smile while you still have teeth :)";
		readonly byte[] _salt = new byte[] { 0x43, 0x87, 0x23, 0x72, 0x45, 0x56, 0x68, 0x14, 0x62, 0x84 };

        public async Task<bool> PasswordExistsAsync(string id)
        {
            await LoadPasswordsAsync();
            return _passwords.ContainsKey(id);
        }

        public async Task AddOrUpdatePasswordAsync(string id, string passwd)
        {
            await LoadPasswordsAsync();

            Password _password;
            if (!_passwords.TryGetValue(id, out _password))
            {
                _password = new Password()
                {
                    iv = _cryptographer.GenerateIV()
                };
            }

			byte[] key = _cryptographer.GenerateKey(Encoding.UTF8.GetBytes(passwd), _salt);
            _password.data = _cryptographer.Encrypt(Encoding.UTF8.GetBytes(_phrase), key, _password.iv);

            _passwords[id] = _password;

            await SavePasswordsAsync();
        }

        public async Task DeletePasswordAsync(string id)
        {
            if (await PasswordExistsAsync(id))
            {
                _passwords.Remove(id);
                await SavePasswordsAsync();
            }
		}

		public async Task<bool> ComparePasswordAsync(string id, string passwd)
		{
            await LoadPasswordsAsync();

            Password _password;
            if (!_passwords.TryGetValue(id, out _password))
                return false;

			byte[] key = _cryptographer.GenerateKey(Encoding.UTF8.GetBytes(passwd), _salt);
            byte[] ddata = _cryptographer.Decrypt(_password.data, key, _password.iv);

            return ddata.SequenceEqual(Encoding.UTF8.GetBytes(_phrase));
        }

		//

		public async Task AddOrUpdatePasswordAsync(string id, SecureString passwd)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ComparePasswordAsync(string id, SecureString passwd)
        {
            throw new NotImplementedException();
        }


    }
}
