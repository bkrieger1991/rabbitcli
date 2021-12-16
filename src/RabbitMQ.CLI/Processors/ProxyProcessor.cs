using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
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
        private const string ExchangeHeaderKey = "X-Exchange";
        private const string RoutingKeyHeaderKey = "X-RoutingKey";
        private const string QueueHeaderKey = "X-Queue";

        private readonly RabbitMqClient _rmqClient;
        private readonly ConfigurationManager _configManager;
        private readonly CancellationTokenSource _cts;
        private readonly CancellationToken _token;
        private RabbitMqConfiguration _config;
        private ProxyOptions _options;

        public ProxyProcessor(RabbitMqClient rmqClient, ConfigurationManager configManager)
        {
            _rmqClient = rmqClient;
            _configManager = configManager;
            _cts = new CancellationTokenSource();
            _token = _cts.Token;
        }

        public async Task<int> CreateProxy(ProxyOptions options)
        {
            _options = options;
            _config = _configManager.Get(options.ConfigName);
            if(!options.Headless) 
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
                    await StartWaitMessage();
                }

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
            _rmqClient.SetConfig(_config);

            try
            {
                var headers = context.Request.Headers;
                var routingKey = GetHeaderOrNull(RoutingKeyHeaderKey, headers);
                var exchange = GetHeaderOrNull(ExchangeHeaderKey, headers);
                var queue = GetHeaderOrNull(QueueHeaderKey, headers);
                ValidateExchangeAndQueue(exchange, queue);
                var headerBlacklist = GetHeaderBlacklist();
                headerBlacklist.AddRange(new [] { RoutingKeyHeaderKey, ExchangeHeaderKey, QueueHeaderKey});
                var parameters = GetParameters(headers, headerBlacklist.ToArray());

                var content = await GetRequestContent(context.Request);
                
                if(string.IsNullOrWhiteSpace(queue)) 
                { 
                    _rmqClient.PublishMessageToExchange(exchange, routingKey, Encoding.UTF8.GetBytes(content), parameters);
                }
                else
                {
                    _rmqClient.PublishMessageToQueue(queue, routingKey, Encoding.UTF8.GetBytes(content), parameters);
                }

                context.Response.StatusCode = 202;
                await context.Response.WriteAsJsonAsync(new {Message = "Sucessful published message"});
            }
            catch (Exception e)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { Error = e.Message }, _token);
            }
        }

        private IDictionary<string, string> GetParameters(
            IHeaderDictionary headers,
            string[] blacklist
        )
        {
            return headers
                .ToArray()
                .Where(kv => !blacklist.Contains(kv.Key, StringComparer.InvariantCultureIgnoreCase))
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
        }

        private List<string> GetHeaderBlacklist()
        {
            return _options.ExceptHeaders
                .Split(",")
                .Select(h => h.Trim())
                .ToList();
        }

        private void ValidateExchangeAndQueue(string exchange, string queue)
        {
            if (!string.IsNullOrWhiteSpace(exchange) && !string.IsNullOrWhiteSpace(queue))
            {
                throw new Exception(
                    "Ambiguous definition of queue and exchange. Provide only one of both."
                );
            }

            if (string.IsNullOrWhiteSpace(exchange) && string.IsNullOrWhiteSpace(queue))
            {
                throw new Exception("Neither queue nor exchange defined. One of both is required.");
            }
        }

        private string GetHeaderOrNull(string key, IHeaderDictionary headers)
        {
            return headers.ContainsKey(key)
                ? headers[key].ToString()
                : null;
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
                        .UseUrls($"http://*:{port}")
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
            Console.WriteLine("See also full documentation at: https://github.com/bkrieger1991/rabbitcli/tree/master#http-proxy-command-proxy", Color.ForestGreen);
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
            var cts = new CancellationTokenSource();
            var delay = TimeSpan.FromSeconds(5);
            var startDate = DateTime.Now;
            var informTask = Task.Run(
                async () =>
                {
                    while (startDate + delay > DateTime.Now && !cts.Token.IsCancellationRequested)
                    {
                        Console.Write($"Starting WebHost in: {((startDate + delay) - DateTime.Now).Seconds} seconds...\r");
                        await Task.Delay(200);
                    }
                }, cts.Token
            );
            var triggerTask = Task.Run(
                () =>
                {
                    Console.ReadKey();
                }, cts.Token
            );

            await Task.WhenAny(informTask, triggerTask);
            cts.Cancel();
        }

        private bool CheckIfPortIsAvailable(int port)
        {
            return IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpListeners()
                .All(c => c.Port != port);
        }

        private async Task<string> GetRequestContent(HttpRequest request)
        {
            using var sr = new StreamReader(request.Body);
            return await sr.ReadToEndAsync();
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