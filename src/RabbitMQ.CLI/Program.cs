using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.CLI.Processors;
using RabbitMQ.Library;

namespace RabbitMQ.CLI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var provider = new ServiceCollection()
                .AddRabbitMqLibraryComponents()
                .AddSingleton<CommandLineProcessor>()
                .AddSingleton<ConfigProcessor>()
                .AddSingleton<MessageProcessor>()
                .AddSingleton<QueueProcessor>()
                .AddSingleton<ProxyProcessor>()
                .BuildServiceProvider();

            await provider.GetRequiredService<CommandLineProcessor>().Execute(args);
        }
    }
}
