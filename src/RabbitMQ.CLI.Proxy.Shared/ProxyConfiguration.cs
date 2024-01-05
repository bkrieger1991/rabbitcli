using JetBrains.Annotations;

namespace RabbitMQ.CLI.Proxy.Shared;

public class ProxyConfiguration
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; }

    [CanBeNull]
    public string HeaderBlacklist { get; set; }
    [CanBeNull]
    public string DefaultHeaderBlacklist { get; set; }
}