using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions
{
    [Verb("add-config", HelpText = "Generates a config with given information and writes it to your user-directory into <rabbitcli.json>")]
    public class AddConfigOptions
    {
        [Option("user", Required = true)]
        public string Username { get; set; }
        [Option("password", Required = true)]
        public string Password { get; set; }
        [Option("vhost", Required = true)]
        public string VirtualHost { get; set; }
        [Option("amqp", Required = true, HelpText = "Must be full uri to amqp address (e.g. amqp://localhost:5672)")]
        public string AmqpAddress { get; set; }
        [Option("web", Required = true, HelpText = "Must be full uri to web address (e.g. http://localhost:15672)")]
        public string WebInterfaceAddress { get; set; }
        [Option("name", Required = false, Default = "default", HelpText = "Name of your config to refer to it in nearly all commands. If empty, it will be stored as default config")]
        public string ConfigName { get; set; }
    }
}