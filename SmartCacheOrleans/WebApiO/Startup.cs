using System;
using System.Threading.Tasks;
using CacheGrainInter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleankka.Client;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using ServiceCode;
using Serilog;
using Seq;
using ServiceInterface;
using Orleans.Hosting;
using Orleans.ApplicationParts;
using Orleans.Storage;
using Orleankka;
using Orleans.Providers.Streams.SimpleMessageStream;

namespace WebApiO
{
    public class Startup
    {
        const int initializeAttemptsBeforeFailing = 10;
        private static int attempt = 0;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration; 
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            IClientActorSystem orleansClient = StartClientWithRetries().GetAwaiter().GetResult();
            services.AddSingleton<IEmailCheck, EmailCheck>(serviceProvider =>
            {
                return new EmailCheck(orleansClient);
            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseMvc(routes =>
            {
                // Set routing to root
                routes.MapRoute("default", "");
            });

        }

        private static async Task<IClientActorSystem> StartClientWithRetries()
        {
            // Client config
            IClusterClient client;
            client = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "nejcSC";
                    options.ServiceId = "SmartCache";
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IDomain).Assembly).WithReferences())
                .AddSimpleMessageStreamProvider("SMSProvider")
                .UseOrleankka()
                .Build();

            var log = new LoggerConfiguration()
            .WriteTo.Seq("http://localhost:5341")
            .WriteTo.Console()
            .CreateLogger();

            await client.Connect(RetryFilter);
            return client.ActorSystem();
        }

        private static async Task<bool> RetryFilter(Exception exception)
        {
            attempt++;
            if (attempt > initializeAttemptsBeforeFailing)
            {
                return false;
            }
            await Task.Delay(TimeSpan.FromSeconds(2));
            return true;
        }
    }
}
