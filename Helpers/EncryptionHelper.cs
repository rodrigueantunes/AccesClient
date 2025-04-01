using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AccesClientWPF.Helpers
{
    public static class EncryptionHelper
    {
        // Clé et vecteur d'initialisation fixes (à remplacer par une méthode plus sécurisée en production)
        private static readonly byte[] Key = new byte[32] { 14, 53, 124, 45, 12, 122, 35, 77, 99, 103, 109, 111, 113, 117, 119, 123, 127, 129, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197 };
        private static readonly byte[] IV = new byte[16] { 87, 103, 119, 137, 151, 167, 181, 197, 211, 229, 233, 239, 241, 251, 211, 193 };

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            byte[] encrypted;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Convertir les bytes en une chaîne Base64 pour le stockage
            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            string plaintext = string.Empty;
            try
            {
                byte[] cipherBytes = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = IV;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                    using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // En cas d'erreur de déchiffrement, retourne une chaîne vide
                return string.Empty;
            }

            return plaintext;
        }
    }
}