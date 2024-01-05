using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConsoleTables;
using Newtonsoft.Json;
using RabbitMQ.CLI.CommandLineOptions;
using RabbitMQ.Library;
using RabbitMQ.Library.Configuration;
using Console = Colorful.Console;

namespace RabbitMQ.CLI.Processors;

public class ConfigProcessor
{
    private readonly ConfigurationManager _configManager;

    public ConfigProcessor(ConfigurationManager configManager)
    {
        _configManager = configManager;
    }

    public Task HandleConfigCommand(ConfigOptions options)
    {
        // Normalize empty config name
        if (options.ConfigName.IsEmpty())
        {
            options.ConfigName = null;
        }

        var action = options.Action.ToEnum<ConfigOptions.Actions>();

        switch (action)
        {
            case ConfigOptions.Actions.Add:
                AddConfig(options.Parse());
                break;
            case ConfigOptions.Actions.Edit:
                EditConfig(options.Parse());
                break;
            case ConfigOptions.Actions.Delete:
                DeleteConfig(options.ConfigName);
                break;
            case ConfigOptions.Actions.Get:
                GetConfigs(options.ConfigName);
                break;
            case ConfigOptions.Actions.Use:
                SetAsDefault(options.ConfigName);
                break;
        }

        return Task.CompletedTask;
    }

    private void AddConfig(RabbitMqConfiguration config)
    {
        _configManager.AddConfiguration(config);
        Console.WriteLine();
        Console.WriteLineFormatted("Configuration stored. Name: {0}", config.Name, ConsoleColors.HighlightColor, ConsoleColors.DefaultColor);

    }
    private void DeleteConfig(string configName)
    {
        _configManager.RemoveConfiguration(configName);
        Console.WriteLine();
        Console.WriteLineFormatted("Configuration with name {0} deleted.", configName, ConsoleColors.HighlightColor, ConsoleColors.DefaultColor);
    }

    private void EditConfig(RabbitMqConfiguration config)
    {
        _configManager.UpdateConfiguration(config);
        Console.WriteLine();
        Console.WriteLine("Values of configuration updated:", ConsoleColors.DefaultColor);
        Console.WriteLine(JsonConvert.SerializeObject(config, Formatting.Indented), ConsoleColors.JsonColor);
    }

    private void SetAsDefault(string configName)
    {
        _configManager.SetProperty(nameof(Configuration.DefaultConfiguration), configName);
        Console.WriteLine();
        Console.WriteLine("Configured new default: {0}", configName, ConsoleColors.HighlightColor, ConsoleColors.DefaultColor);
    }

    private void GetConfigs(string configName = null)
    {
        if (!string.IsNullOrWhiteSpace(configName))
        {
            var config = _configManager.Get(configName);
            Console.WriteLine(JsonConvert.SerializeObject(config, Formatting.Indented), ConsoleColors.JsonColor);
            Console.WriteLine();
            Console.WriteLine("CLI-Parameters for edit:");
            Console.WriteLine(
                "rabbitcli config edit " +
                $"--name {config.Name} " +
                $"--amqp {config.Amqp.ToUri()} " +
                $"--web {config.Web.ToUri()}" +
                $"{(config.Amqp.Unsecure ? " --ignore-invalid-cert" : "")}" +
                $"{(!config.Amqp.TlsVersion.IsEmpty() ? $"--amqps-tls-version {config.Amqp.TlsVersion}" : "")}" +
                $"{(!config.Amqp.TlsServerName.IsEmpty() ? $"--amqps-tls-server {config.Amqp.TlsServerName}" : "")}", ConsoleColors.HighlightColor);
            return;
        }

        var configKeys = _configManager.GetConfigurationKeys();
        if (configKeys.Length == 0)
        {
            Console.WriteLine("There are no configurations yet");
        }
        configKeys.ToList().ForEach(Console.WriteLine);
    }

    public Task HandlePropertyCommand(PropertyOptions options)
    {
        var action = options.Action.ToEnum<PropertyOptions.Actions>();
        switch (action)
        {
            case PropertyOptions.Actions.Get:
                WritePropertyTable();
                break;
            case PropertyOptions.Actions.Set:
                _configManager.SetProperty(options.Name, options.Value);
                break;
        }
        return Task.CompletedTask;
    }

    private void WritePropertyTable()
    {
        var props = typeof(Configuration).GetProperties()
            .Where(p => p.Name != nameof(Configuration.ConfigurationCollection))
            .ToList();

        var table = new ConsoleTable("Property", "Current value");
        props.ForEach(p => table.AddRow(p.Name, _configManager.GetProperty(p.Name)));

        table.Write();
    }
}