using System;
using System.Text;
using System.Security.Cryptography;

namespace NATMP.Utilities
{
    public static class EncryptionUtility
    {
        private static readonly string EncryptionKey = "GamingForLife666"; // Nên lưu ở nơi bảo mật hơn

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return null;
            byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey);
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform encryptor = aes.CreateEncryptor();
                byte[] inputBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
                return Convert.ToBase64String(encryptedBytes);
            }
        }

        public static string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText)) return null;
            byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey);
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;
                ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }
    }
}