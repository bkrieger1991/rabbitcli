using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions
{
    public class DefaultOptions
    {
        [Option('c', "config", Required = false, Default = "default", HelpText = "Name of a specific config you want to use (earlier added by <add-config> command)")]
        public string ConfigName { get; set; }
    }
}