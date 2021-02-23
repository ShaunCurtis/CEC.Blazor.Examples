using CEC.Blazor.Examples.Services;
using CEC.Blazor.Hydra.Extensions;
using CEC.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CEC.Blazor
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages();
            services.AddControllersWithViews();
            services.AddServerSideBlazor();
            services.AddServerSideHttpClient();
            services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
            // CEC - Services added here
            services.AddCECRouting();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/red"), app1 =>
            {
                app1.UseBlazorFrameworkFiles("/red");
                app1.UseRouting();
                app1.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToPage("/red/{*path:nonfile}", "/_Red");
                });

            });

            app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/purple"), app2 =>
            {
                app2.UseBlazorFrameworkFiles("/purple");
                app2.UseRouting();
                app2.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToPage("/purple/{*path:nonfile}", "/_Purple");
                });

            });


            app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/exampleswasm"), app3 =>
            {
                app3.UseBlazorFrameworkFiles("/exampleswasm");
                app3.UseRouting();
                app3.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToPage("/exampleswasm/{*path:nonfile}", "/_ExamplesWASM");
                });

            });

            app.UseRouting();

            app.UseBlazorFrameworkFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapRazorPages();
                endpoints.MapFallbackToPage("/examplesserver/{*path:nonfile}", "/_ExamplesServer");
                endpoints.MapFallbackToPage("/grey/{*path:nonfile}", "/_Grey");
                endpoints.MapFallbackToPage("/blue/{*path:nonfile}", "/_Blue");
                endpoints.MapFallbackToPage("/routing/{*path:nonfile}", "/_Routing");
                endpoints.MapFallbackToPage("/Index");
            });
        }
    }
}
