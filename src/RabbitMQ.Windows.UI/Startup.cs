using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Library;
using RabbitMQ.Windows.UI.Forms;
using Serilog;
using Serilog.Sinks.WinForms;

namespace RabbitMQ.Windows.UI
{
    public class Startup
    {
        public Startup()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteToGridView()
                .MinimumLevel.Verbose()
                .CreateLogger();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(b => b
                .AddSerilog(dispose: true)
            );

            services.AddRabbitMqLibraryComponents();

            services.AddTransient<MainForm>();
        }
    }
}