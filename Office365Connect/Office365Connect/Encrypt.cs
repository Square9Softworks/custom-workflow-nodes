using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SharePointRelease
{
    public static class Encrypt
    {
        private const string initVector = "pemgail9uzpgzl88";

        private const int keysize = 256;

        public static string EncryptString(string plainText, string passPhrase)
        {
            byte[] bytes = Encoding.UTF8.GetBytes("pemgail9uzpgzl88");
            byte[] bytes2 = Encoding.UTF8.GetBytes(plainText);
            PasswordDeriveBytes passwordDeriveBytes = new PasswordDeriveBytes(passPhrase, null);
            byte[] bytes3 = passwordDeriveBytes.GetBytes(32);
            ICryptoTransform transform = new RijndaelManaged
            {
                Mode = CipherMode.CBC
            }.CreateEncryptor(bytes3, bytes);
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
            cryptoStream.Write(bytes2, 0, bytes2.Length);
            cryptoStream.FlushFinalBlock();
            byte[] inArray = memoryStream.ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            return Convert.ToBase64String(inArray);
        }

        public static string DecryptString(string cipherText, string passPhrase)
        {
            string @string;
            try
            {
                byte[] bytes = Encoding.ASCII.GetBytes("pemgail9uzpgzl88");
                byte[] array = Convert.FromBase64String(cipherText);
                PasswordDeriveBytes passwordDeriveBytes = new PasswordDeriveBytes(passPhrase, null);
                byte[] bytes2 = passwordDeriveBytes.GetBytes(32);
                ICryptoTransform transform = new RijndaelManaged
                {
                    Mode = CipherMode.CBC
                }.CreateDecryptor(bytes2, bytes);
                MemoryStream memoryStream = new MemoryStream(array);
                CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read);
                byte[] array2 = new byte[array.Length];
                int count = cryptoStream.Read(array2, 0, array2.Length);
                memoryStream.Close();
                cryptoStream.Close();
                @string = Encoding.UTF8.GetString(array2, 0, count);
            }
            catch (Exception ex)
            {
                throw new Exception(DateTime.Now.ToString() + "\tError in password decryption: " + ex.Message);
            }
            return @string;
        }
    }
}