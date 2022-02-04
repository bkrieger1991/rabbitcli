using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace RabbitMQ.CLI.Proxy.Shared
{
    public static class ProxyHostBuilder
    {
        public static IHostBuilder CreateHostBuilder(int port, string[] args)
        {
            var switchMappings = new Dictionary<string, string>()
            {
                ["--host"] = "RabbitMQ:Host",
                ["--port"] = "RabbitMQ:Port",
                ["--user"] = "RabbitMQ:Username",
                ["--username"] = "RabbitMQ:Username",
                ["--password"] = "RabbitMQ:Password",
                ["--vhost"] = "RabbitMQ:VirtualHost",
                ["--header-blacklist"] = "RabbitMQ:HeaderBlacklist",
                ["--environment"] = "ASPNETCORE_ENVIRONMENT",
                ["--logging"] = "Logging:LogLevel:Default",
                ["-h"] = "RabbitMQ:Host",
                ["-p"] = "RabbitMQ:Port",
                ["-u"] = "RabbitMQ:Username",
                ["-s"] = "RabbitMQ:Password",
                ["-v"] = "RabbitMQ:VirtualHost"
            };

            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(
                    webBuilder =>
                    {
                        webBuilder
                            .ConfigureAppConfiguration(c => c.AddCommandLine(args, switchMappings))
                            .UseStartup<Startup>()
                            .UseUrls($"http://*:{port}");
                    });
        }
    }
}