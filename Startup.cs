using System.Linq;
using csmon.Models;
using csmon.Models.Db;
using csmon.Models.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace csmon
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Settings.Parse(Configuration);

            foreach (var netSection in Configuration.GetSection("Networks").GetChildren())
                Network.Networks.Add(new Network()
                {
                    Id = netSection["Id"],
                    Title = netSection["Title"],
                    Api = $"/{netSection["Id"]}/{netSection["API"]}",
                    Ip = netSection["Ip"],
                    SignalIp = netSection["SignalIp"],
                    CachePools = bool.Parse(netSection["CachePools"]),
                    RandomNodes = bool.Parse(netSection["RandomNodes"])
                });
        }

        private static void AddHostedService<TService, TImplementation>(IServiceCollection services)
            where TService : class where TImplementation : class, TService
        {
            services.AddSingleton<TService, TImplementation>();
            services.AddSingleton(provider => provider.GetService<TService>() as IHostedService);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddDbContext<CsmonDbContext>();
            AddHostedService<IIndexService, IndexService>(services);
            AddHostedService<INodesService, NodesService>(services);
            AddHostedService<IGraphService, GraphService>(services);

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
                options.HttpsPort = 443;
            });
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
                app.UseExceptionHandler("/Monitor/Error");
                //app.UseHsts();
                app.UseHttpsRedirection();
            }
            
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{network}/{controller}/{action}/{id?}",
                    new
                    {
                        network = Network.Networks.First().Id,
                        controller = "monitor",
                        action = "index"
                    });
            });
        }
    }
}
