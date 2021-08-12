using System;

namespace RabbitMQ.Library.Configuration
{
    public class RabbitMqConfiguration
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string VirtualHost { get; set; }
        public string Name { get; set; }
        public string AmqpAddress { get; set; }
        public int AmqpPort { get; set; }
        public string WebInterfaceAddress { get; set; }
        public int WebInterfacePort { get; set; }
        public bool Ssl { get; set; }

        public static RabbitMqConfiguration Create(
            string username, string password, string virtualHost,
            string amqpHost, int amqpPort, string webHost,
            int webPort, bool ssl = false, string name = "default"
        )
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "default";
            }

            return new()
            {
                Username = username,
                Password = password,
                VirtualHost = virtualHost,
                Name = name,
                AmqpAddress = amqpHost,
                AmqpPort = amqpPort,
                WebInterfaceAddress = webHost,
                WebInterfacePort = webPort,
                Ssl = ssl
            };
        }
    }
}