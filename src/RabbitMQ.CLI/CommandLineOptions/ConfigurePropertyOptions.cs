using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions
{
    [Verb("config-property", HelpText = "Read or edit configuration properties.")]
    public class ConfigurePropertyOptions
    {
        [Option("set", Required = false, HelpText = "Provide the property name you want to set")]
        public string SetProperty { get; set; }

        [Option("value", Required = false, HelpText = "Provide the value you want to set into property, provided with --set")]
        public string Value { get; set; }

        [Option("list", Required = false, HelpText = "Provide this option if you want to get a list of all properties and values")]
        public bool GetProperties { get; set; }
    }
}