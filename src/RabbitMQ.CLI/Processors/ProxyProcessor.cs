using System;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using ConsoleTables;
using Microsoft.Extensions.Hosting;
using RabbitMQ.CLI.CommandLineOptions;
using RabbitMQ.CLI.Proxy.Shared;
using RabbitMQ.Library.Configuration;
using Console = Colorful.Console;

namespace RabbitMQ.CLI.Processors;

public class ProxyProcessor
{
    private readonly ConfigurationManager _configManager;
    private readonly CancellationTokenSource _cts;

    public ProxyProcessor(ConfigurationManager configManager)
    {
        _configManager = configManager;
        _cts = new CancellationTokenSource();
    }

    public async Task<int> CreateProxy(ProxyOptions options)
    {
        var config = _configManager.Get(options.ConfigName);
        if (!options.Headless)
        {
            Console.WriteLine("=== RabbitMQ HTTP Proxy by RabbitCLI ===");
            Console.WriteLine("Starting proxy WebServer -- Press CTRL+C to quit", Color.DarkGray);
        }

        if (!CheckIfPortIsAvailable(options.Port))
        {
            Console.WriteLine($"Error: the port {options.Port} seems to be used by another program. Try choose another port with '--port' option.", Color.DarkRed);
            return 0;
        }

        try
        {
            if (!options.Headless)
            {
                Console.CancelKeyPress += CancellationHandler;
                OutputUsageInfo(options.Port);
            }

            var host = ProxyHostBuilder.CreateHostBuilder(
                options.Port, 
                new[]
                {
                    "--enable-swagger=true",
                    "--logging=trace",
                    $"--host={config.Amqp.Hostname}",
                    $"--port={config.Amqp.Port}",
                    $"--username={config.Amqp.Username}",
                    $"--password={config.Amqp.Password}",
                    $"--vhost={config.Amqp.VirtualHost}",
                    $"--header-blacklist={options.ExceptHeaders}"
                }
            );

            await host.Build().RunAsync(_cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message, Color.DarkRed);
            _cts.Cancel();
            Console.WriteLine("Server stopped", Color.DarkRed);
        }

        return 0;
    }

    private void CancellationHandler(object sender, ConsoleCancelEventArgs args)
    {
        Console.WriteLine("Stopping proxy service...");
        _cts.Cancel();
        args.Cancel = true;
    }

    private void OutputUsageInfo(int port)
    {
        Console.WriteLine();
        Console.WriteLine("See also full documentation at: https://github.com/bkrieger1991/rabbitcli/tree/master#http-proxy-command-proxy", Color.ForestGreen);
        Console.WriteLine();
        var propTable = new ConsoleTable("Field", "Info")
        {
            Options = { EnableCount = false }
        };
        propTable.AddRow("Request Uri", $"http://localhost:{port}");
        propTable.AddRow("Request Method", "POST");
        propTable.AddRow("Request Body", "<content of your message>");
        propTable.Write();
        Console.WriteLine("Request Headers:");
        var headerTable = new ConsoleTable("Header", "Description")
        {
            Options = { EnableCount = false }
        };
        headerTable.AddRow(
            "X-RoutingKey",
            "Define the routing-key which is used to publish the message"
        );
        headerTable.AddRow(
            "X-Exchange",
            "Name of the exchange, where the message gets published"
        );
        headerTable.AddRow(
            "X-Queue",
            "Name of the queue, where the message gets published (alternative to 'X-Exchange')"
        );
        headerTable.AddRow(
            "Content-Type",
            "The provided content-type gets set into message-property 'content_type'"
        );
        headerTable.AddRow(
            "RMQ-*",
            "All headers provided with 'RMQ-' prefix, will be set into publish-properties. See documentation for a list of possible options."
        );
        headerTable.AddRow(
            "*",
            "Every request header, that is not directly used as described here, will be set as header in the published message"
        );
        headerTable.Write();
    }

    private bool CheckIfPortIsAvailable(int port)
    {
        return IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .All(c => c.Port != port);
    }
}