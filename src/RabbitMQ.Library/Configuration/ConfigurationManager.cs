using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace RabbitMQ.Library.Configuration
{
    public class ConfigurationManager
    {
        protected Dictionary<string, RabbitMqConfiguration> ConfigurationCollection { get; set; }
        private string _encryptionKey;

        public ConfigurationManager()
        {
            //_encryptionKey = Environment.MachineName + Environment.UserName + "!#RabbitMqCli#!";
            _encryptionKey = "!#RabbitMqCli#!";

            EnsureConfigFileExists();

            LoadConfiguration();
        }

        public bool Exists(string name)
        {
            return ConfigurationCollection.ContainsKey(name);
        }

        public RabbitMqConfiguration Get(string name)
        {
            if (!ConfigurationCollection.ContainsKey(name))
            {
                throw new Exception($"Configuration with the name \"{name}\" does not exists.");
            }

            return ConfigurationCollection[name];
        }

        public void AddConfiguration(RabbitMqConfiguration config)
        {
            if (ConfigurationCollection.ContainsKey(config.Name))
            {
                throw new Exception($"Configuration with the name \"{config.Name}\" already exists.");
            }

            ConfigurationCollection.Add(config.Name, config);

            SaveConfiguration();
        }

        public void UpdateConfiguration(RabbitMqConfiguration config)
        {
            if (!ConfigurationCollection.ContainsKey(config.Name))
            {
                throw new Exception($"Configuration with the name \"{config.Name}\" does not exists.");
            }

            ConfigurationCollection[config.Name] = config;

            SaveConfiguration();
        }

        public void RemoveConfiguration(string name)
        {
            if (!ConfigurationCollection.ContainsKey(name))
            {
                throw new Exception($"Configuration with the name \"{name}\" does not exists.");
            }

            ConfigurationCollection.Remove(name);

            SaveConfiguration();
        }

        public string[] GetConfigurationKeys()
        {
            return ConfigurationCollection.Keys.ToArray();
        }

        private void SaveConfiguration()
        {
            File.WriteAllText(GetConfigFilePath(), SerializeConfigCollection(ConfigurationCollection));
        }

        private void LoadConfiguration()
        {
            var json = File.ReadAllText(GetConfigFilePath());

            ConfigurationCollection = DeserializeConfigJson(json);
        }

        private void EnsureConfigFileExists()
        {
            if (!File.Exists(GetConfigFilePath()))
            {
                ConfigurationCollection = new Dictionary<string, RabbitMqConfiguration>();
                SaveConfiguration();
            }
        }

        private string GetConfigFilePath()
        {
            var userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userDir, "rabbitcli.json");
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

        private Dictionary<string, RabbitMqConfiguration> DeserializeConfigJson(string json)
        {
            var encryptedConfigs = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            return encryptedConfigs.Select(kv => 
                    new KeyValuePair<string, RabbitMqConfiguration>(
                        kv.Key,
                        JsonConvert.DeserializeObject<RabbitMqConfiguration>(kv.Value.Decrypt(_encryptionKey))
                    ))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}
