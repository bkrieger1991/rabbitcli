using Microsoft.Extensions.Hosting;
using RabbitMQ.CLI.Proxy.Shared;

namespace RabbitMQ.CLI.Proxy;

public static class Program
{
    public static void Main(string[] args)
    {
        ProxyHostBuilder.CreateHostBuilder(5000, args).Build().Run();
    }
}