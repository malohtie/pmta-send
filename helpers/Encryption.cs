using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Send.helpers
{
    class Encryption
    {
        private const string Key = "qAnq8gLoTgnXI6LgL8I8";
        private const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz234567";
        public static string Encrypt(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return string.Empty;
            }
            using (Aes aes = Aes.Create())
            {
                aes.Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(Key));
                aes.Mode = CipherMode.CBC;

                aes.GenerateIV();
                byte[] iv = aes.IV;
                byte[] encrypted;

                using (ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, iv))
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ms.Write(iv, 0, iv.Length);
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter sw = new StreamWriter(cs))
                            {
                                sw.Write(data);
                            }
                        }
                        encrypted = ms.ToArray();
                    }
                }

                return ToBase32String(encrypted);
            }
        }

        public static string Decrypt(string data)
        {
            byte[] cipherText = FromBase32String(data);

            using (Aes aes = Aes.Create())
            {
                aes.Key = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(Key));
                aes.Mode = CipherMode.CBC;

                byte[] iv = new byte[aes.BlockSize / 8];
                byte[] encryptedData = new byte[cipherText.Length - iv.Length];

                Array.Copy(cipherText, 0, iv, 0, iv.Length);
                Array.Copy(cipherText, iv.Length, encryptedData, 0, encryptedData.Length);

                using (ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, iv))
                {
                    using (MemoryStream ms = new MemoryStream(encryptedData))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }

        private static string ToBase32String(byte[] bytes)
        {

            StringBuilder result = new StringBuilder((bytes.Length + 7) * 8 / 5);
            byte index;
            int bits = 0;
            int buffer = 0;

            foreach (byte b in bytes)
            {
                buffer = (buffer << 8) | b;
                bits += 8;

                while (bits >= 5)
                {
                    index = (byte)((buffer >> (bits - 5)) & 31);
                    result.Append(alphabet[index]);
                    bits -= 5;
                }
            }

            if (bits > 0)
            {
                index = (byte)((buffer << (5 - bits)) & 31);
                result.Append(alphabet[index]);
            }

            return result.ToString();
        }

        private static byte[] FromBase32String(string base32)
        {

            int bits = 0;
            int buffer = 0;
            int outputIndex = 0;
            byte[] output = new byte[base32.Length * 5 / 8];

            foreach (char c in base32)
            {
                int index = alphabet.IndexOf(c);

                if (index < 0)
                {
                    throw new FormatException("Invalid Base32 character.");
                }

                buffer = (buffer << 5) | index;
                bits += 5;

                if (bits >= 8)
                {
                    output[outputIndex++] = (byte)((buffer >> (bits - 8)) & 255);
                    bits -= 8;
                }
            }

            return output;
        }
    }
}
