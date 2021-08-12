﻿using System;
using System.Linq;
using System.Threading.Tasks;
using ConsoleTables;
using Newtonsoft.Json;
using RabbitMQ.CLI.CommandLineOptions;
using RabbitMQ.Library.Configuration;
using Console = Colorful.Console;

namespace RabbitMQ.CLI.Processors
{
    public class ConfigProcessor
    {
        private readonly ConfigurationManager _configManager;

        public ConfigProcessor(ConfigurationManager configManager)
        {
            _configManager = configManager;
        }

        public Task<int> AddConfig(AddConfigOptions options)
        {
            // Normalize empty config name
            if (string.IsNullOrWhiteSpace(options.ConfigName))
            {
                options.ConfigName = null;
            }

            var config = RabbitMqConfiguration.Create(
                options.Username, options.Password,
                options.VirtualHost, options.AmqpAddress,
                options.WebInterfaceAddress, options.ConfigName
            );

            _configManager.AddConfiguration(config);

            Console.WriteLine();
            Console.Write("Configuration stored. Name: ", ConsoleColors.DefaultColor);
            Console.WriteLine(config.Name, ConsoleColors.HighlightColor);
            return Task.FromResult(0);
        }

        public Task<int> UpdateConfig(UpdateConfigOptions options)
        {
            if (options.Delete)
            {
                _configManager.RemoveConfiguration(options.ConfigName);
                return Task.FromResult(0);
            }

            var config = _configManager.Get(options.ConfigName);
            config.Password = options.Password ?? config.Password;
            config.Username = options.Username ?? config.Username;
            config.VirtualHost = options.VirtualHost ?? config.VirtualHost;

            if (!string.IsNullOrWhiteSpace(options.AmqpAddress))
            {
                var uri = new Uri(options.AmqpAddress);
                config.AmqpAddress = uri.Host;
                config.AmqpPort = uri.Port;
            }

            if (!string.IsNullOrWhiteSpace(options.WebInterfaceAddress))
            {
                var uri = new Uri(options.WebInterfaceAddress);
                config.WebInterfaceAddress = uri.Host;
                config.WebInterfacePort = uri.Port;
            }

            _configManager.UpdateConfiguration(config);
            return Task.FromResult(0);
        }

        public Task<int> GetConfigs(GetConfigOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.ConfigName))
            {
                var config = _configManager.Get(options.ConfigName);
                Console.WriteLine(JsonConvert.SerializeObject(config, Formatting.Indented), ConsoleColors.JsonColor);
                return Task.FromResult(0);
            }

            var configKeys = _configManager.GetConfigurationKeys();
            configKeys.ToList().ForEach(Console.WriteLine);
            return Task.FromResult(0);
        }

        public Task<int> ConfigureProperty(ConfigurePropertyOptions options)
        {
            if (options.GetProperties)
            {
                var props = typeof(Configuration).GetProperties()
                    .Where(p => p.Name != nameof(Configuration.ConfigurationCollection))
                    .ToList();

                var table = new ConsoleTable("Property", "Current value");
                props.ForEach(p => table.AddRow(p.Name, _configManager.GetProperty(p.Name)));

                table.Write();
                return Task.FromResult(0);
            }

            if (!string.IsNullOrWhiteSpace(options.SetProperty))
            {
                if (string.IsNullOrWhiteSpace(options.Value))
                {
                    throw new Exception("You have to provide a value in order to set a configuration property");
                }

                _configManager.SetProperty(options.SetProperty, options.Value);
            }

            return Task.FromResult(0);
        }
    }
}