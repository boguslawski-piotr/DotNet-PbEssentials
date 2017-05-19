using System;
using System.Collections.Generic;
using System.IO;
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

        protected IFileSystem _fs;
        public IFileSystem Fs
        {
            get {
                return _fs;
            }
        }

        public SecretsManager(string id, ICryptographer cryptographer = null, IFileSystem fs = null)
        {
            _id = id;
            _cryptographer = cryptographer ?? new AesCryptographer();
            _fs = fs ?? new DeviceFileSystem(DeviceFileSystemRoot.Config);
        }


        // Basic device owner authentication (pin, passkey, biometrics, etc.)
        // You will find the implementation in the platform directories...


        // Basic authentication based on passwords
        // No password is ever written anywhere

        [Serializable]
        protected class Password
        {
            public byte[] iv;
            public string data;
        };

        protected IDictionary<string, Password> _passwords = new Dictionary<string, Password>();

        // TODO: Exceptions/Erros handling
        // TODO: save data to Plugin.Settings instead of DFS

        const string _PasswordsFileName = "p";

        protected virtual async Task<IFileSystem> PasswordsFs()
        {
            await _fs.SetCurrentDirectoryAsync(null);
            await _fs.CreateDirectoryAsync(_id);
            return _fs;
        }

        protected virtual async Task LoadPasswordsAsync()
        {
            if (_passwords.Count > 0)
                return;

            IFileSystem fs = await PasswordsFs();

            if (await fs.FileExistsAsync(_PasswordsFileName))
            {
                string d = await fs.ReadTextAsync(_PasswordsFileName);
                if (!string.IsNullOrEmpty(d))
                {
                    d = await Tools.DeObfuscateAsync(d);
                    using (MemoryStream s = new MemoryStream(ConvertEx.FromHexString(d)))
                    {
                        _passwords = (Dictionary<string, Password>)new BinaryFormatter().Deserialize(s);
                    }
                }
            }
        }

        protected virtual async Task SavePasswordsAsync()
        {
            if (_passwords.Count <= 0)
                return;

            using (MemoryStream s = new MemoryStream(128))
            {
                new BinaryFormatter().Serialize(s, _passwords);

                string d = ConvertEx.ToHexString(s.ToArray());
                d = await Tools.ObfuscateAsync(d);

                IFileSystem fs = await PasswordsFs();
                await fs.WriteTextAsync(_PasswordsFileName, d);
            }
        }

        const string _phrase = "Life is short. Smile while you still have teeth :)";

        async Task<string> EncryptPhraseAsync(string passwd, byte[] aiv)
        {
            byte[] apasswd = Encoding.UTF8.GetBytes(passwd);
            byte[] asalt = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            byte[] bkey = _cryptographer.GenerateKey(apasswd, asalt);
            byte[] bphrase = Encoding.UTF8.GetBytes(_phrase);

            byte[] e = await _cryptographer.EncryptAsync(bphrase, bkey, aiv);
            return Convert.ToBase64String(e);
        }

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

            _password.data = await EncryptPhraseAsync(passwd, _password.iv);

            _passwords[id] = _password;

            await SavePasswordsAsync();
        }

        public async Task<bool> ComparePasswordAsync(string id, string passwd)
        {
            await LoadPasswordsAsync();

            Password _password;
            if (!_passwords.TryGetValue(id, out _password))
                return false;

            string data = await EncryptPhraseAsync(passwd, _password.iv);
            return data == _password.data;
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
