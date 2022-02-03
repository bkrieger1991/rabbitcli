using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace RabbitMQ.CLI.Proxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static async Task RunWebHostAsync(string[] args, int port, CancellationToken token)
        {
            await CreateHostBuilder(args, port).Build().RunAsync(token);
        }

        private static IHostBuilder CreateHostBuilder(string[] args, int port = 5000)
        {
            var switchMappings = new Dictionary<string, string>()
            {
                ["--host"] = "RabbitMQ__Host",
                ["--port"] = "RabbitMQ__Port",
                ["--user"] = "RabbitMQ__Username",
                ["--password"] = "RabbitMQ__Password",
                ["--vhost"] = "RabbitMQ__VirtualHost",
                ["--header-blacklist"] = "RabbitMQ__HeaderBlacklist",
                ["--environment"] = "ASPNETCORE_ENVIRONMENT",
                ["--logging"] = "Logging__LogLevel__Default",
                ["-h"] = "RabbitMQ__Host",
                ["-p"] = "RabbitMQ__Port",
                ["-u"] = "RabbitMQ__Username",
                ["-s"] = "RabbitMQ__Password",
                ["-v"] = "RabbitMQ__VirtualHost"
            };

            return Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(config => config.AddCommandLine(args, switchMappings))
                .ConfigureWebHostDefaults(
                    webBuilder =>
                    {
                        webBuilder
                            .UseStartup<Startup>()
                            .UseUrls($"https://*:{port}");
                    });
        }
    }
}
