using System;
using System.Collections.Generic;
using System.Windows;
using Caliburn.Micro;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Library;
using RabbitMQ.Win.UI.ViewModels;
using Serilog;

namespace RabbitMQ.Win.UI
{
    public class Startup : BootstrapperBase
    {
        private readonly IServiceProvider _provider;

        public Startup()
        {
            _provider = ConfigureServices(new ServiceCollection()).BuildServiceProvider();

            Initialize();

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Verbose()
                .CreateLogger();
        }

        public IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(b => b
                .AddSerilog(dispose: true)
            );

            services.AddRabbitMqLibraryComponents();

            services.AddSingleton<IWindowManager, WindowManager>();
            services.AddSingleton<IEventAggregator, EventAggregator>();

            RegisterUiComponents(services);

            return services;
        }

        protected void RegisterUiComponents(IServiceCollection services)
        {
            services.AddTransient<RootViewModel>();
            services.AddTransient<NavigationViewModel>();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            DisplayRootViewFor<RootViewModel>();
        }

        protected override object GetInstance(Type service, string key)
        {
            return _provider.GetService(service);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            return _provider.GetServices(service);
        }

        protected override void BuildUp(object instance)
        {
            
        }
    }
}