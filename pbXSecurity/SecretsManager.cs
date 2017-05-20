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
        public ISecretsManagerUI UI { get; set; }

        public string Id { get; }

        protected ICryptographer Cryptographer { get; }

        protected IStorage<string> Storage { get; }

        public SecretsManager(string id = null, ICryptographer cryptographer = null, IStorage<string> storage = null, ISecretsManagerUI ui = null)
        {
            Id = id ?? pbXNet.Tools.CreateGuid();
            Cryptographer = cryptographer ?? new AesCryptographer();
            Storage = storage;
            UI = ui;
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
            //[NonSerialized] private bool flag;
        };

        protected IDictionary<string, Password> Passwords = new Dictionary<string, Password>();

        // TODO: Exceptions/Errors handling

        const string _PasswordsDataId = "12b77038229d4f8f941b8465cb132c03";

        protected virtual async Task LoadPasswordsAsync()
        {
            if (Passwords.Count > 0 || Storage == null)
                return;

            if (await Storage.ExistsAsync(_PasswordsDataId))
            {
                string d = await Storage.GetACopyAsync(_PasswordsDataId);
                if (!string.IsNullOrEmpty(d))
                {
                    d = Tools.DeObfuscate(d);
                    using (MemoryStream s = new MemoryStream(ConvertEx.FromHexString(d)))
                    {
                        Passwords = (Dictionary<string, Password>)new BinaryFormatter().Deserialize(s);
                    }
                }
            }
        }

        protected virtual async Task SavePasswordsAsync()
        {
            if (Storage == null)
                return;

            using (MemoryStream s = new MemoryStream(1024))
            {
                new BinaryFormatter().Serialize(s, Passwords);

                string d = ConvertEx.ToHexString(s.ToArray());
                d = Tools.Obfuscate(d);

                await Storage.StoreAsync(_PasswordsDataId, d);
            }
        }

        const string _phrase = "Life is short. Smile while you still have teeth :)";
        readonly byte[] _salt = { 0x43, 0x87, 0x23, 0x72, 0x45, 0x56, 0x68, 0x14, 0x62, 0x84 };

        public async Task<bool> PasswordExistsAsync(string id)
        {
            await LoadPasswordsAsync();
            return Passwords.ContainsKey(id);
        }

        public async Task AddOrUpdatePasswordAsync(string id, string passwd)
        {
            await LoadPasswordsAsync();

            Password _password;
            if (!Passwords.TryGetValue(id, out _password))
            {
                _password = new Password()
                {
                    iv = Cryptographer.GenerateIV()
                };
            }

            byte[] key = Cryptographer.GenerateKey(Encoding.UTF8.GetBytes(passwd), _salt);
            _password.data = Cryptographer.Encrypt(Encoding.UTF8.GetBytes(_phrase), key, _password.iv);

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

        public async Task<bool> ComparePasswordAsync(string id, string passwd)
        {
            await LoadPasswordsAsync();

            Password _password;
            if (!Passwords.TryGetValue(id, out _password))
                return false;

            byte[] key = Cryptographer.GenerateKey(Encoding.UTF8.GetBytes(passwd), _salt);
            byte[] ddata = Cryptographer.Decrypt(_password.data, key, _password.iv);

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
