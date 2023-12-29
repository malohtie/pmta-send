using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Send.helpers
{
    internal class Hmac
    {
        private static byte[] hexChars = new byte[16]
        {
            48, 49, 50, 51, 52, 53, 54, 55, 56, 57,
            97, 98, 99, 100, 101, 102
        };

        private byte[] _kXorOpad;

        private MD5 _md5;

        public Hmac(string key)
        {
            _kXorOpad = new byte[64];
            _md5 = new MD5CryptoServiceProvider();
            byte[] array;
            if (key.Length > 64)
            {
                MD5 mD = new MD5CryptoServiceProvider();
                byte[] bytes = GetBytes(key);
                array = mD.ComputeHash(bytes, 0, bytes.Length);
            }
            else
            {
                array = GetBytes(key);
            }

            byte[] array2 = new byte[64];
            checked
            {
                for (int i = 0; i < 64; i++)
                {
                    if (i < array.Length)
                    {
                        array2[i] = (byte)(array[i] ^ 0x36);
                        _kXorOpad[i] = (byte)(array[i] ^ 0x5C);
                    }
                    else
                    {
                        array2[i] = 54;
                        _kXorOpad[i] = 92;
                    }
                }

                Update(array2);
            }
        }

        internal static byte[] GetBytes(string s)
        {
            byte[] array = new byte[s.Length];
            checked
            {
                for (int i = 0; i < s.Length; i++)
                {
                    array[i] = (byte)s[i];
                }

                return array;
            }
        }

        public void Update(byte[] data)
        {
            _md5.TransformBlock(data, 0, data.Length, data, 0);
        }

        public byte[] Final()
        {
            _md5.TransformFinalBlock(new byte[0], 0, 0);
            byte[] hash = _md5.Hash;
            MD5 mD = new MD5CryptoServiceProvider();
            mD.TransformBlock(_kXorOpad, 0, _kXorOpad.Length, _kXorOpad, 0);
            mD.TransformFinalBlock(hash, 0, hash.Length);
            return HexString(mD.Hash);
        }

        internal static byte[] HexString(byte[] input)
        {
            checked
            {
                byte[] array = new byte[input.Length * 2];
                for (int i = 0; i < input.Length; i++)
                {
                    int num = (input[i] & 0xF0) >> 4;
                    int num2 = input[i] & 0xF;
                    array[i * 2] = hexChars[num];
                    array[i * 2 + 1] = hexChars[num2];
                }

                return array;
            }
        }
    }
}
