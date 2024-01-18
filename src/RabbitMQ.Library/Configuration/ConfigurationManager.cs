using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace RabbitMQ.Library.Configuration;

public class ConfigurationManager
{
    private Configuration _config;
        
    public static string GetEncryptionKey()
    {
        return Environment.MachineName + Environment.UserName + "!#RabbitMqCli#!";
    }

    public void Initialize()
    {
        EnsureConfigFileExists();
        LoadConfiguration();
    }

    public void SetProperty(string property, string value)
    {
        var prop = typeof(Configuration).GetProperty(property,
            BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
        if (prop == null)
        {
            throw new ArgumentException($"No property found with name '{property}'");
        }
        prop.SetValue(_config, value);

        SaveConfiguration();
    }

    public string GetProperty(string property)
    {
        var prop = typeof(Configuration).GetProperty(property,
            BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
        if (prop == null)
        {
            throw new ArgumentException($"No property found with name '{property}'");
        }
        return (string)prop.GetValue(_config);
    }

    public RabbitMqConfiguration Get(string name)
    {
        if (name.IsEmpty())
        {
            name = _config.DefaultConfiguration;
        }

        if (!_config.ConfigurationCollection.ContainsKey(name))
        {
            ThrowConfigNotExists(name);
        }

        return _config.ConfigurationCollection[name];
    }

    public void AddConfiguration(RabbitMqConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
        {
            config.Name = "default";
        }

        if (_config.ConfigurationCollection.ContainsKey(config.Name))
        {
            throw new ArgumentException($"Configuration with the name \"{config.Name}\" already exists.");
        }

        _config.ConfigurationCollection.Add(config.Name, config);

        SaveConfiguration();
    }

    public void UpdateConfiguration(RabbitMqConfiguration config)
    {
        if (!_config.ConfigurationCollection.ContainsKey(config.Name))
        {
            ThrowConfigNotExists(config.Name);
        }

        _config.ConfigurationCollection[config.Name] = config;

        SaveConfiguration();
    }

    public void RemoveConfiguration(string name)
    {
        if (!_config.ConfigurationCollection.ContainsKey(name))
        {
            ThrowConfigNotExists(name);
        }

        _config.ConfigurationCollection.Remove(name);

        SaveConfiguration();
    }

    public string[] GetConfigurationKeys()
    {
        return _config.ConfigurationCollection.Keys.ToArray();
    }

    protected virtual void WriteConfiguration(string data)
    {
        File.WriteAllText(GetConfigFilePath(), data);
    }

    protected virtual string ReadConfiguration()
    {
        return File.ReadAllText(GetConfigFilePath());
    }

    private void SaveConfiguration()
    {
        WriteConfiguration(SerializeConfiguration(_config));
    }

    private void LoadConfiguration()
    {
        var json = ReadConfiguration();
        _config = DeserializeConfiguration(json);
    }

    protected virtual void EnsureConfigFileExists()
    {
        if (File.Exists(GetConfigFilePath()))
        {
            return;
        }

        _config = new Configuration()
        {
            ConfigurationCollection = new Dictionary<string, RabbitMqConfiguration>()
        };
        SaveConfiguration();
    }

    private string GetConfigFilePath()
    {
        var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userDir, "rabbitcli.json");
    }

    private string SerializeConfiguration(Configuration config)
    {
        var props = typeof(Configuration).GetProperties();
        var dict = props.Where(p => p.Name != nameof(config.ConfigurationCollection))
            .ToDictionary(p => p.Name, p => p.GetValue(config));
        dict.Add(nameof(config.ConfigurationCollection), SerializeConfigCollection(config.ConfigurationCollection));

        return JsonConvert.SerializeObject(dict);
    }

    private Dictionary<string, string> SerializeConfigCollection(Dictionary<string, RabbitMqConfiguration> configCollection)
    {
        var encryptedConfigs = configCollection
            .Select(kv =>
                new KeyValuePair<string, string>(
                    kv.Key,
                    JsonConvert.SerializeObject(kv.Value).Encrypt(GetEncryptionKey())
                ))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        return encryptedConfigs;
    }

    private Configuration DeserializeConfiguration(string json)
    {
        // Note:
        // Some kind of manual deserialization here.
        // Cause: the serialized file content contains a key-value pair in "ConfigurationCollection" with encrypted values
        // We don't want that encrypted values in our configuration object, so we first take all configurable properties
        // And then read decrypted values from ConfigurationCollection dictionary

        var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        var config = new Configuration();

        var props = typeof(Configuration).GetProperties();
        props.Where(p => p.Name != nameof(config.ConfigurationCollection))
            .Where(p => dict.Keys.Contains(p.Name))
            .ToList()
            .ForEach(p => p.SetValue(config, dict[p.Name]));
            
        // Value in key "ConfigurationCollection" is a JObject, convert it into Dict<string,string> to better handle it
        var collection = dict[nameof(config.ConfigurationCollection)];
        var encryptedDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(collection));
        config.ConfigurationCollection = DeserializeConfigCollection(encryptedDict);
            
        return config;
    }

    private Dictionary<string, RabbitMqConfiguration> DeserializeConfigCollection(Dictionary<string, string> encryptedCollection)
    {
        return encryptedCollection.Select(kv => 
                new KeyValuePair<string, RabbitMqConfiguration>(
                    kv.Key,
                    JsonConvert.DeserializeObject<RabbitMqConfiguration>(kv.Value.Decrypt(GetEncryptionKey()))
                ))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    private void ThrowConfigNotExists(string configName)
    {
        throw new ArgumentException($"Configuration with the name \"{configName}\" does not exists.");
    }
}