using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.Text;

namespace send.helpers
{
    class Encryption
    {
        public const string AES_ALGORITHM = "AES/CBC/PKCS7";
        public const string CRYPTO_ALGORITHM = "AES";
        public byte[] encryptionKey;
        public byte[] iv;
        public Encryption()
        {
            encryptionKey = Convert.FromBase64String("OXzN4fxHHgcKtAt/SJ4UWtNiQlzno7II1gIBs24CWpY=");
            iv = Convert.FromBase64String("fdL8sKmhC8YIrYHMzoJJvQ==");
        }
        public string Encrypt(string plainText)
        {
            //setting up AES Key
            KeyParameter aesKeyParam = ParameterUtilities.CreateKeyParameter(CRYPTO_ALGORITHM, encryptionKey);
            // Setting up the Initialization Vector. IV is used for encrypting the first block of input message
            ParametersWithIV aesIVKeyParam = new ParametersWithIV(aesKeyParam, iv);
            // Create the cipher object for AES algorithm using GCM mode and NO padding
            IBufferedCipher cipher = CipherUtilities.GetCipher(AES_ALGORITHM);
            cipher.Init(true, aesIVKeyParam);
            byte[] output = cipher.DoFinal(Encoding.UTF8.GetBytes(plainText));
            string cipherText = BitConverter.ToString(output).Replace("-", string.Empty).ToLower();
            if(cipherText.Length > 255)
            {
                cipherText = cipherText.Insert(250, "/");
            }

            return cipherText;
        }

        public string Decrypt(string cipherText)
        {
            //setting up AES Key
            KeyParameter aesKeyParam = ParameterUtilities.CreateKeyParameter(CRYPTO_ALGORITHM, encryptionKey);
            // Setting up the Initialization Vector. IV is used for encrypting the first block of input message
            ParametersWithIV aesIVKeyParam = new ParametersWithIV(aesKeyParam, iv);
            // Create the cipher object for AES algorithm using GCM mode and NO padding
            IBufferedCipher cipher = CipherUtilities.GetCipher(AES_ALGORITHM);
            cipher.Init(false, aesIVKeyParam);
            byte[] output = cipher.DoFinal(StringToByteArray(cipherText.Trim()));
            return Encoding.UTF8.GetString(output);
        }

        private static byte[] StringToByteArray(string hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}
