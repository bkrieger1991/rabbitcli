using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using RabbitMQ.Library;

namespace RabbitMQ.CLI.Proxy
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private readonly IConfiguration _configuration;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var config = _configuration.GetSection("RabbitMQ").Get<RabbitMqConfiguration>();
            services.AddSingleton(config);
            services.AddTransient<RabbitMqClient>();
            services.AddAutoMapper(c => c.AddMaps(typeof(RabbitMqClient).Assembly));
            services.AddLogging(c => c
                .ClearProviders()
                .AddConsole()
                .AddConfiguration(_configuration.GetSection("Logging"))
            );
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "RabbitMQ.CLI.Proxy", Version = "v1" });
            });
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var logger = app.ApplicationServices.GetRequiredService<ILogger<Startup>>();
            logger.LogInformation("Swagger endpoint and ui enabled.");
            app.UseDeveloperExceptionPage()
                .UseSwagger()
                .UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RabbitMQ.CLI.Proxy v1"));
            
            app.UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
        }
    }
}
