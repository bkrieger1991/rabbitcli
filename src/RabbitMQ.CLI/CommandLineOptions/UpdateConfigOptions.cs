using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions
{
    [Verb("update-config", HelpText = "Generates a config with given information and writes it to your user-directory into <rabbitcli.json>")]
    public class UpdateConfigOptions
    {
        [Option("user", Required = false)]
        public string Username { get; set; }
        [Option("password", Required = false)]
        public string Password { get; set; }
        [Option("vhost", Required = false)]
        public string VirtualHost { get; set; }
        [Option("amqp-host", Required = false, HelpText = "Provide your hostname, where your rabbitmq instance is hosted")]
        public string AmqpHostname { get; set; }
        [Option("amqp-port", Required = false, HelpText = "Enter the port of the AMQP protocol. Defaults to 5672")]
        public int AmqpPort { get; set; }
        [Option("web-port", Required = false, HelpText = "Enter the port of your management api. Defaults to 15672")]
        public int WebPort { get; set; }
        [Option("web-host", Required = false, HelpText = "If your AMQP and Management API Hostnames differ, provide the AMQP host in --host and the web-host in this option.")]
        public string WebHostname { get; set; }
        [Option("name", Required = false, HelpText = "Name of your config to refer to it in nearly all commands. If empty, it will be stored as default config")]
        public string ConfigName { get; set; }
        [Option("ssl", Required = false, HelpText = "Define whether your instance is setup using ssl or not")]
        public bool? Ssl { get; set; }
        [Option("delete", Required = false, HelpText = "Remove the configuration from the list of your configurations")]
        public bool Delete { get; set; }
    }
}