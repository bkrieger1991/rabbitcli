﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using AutoMapper;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Library.Configuration;
using RabbitMQ.Library.Mappings;
using RabbitMQ.Library.Models;

namespace RabbitMQ.Library;

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

    public static string FromBase64(this string base64)
    {
        return Encoding.UTF8.GetString(
            Convert.FromBase64String(base64)
        );
    }

    /// <summary>
    /// Performs string.IsNullOrWhiteSpace() on value.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsEmpty(this string value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Checks whether the string "value" is equal to the name of given enumValue.
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="value"></param>
    /// <param name="enumValue"></param>
    /// <param name="caseSensitive"></param>
    /// <returns></returns>
    public static bool Is<TEnum>(this string value, TEnum enumValue, bool caseSensitive = false) where TEnum : struct, IConvertible
    {
        if (!typeof(TEnum).IsEnum)
        {
            throw new ArgumentException("TEnum must be an enumerated type");
        }

        return value.Equals(
            enumValue.ToString(),
            caseSensitive
                ? StringComparison.InvariantCulture
                : StringComparison.InvariantCultureIgnoreCase
        );
    }

    /// <summary>
    /// True if the string value is equal to at least one of the given enum values
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="value"></param>
    /// <param name="enumValues"></param>
    /// <param name="caseSensitive"></param>
    /// <returns></returns>
    public static bool IsIn<TEnum>(
        this string value,
        TEnum[] enumValues,
        bool caseSensitive = false
    )
        where TEnum : struct, IConvertible
    {
        if (!typeof(TEnum).IsEnum)
        {
            throw new ArgumentException("TEnum must be an enumerated type");
        }

        return enumValues
            .Select(e => e.ToString())
            .Contains(value, caseSensitive ? StringComparer.InvariantCulture : StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Tries to convert the given string value into the given enum type value.
    /// Uses Enum.Parse<> and throws same exceptions.
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static TEnum ToEnum<TEnum>(this string value) where TEnum : struct, IConvertible
    {
        if (!typeof(TEnum).IsEnum)
        {
            throw new ArgumentException("TEnum must be an enumerated type");
        }

        return Enum.Parse<TEnum>(value, true);
    }

    /// <summary>
    /// Returns true, if none of the items given in "exclude" are contained in the string value
    /// </summary>
    /// <param name="value"></param>
    /// <param name="exclude"></param>
    /// <returns></returns>
    public static bool ContainsNoneOf(this string value, IEnumerable<string> exclude)
    {
        var lowerValue = value.ToLower();
        return !exclude.Any(e => lowerValue.Contains(e.ToLower()));
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
            
        // If we are getting executed on windows, we have the possibility to encrypt data in
        // user-scope without providing any kind of secret for encryption
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Convert.ToBase64String(ProtectedData.Protect(
                buffer,
                Encoding.UTF8.GetBytes(key),
                DataProtectionScope.CurrentUser
            ));
        }

        // TODO: Find something for linux and mac to encrypt without using a predicatable secret
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

        var encrypted = Convert.FromBase64String(encryptedText);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(
                encrypted,
                Encoding.UTF8.GetBytes(key),
                DataProtectionScope.CurrentUser
            ));
        }

        var buffer = new byte[encrypted.Length];
        using var aes = CreateAes(key);

        var iv = new byte[aes.IV.Length];
        var ciphertext = new byte[buffer.Length - iv.Length];
        Array.ConstrainedCopy(encrypted, 0, iv, 0, iv.Length);
        Array.ConstrainedCopy(encrypted, iv.Length, ciphertext, 0, ciphertext.Length);
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

    [AssertionMethod]
    private static void CheckKeyAndThrow(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key must have valid value.", nameof(key));
        }
    }

    [AssertionMethod]
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
        var hash = SHA512.Create();
        Buffer.BlockCopy(hash.ComputeHash(Encoding.UTF8.GetBytes(key)), 0, aesKey, 0, 24);
        aes.Key = aesKey;

        return aes;
    }
}