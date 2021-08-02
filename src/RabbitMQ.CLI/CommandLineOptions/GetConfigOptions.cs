using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions
{
    [Verb("get-configs", HelpText = "Get a list of stored configuration names, provide a name to output the configuration values.")]
    public class GetConfigOptions
    {
        [Option("name", Required = false, HelpText = "Define the name of the configuration you want to display.")]
        public string ConfigName { get; set; }
    }
}