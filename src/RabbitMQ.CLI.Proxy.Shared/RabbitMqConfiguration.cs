namespace RabbitMQ.CLI.Proxy.Shared
{
    public class RabbitMqConfiguration
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
        public string HeaderBlacklist { get; set; }
        public string DefaultHeaderBlacklist { get; set; }
    }
}