using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace csmon.Models.Services
{
    public class GetLastBlockMiddleware
    {
        private readonly RequestDelegate _next;

        public GetLastBlockMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {            
            var token = context.Request.Path.ToString();
            if (!context.Request.IsHttps && token.EndsWith("/getLastBlock", StringComparison.InvariantCultureIgnoreCase))
            {
                var net = Network.GetById(token.Split('/')[1]);
                if (net == null)
                {
                    context.Response.Redirect("/");
                    return;
                }
                using (var client = ApiFab.CreateReleaseApi(net.Ip))
                {
                    var pools = client.PoolListGet(0, 1);
                    context.Response.StatusCode = 200;
                    await context.Response.WriteAsync(pools.Pools[0].PoolNumber.ToString());
                }
            }
            else
            {
                await _next.Invoke(context);
            }
        }
    }
}
