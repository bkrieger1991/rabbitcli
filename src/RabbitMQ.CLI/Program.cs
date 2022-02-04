using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.CLI.Processors;
using RabbitMQ.Library;

namespace RabbitMQ.CLI
{
    public class Program
    {
        private const string VerboseArgument = "--verbose";

        public static async Task Main(string[] args)
        {
            // Check if args contain a verbosity switch.
            var verbose = args.Contains(VerboseArgument);
            if (verbose)
            {
                // Remove it from args to not confuse the commandline-processor.
                var argList = args.ToList();
                argList.Remove(VerboseArgument);
                args = argList.ToArray();
            }

            var provider = new ServiceCollection()
                // Add logging with filter for warning/errors - or for verbose enabled: everything.
                .AddLogging(c => c.AddConsole().AddFilter(level => verbose || level >= LogLevel.Warning))
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
