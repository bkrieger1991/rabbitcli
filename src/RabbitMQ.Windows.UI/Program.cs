using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Windows.UI.Forms;

namespace RabbitMQ.Windows.UI
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var startup = new Startup();
            var services = new ServiceCollection();
            startup.ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(provider.GetRequiredService<MainForm>());
        }
    }
}
