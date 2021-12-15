using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using ConsoleTables;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.CLI.CommandLineOptions;
using RabbitMQ.Library;
using RabbitMQ.Library.Configuration;
using Console = Colorful.Console;

namespace RabbitMQ.CLI.Processors
{
    public class ProxyProcessor
    {
        private readonly RabbitMqClient _rmqClient;
        private readonly ConfigurationManager _configManager;
        private readonly CancellationTokenSource _cts;
        private readonly CancellationToken _token;
        private RabbitMqConfiguration _config;

        public ProxyProcessor(RabbitMqClient rmqClient, ConfigurationManager configManager)
        {
            _rmqClient = rmqClient;
            _configManager = configManager;
            _cts = new CancellationTokenSource();
            _token = _cts.Token;
        }

        public async Task<int> CreateProxy(ProxyOptions options)
        {
            _config = _configManager.Get(options.ConfigName);
            Console.WriteLine("=== RabbitMQ HTTP Proxy by RabbitCLI ===");
            Console.WriteLine("Starting proxy WebServer -- Press CTRL+C to quit", Color.DarkGray);
            if (!CheckIfPortIsAvailable(options.Port))
            {
                Console.WriteLine($"Error: the port {options.Port} seems to be used by another program. Try choose another port with '--port' option.", Color.DarkRed);
                return 0;
            }

            try
            {
                Console.CancelKeyPress += CancellationHandler;
                OutputUsageInfo(options.Port);
                await StartWaitMessage();
                await CreateHostBuilder(options.Port).Build().RunAsync(_token);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message, Color.DarkRed);
                _cts.Cancel();
                Console.WriteLine("Server stopped", Color.DarkRed);
            }

            return 0;
        }

        public async Task HandleWebRequest(HttpContext context)
        {
            var headers = context.Request.Headers;
            var content = GetRequestContent(context.Request);
            _rmqClient.SetConfig(_config);
            _rmqClient.PublishMessage();
        }

        private IHostBuilder CreateHostBuilder(int port)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureHostConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    ["Logging:LogLevel:Default"] = "Information"
                }))
                .ConfigureLogging(c => c.AddConsole().AddDebug())
                .ConfigureServices(
                    services => services.AddSingleton(this)    
                )
                .ConfigureWebHostDefaults(
                    c => c.UseStartup<WebHostStartup>()
                        .UseUrls($"http://localhost:{port}")
                );
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
            Console.WriteLine("See also full documentation at: https://github.com/bkrieger1991/rabbitcli/tree/master#command-proxy", Color.ForestGreen);
            Console.WriteLine();
            var propTable = new ConsoleTable("Field", "Info")
            {
                Options = {EnableCount = false}
            };
            propTable.AddRow("Request Uri", $"http://localhost:{port}");
            propTable.AddRow("Request Method", "POST, PUT, PATCH");
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

        private async Task StartWaitMessage()
        {
            Console.WriteLine();
            Console.WriteLine("<Press any key to start immediately>");
            var delay = TimeSpan.FromSeconds(5);
            var startDate = DateTime.Now;
            var informTask = Task.Run(
                async () =>
                {
                    while (startDate + delay > DateTime.Now)
                    {
                        Console.Write($"Starting WebHost in: {((startDate + delay) - DateTime.Now).Seconds} seconds...\r");
                        await Task.Delay(200);
                    }
                }
            );
            var triggerTask = Task.Run(
                () =>
                {
                    Console.ReadKey();
                }
            );

            await Task.WhenAny(informTask, triggerTask);
        }

        private bool CheckIfPortIsAvailable(int port)
        {
            return IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .All(c => c.Port != port);
        }

        private string GetRequestContent(HttpRequest request)
        {
            using var mem = new MemoryStream();
            using var reader = new StreamReader(mem);
            request.Body.CopyTo(mem);
            var content = reader.ReadToEnd();
            mem.Seek(0, SeekOrigin.Begin);

            return content;
        }
    }

    public class WebHostStartup
    {
        public void ConfigureServices(IServiceCollection services)
        { }

        public void Configure(IApplicationBuilder app, ProxyProcessor proxyProcessor)
        {
            app.Map("", c => c.Run(proxyProcessor.HandleWebRequest));
        }
    }
}