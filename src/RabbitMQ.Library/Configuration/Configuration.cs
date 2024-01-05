using System.Collections.Generic;

namespace RabbitMQ.Library.Configuration;

public class Configuration
{
    public string TextEditorPath { get; set; } = "notepad";
    public string DefaultConfiguration { get; set; } = "default";
    public Dictionary<string, RabbitMqConfiguration> ConfigurationCollection { get; set; }
}