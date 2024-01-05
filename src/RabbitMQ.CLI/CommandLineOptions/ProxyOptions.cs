using CommandLine;

namespace RabbitMQ.CLI.CommandLineOptions;

[Verb("proxy", HelpText = "Starts a WebHost that helps you easily publish messages to RabbitMQ by using a tool of your choice to manage HTTP calls")]
public class ProxyOptions : DefaultOptions, ICommandLineOption
{
    [Option("port", Required = false, Default = 15673, HelpText = "Provide a port if the default port collides with your environment")]
    public int Port { get; set; }

    [Option("except-headers", Required = false, Default = "Content-Length,Host,User-Agent,Accept,Accept-Encoding,Connection,Cache-Control", HelpText = "Provide a list of header-keys you don't want to get published")]
    public string ExceptHeaders { get; set; }

    [Option("headless", Required = false, Default = false, HelpText = "Reduces console-output and removes delay before starting. Also removes custom handling of CTRL+C.")]
    public bool Headless { get; set; }

    public void Validate()
    {
        // Nothing to validate here
    }
}