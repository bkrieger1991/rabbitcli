using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Library.Configuration;
using RabbitMQ.Library.Mappings;
using RabbitMQ.Library.Models;

namespace RabbitMQ.Library
{
    public static class ExtensionMethods
    {
        public static IServiceCollection AddRabbitMqLibraryComponents(this IServiceCollection services)
        {
            var configManager = new ConfigurationManager();
            configManager.Initialize();
            services.AddSingleton(configManager);

            services.AddSingleton<RabbitMqClient>();
            services.AddAutoMapper(cfg => cfg.AddMaps(typeof(MessageMapping).Assembly));

            return services;
        }

        public static IBasicProperties CreateBasicProperties(this IModel model, AmqpMessage message, IMapper mapper)
        {
            var basicProperties = model.CreateBasicProperties();
            mapper.Map(message.Properties, basicProperties);
            return basicProperties;
        }

        public static string Shorten(this string input, int length)
        {
            return input.Substring(0, Math.Min(input.Length, length));
        }

        public static string Encrypt(this string text, string key)
        {
            CheckKeyAndThrow(key);
            CheckTextAndThrow(text);

            var buffer = Encoding.UTF8.GetBytes(text);
            using var aes = CreateAes(key);

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            var result = Crypt(buffer, encryptor);

            var combined = new byte[aes.IV.Length + result.Length];
            Array.ConstrainedCopy(aes.IV, 0, combined, 0, aes.IV.Length);
            Array.ConstrainedCopy(result, 0, combined, aes.IV.Length, result.Length);

            return Convert.ToBase64String(combined);
        }

        public static string Decrypt(this string encryptedText, string key)
        {
            CheckKeyAndThrow(key);
            CheckTextAndThrow(encryptedText);

            var combined = Convert.FromBase64String(encryptedText);
            var buffer = new byte[combined.Length];
            using var aes = CreateAes(key);

            var iv = new byte[aes.IV.Length];
            var ciphertext = new byte[buffer.Length - iv.Length];
            Array.ConstrainedCopy(combined, 0, iv, 0, iv.Length);
            Array.ConstrainedCopy(combined, iv.Length, ciphertext, 0, ciphertext.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            return Encoding.UTF8.GetString(Crypt(ciphertext, decryptor));
        }

        private static byte[] Crypt(byte[] text, ICryptoTransform cryptTransform)
        {
            using var resultStream = new MemoryStream();
            using var aesStream = new CryptoStream(resultStream, cryptTransform, CryptoStreamMode.Write);
            using var plainStream = new MemoryStream(text);

            plainStream.CopyTo(aesStream);
            aesStream.Close();

            return resultStream.ToArray();
        }

        private static void CheckKeyAndThrow(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Key must have valid value.", nameof(key));
            }
        }

        private static void CheckTextAndThrow(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentException("The text to encrypt/decrypt must have valid value.", nameof(text));
            }
        }

        private static Aes CreateAes(string key)
        {
            var aes = Aes.Create();

            if (aes == null)
            {
                throw new ArgumentException("Parameter must not be null.", nameof(aes));
            }

            var aesKey = new byte[24];
            var hash = new SHA512CryptoServiceProvider();
            Buffer.BlockCopy(hash.ComputeHash(Encoding.UTF8.GetBytes(key)), 0, aesKey, 0, 24);
            aes.Key = aesKey;

            return aes;
        }
    }
}