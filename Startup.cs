using System;
using csmon.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Thrift.Protocol;
using Thrift.Transport;

namespace csmon
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            ConvUtils.AllowNegativeTime = bool.Parse(Configuration["AllowNegativeTime"]);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDataProtection(opts =>
            {
                opts.ApplicationDiscriminator = "csmon";
            });
            services.AddMvc();
            services.AddTransient(CreditsApi);
            services.AddSingleton<ITpsSource, TpsSource>();
        }

        // Credits API fab
        private API.ISync CreditsApi(IServiceProvider provider)
        {
            TTransport transport = new TSocket(Configuration["Thrift:Host"],
                int.Parse(Configuration["Thrift:Port"]),
                int.Parse(Configuration["Thrift:Timeout"]));
            TProtocol protocol = new TBinaryProtocol(transport);
            var client = new API.Client(protocol);
            try
            {
                transport.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return client;
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

            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=Monitor}/{action=Index}/{id?}");
            });

            app.UseStaticFiles();

            if(bool.Parse(Configuration["ShowTPS"]))
                app.ApplicationServices.GetService<ITpsSource>().Configure();
        }
    }
}
