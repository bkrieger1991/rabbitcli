﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace RabbitMQ.Library.Configuration
{
    public class ConfigurationManager
    {
        private Configuration _config;
        private string _encryptionKey;

        public ConfigurationManager()
        {
            //_encryptionKey = Environment.MachineName + Environment.UserName + "!#RabbitMqCli#!";
            _encryptionKey = "!#RabbitMqCli#!";
        }

        public void Initialize()
        {
            EnsureConfigFileExists();
            LoadConfiguration();
        }

        public bool Exists(string name)
        {
            return _config.ConfigurationCollection.ContainsKey(name);
        }

        public void SetProperty(string property, string value)
        {
            var prop = typeof(Configuration).GetProperty(property,
                BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
            if (prop == null)
            {
                throw new Exception($"No property found with name '{property}'");
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
                throw new Exception($"No property found with name '{property}'");
            }
            return (string)prop.GetValue(_config);
        }

        public RabbitMqConfiguration Get(string name)
        {
            if (!_config.ConfigurationCollection.ContainsKey(name))
            {
                throw new Exception($"Configuration with the name \"{name}\" does not exists.");
            }

            return _config.ConfigurationCollection[name];
        }

        public void AddConfiguration(RabbitMqConfiguration config)
        {
            if (_config.ConfigurationCollection.ContainsKey(config.Name))
            {
                throw new Exception($"Configuration with the name \"{config.Name}\" already exists.");
            }

            _config.ConfigurationCollection.Add(config.Name, config);

            SaveConfiguration();
        }

        public void UpdateConfiguration(RabbitMqConfiguration config)
        {
            if (!_config.ConfigurationCollection.ContainsKey(config.Name))
            {
                throw new Exception($"Configuration with the name \"{config.Name}\" does not exists.");
            }

            _config.ConfigurationCollection[config.Name] = config;

            SaveConfiguration();
        }

        public void RemoveConfiguration(string name)
        {
            if (!_config.ConfigurationCollection.ContainsKey(name))
            {
                throw new Exception($"Configuration with the name \"{name}\" does not exists.");
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
            if (!File.Exists(GetConfigFilePath()))
            {
                _config = new Configuration();
                SaveConfiguration();
            }
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

        private string SerializeConfigCollection(Dictionary<string, RabbitMqConfiguration> configCollection)
        {
            var encryptedConfigs = configCollection
                .Select(kv =>
                    new KeyValuePair<string, string>(
                        kv.Key,
                        JsonConvert.SerializeObject(kv.Value).Encrypt(_encryptionKey)
                    ))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            return JsonConvert.SerializeObject(encryptedConfigs);
        }

        private Configuration DeserializeConfiguration(string json)
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            var config = new Configuration();
            
            // Compatibility to old configs
            if (!dict.ContainsKey(nameof(config.ConfigurationCollection)))
            {
                config.ConfigurationCollection = DeserializeConfigCollection(dict.ToDictionary(d => d.Key, d => d.Value.ToString()));
                WriteConfiguration(SerializeConfiguration(config));
                return config;
            }
            var props = typeof(Configuration).GetProperties();
            props.Where(p => p.Name != nameof(config.ConfigurationCollection)).ToList().ForEach(p => p.SetValue(config, dict[p.Name]));
            var jobject = dict[nameof(config.ConfigurationCollection)];
            var encryptedDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(jobject));
            config.ConfigurationCollection = DeserializeConfigCollection(encryptedDict);
            
            return config;
        }

        private Dictionary<string, RabbitMqConfiguration> DeserializeConfigCollection(Dictionary<string, string> encryptedCollection)
        {
            return encryptedCollection.Select(kv => 
                    new KeyValuePair<string, RabbitMqConfiguration>(
                        kv.Key,
                        JsonConvert.DeserializeObject<RabbitMqConfiguration>(kv.Value.Decrypt(_encryptionKey))
                    ))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}
