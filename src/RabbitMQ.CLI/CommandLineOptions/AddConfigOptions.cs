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

        [Option("host", Required = true, Default = "localhost", HelpText = "Provide your hostname, where your rabbitmq instance is hosted")]
        public string Hostname { get; set; }
        [Option("amqp-port", Required = false, Default = 5672, HelpText = "Enter the port of the AMQP protocol. Defaults to 5672")]
        public int AmqpPort { get; set; }
        [Option("web-port", Required = false, Default = 15672, HelpText = "Enter the port of your management api. Defaults to 15672")]
        public int WebPort { get; set; }
        [Option("web-host", Required = false, HelpText = "If your AMQP and Management API Hostnames differ, provide the AMQP host in --host and the web-host in this option.")]
        public string WebHostname { get; set; }
        [Option("name", Required = false, Default = "default", HelpText = "Name of your config to refer to it in nearly all commands. If empty, it will be stored as default config")]
        public string ConfigName { get; set; }
        [Option("ssl", Required = false, Default = false, HelpText = "Define whether your instance is setup using ssl or not")]
        public bool Ssl { get; set; }
    }
}