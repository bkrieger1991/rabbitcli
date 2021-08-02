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
        [Option("amqp", Required = false, HelpText = "Must be full uri to amqp address (e.g. amqp://localhost:5672)")]
        public string AmqpAddress { get; set; }
        [Option("web", Required = false, HelpText = "Must be full uri to web address (e.g. http://localhost:15672)")]
        public string WebInterfaceAddress { get; set; }
        [Option("name", Required = true, HelpText = "Name of your config you want to update. To update default config, give name \"default\"")]
        public string ConfigName { get; set; }
        [Option("delete", Required = false, HelpText = "Remove the configuration from the list of your configurations")]
        public bool Delete { get; set; }
    }
}