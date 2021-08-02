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

        public static RabbitMqConfiguration Create(string username, string password, string virtualHost,
            string amqpAddress, string webInterfaceAddress, string name = "default")
        {
            var amqpUri = new Uri(amqpAddress);
            var webUri = new Uri(webInterfaceAddress);
            return Create(username, password, virtualHost, amqpUri, webUri, name, webUri.Scheme == "https");
        }

        public static RabbitMqConfiguration Create(string username, string password, string virtualHost,
            Uri amqpAddress, Uri webInterfaceAddress, string name = "default", bool ssl = false)
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
                AmqpAddress = amqpAddress.Host,
                AmqpPort = amqpAddress.Port,
                WebInterfaceAddress = webInterfaceAddress.Host,
                WebInterfacePort = webInterfaceAddress.Port,
                Ssl = ssl
            };
        }
    }
}