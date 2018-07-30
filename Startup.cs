using System.Linq;
using csmon.Models;
using csmon.Models.Db;
using csmon.Models.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace csmon
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        // ReSharper disable once NotAccessedField.Local
        private readonly ILogger _logger;

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            _logger = logger;
            Configuration = configuration;
            ConvUtils.AllowNegativeTime = bool.Parse(Configuration["AllowNegativeTime"]);
            foreach (var netSection in Configuration.GetSection("Networks").GetChildren())
                Network.Networks.Add(new Network()
                {
                    Id = netSection["Id"],
                    Title = netSection["Title"],
                    Ip = netSection["Ip"],
                    SignalIp = netSection["SignalIp"],
                    CachePools = bool.Parse(netSection["CachePools"])
                });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDataProtection(opts =>
            {
                opts.ApplicationDiscriminator = "csmon";
            });
            services.AddDbContext<CsmonDbContext>();
            AddHostedService<IIndexService, IndexService>(services);
            AddHostedService<INodesService, NodesService>(services);
            AddHostedService<IGraphService, GraphService>(services);
            services.AddMvc();
        }

        private static void AddHostedService<TService, TImplementation>(IServiceCollection services)
            where TService : class where TImplementation : class, TService
        {
            services.AddSingleton<TService, TImplementation>();
            services.AddSingleton(provider => provider.GetService<TService>() as IHostedService);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Monitor/Error");
            }
            
            app.UseStaticFiles();
            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{network}/{controller}/{action}/{id?}",
                    new { network = Network.Networks.First().Id,
                          controller = "monitor",
                          action = "index" });
            });              
        }
    }
}
