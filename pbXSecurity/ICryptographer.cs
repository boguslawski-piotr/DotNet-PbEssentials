using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pbXSecurity
{
    public interface ICryptographer
    {
        //

        string Obfuscate(string d);
        Task<string> ObfuscateAsync(string d);

        string DeObfuscate(string d);
        Task<string> DeObfuscateAsync(string d);

        //

        byte[] GenerateKey(byte[] pwd, byte[] salt, int length = 32);

        byte[] GenerateIV(int length = 16);

        byte[] Encrypt(byte[] msg, byte[] key, byte[] iv);
        Task<byte[]> EncryptAsync(byte[] msg, byte[] key, byte[] iv);

        byte[] Decrypt(byte[] msg, byte[] key, byte[] iv);
        Task<byte[]> DecryptAsync(byte[] msg, byte[] key, byte[] iv);
    }
}
