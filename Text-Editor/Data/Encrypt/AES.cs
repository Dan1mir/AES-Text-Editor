using System.IO;
using System.Security.Cryptography;

namespace Text_Editor.Data.Encrypt
{
    public class AES
    {
        private readonly Aes _aes;
        public AES()
        {
            _aes = Aes.Create();
            _aes.KeySize = 256;
            _aes.BlockSize = 128;
            _aes.Padding = PaddingMode.PKCS7;
        }
        public byte[] Encrypt(string plainText, byte[] key, byte[] iv)
        {
            using (var encryptor = _aes.CreateEncryptor(key, iv))
            {
                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        return ms.ToArray();
                    }
                }
            }
        }
        public string Decrypt(byte[] cipherText, byte[] key, byte[] iv)
        {
            using (var decryptor = _aes.CreateDecryptor(key, iv))
            {
                using (var ms = new MemoryStream(cipherText))
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }
        public (byte[] Key, byte[] IV) GenerateKeyIV()
        {
            _aes.GenerateKey();
            _aes.GenerateIV();
            return (_aes.Key, _aes.IV);
        }
    }
}
