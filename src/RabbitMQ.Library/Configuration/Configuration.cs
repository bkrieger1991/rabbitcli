using System.Collections.Generic;

namespace RabbitMQ.Library.Configuration
{
    public class Configuration
    {
        public string TextEditorPath { get; set; } = "notepad";
        public Dictionary<string, RabbitMqConfiguration> ConfigurationCollection { get; set; }
    }
}