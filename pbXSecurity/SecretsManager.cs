
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
    public interface ISecretsManager
    {
        // Basic device owner authentication (pin, passkey, biometrics, etc.)

        bool DeviceOwnerAuthenticationAvailable { get; }
        bool DeviceOwnerAuthenticationWithBiometricsAvailable { get; }
        bool AuthenticateDeviceOwner(string Msg, Action Succes, Action<string> Error);

        // Basic authentication based on passwords
        // Implementation should never store any password anywhere in any form

        Task<bool> PasswordExistsAsync(string id);
        Task AddOrUpdatePasswordAsync(string id, string passwd);
        Task<bool> ComparePasswordAsync(string id, string passwd);
    }

    public partial class SecretsManager : ISecretsManager
    {
        protected readonly string _id;

        protected ICryptographer _cryptographer;

        public SecretsManager(string id, ICryptographer cryptographer)
        {
            _id = id;
            _cryptographer = cryptographer;
        }


        // Basic device owner authentication (pin, passkey, biometrics, etc.)
        // You will find the implementation in the platform directories...


        // Basic authentication based on passwords
        // No password is ever written anywhere

        [Serializable]
        class Password
        {
            public byte[] iv;
            public string data;
        };

        IDictionary<string, Password> _passwords = new Dictionary<string, Password>();

        const string _PasswordsFileName = "p";

        // TODO: Exceptions/Erros handling

        async Task<IFileSystem> PasswordsFs()
        {
            IFileSystem fs = new DeviceFileSystem(DeviceFileSystemRoot.Config);
            await fs.CreateDirectoryAsync(_id);
            return fs;
        }

        async Task LoadPasswordsAsync()
        {
            if (_passwords.Count > 0)
                return;

            using (IFileSystem fs = await PasswordsFs())
            {
                if (await fs.FileExistsAsync(_PasswordsFileName))
                {
                    string d = await fs.ReadTextAsync(_PasswordsFileName);
                    if (!string.IsNullOrEmpty(d))
                    {
                        d = await _cryptographer.DeObfuscateAsync(d);
                        using (MemoryStream s = new MemoryStream(ConvertEx.FromHexString(d)))
                        {
                            _passwords = (Dictionary<string, Password>)new BinaryFormatter().Deserialize(s);
                        }
                    }
                }
            }
        }

        async Task SavePasswordsAsync()
        {
            if (_passwords.Count <= 0)
                return;

            using (MemoryStream s = new MemoryStream(128))
            {
                new BinaryFormatter().Serialize(s, _passwords);

                string d = ConvertEx.ToHexString(s.ToArray());
                d = await _cryptographer.ObfuscateAsync(d);

                using (IFileSystem fs = await PasswordsFs())
                {
                    await fs.WriteTextAsync(_PasswordsFileName, d);
                }
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

        async public Task<bool> PasswordExistsAsync(string id)
        {
            await LoadPasswordsAsync();
            return _passwords.ContainsKey(id);
        }

        async public Task AddOrUpdatePasswordAsync(string id, string passwd)
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

        async public Task<bool> ComparePasswordAsync(string id, string passwd)
        {
            await LoadPasswordsAsync();

            Password _password;
            if (!_passwords.TryGetValue(id, out _password))
                return false;

            string data = await EncryptPhraseAsync(passwd, _password.iv);
            return data == _password.data;
        }


        //

        public Task AddOrUpdatePasswordAsync(string id, SecureString passwd)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ComparePasswordAsync(string id, SecureString passwd)
        {
            throw new NotImplementedException();
        }


    }
}
