using System;
using System.Threading.Tasks;
using CacheGrainInter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleankka.Client;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using ServiceCode;
using ServiceInterface;

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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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
                .UseOrleankka()
                .Build();

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
