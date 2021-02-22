using CEC.Blazor.Examples.Services;
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
            // Server Side Blazor doesn't register HttpClient by default
            // Thanks to Robin Sue - Suchiman https://github.com/Suchiman/BlazorDualMode
            if (!services.Any(x => x.ServiceType == typeof(HttpClient)))
            {
                // Setup HttpClient for server side in a client side compatible fashion
                services.AddScoped<HttpClient>(s =>
                {
                    // Creating the URI helper needs to wait until the JS Runtime is initialized, so defer it.
                    var uriHelper = s.GetRequiredService<NavigationManager>();
                    return new HttpClient
                    {
                        BaseAddress = new Uri(uriHelper.BaseUri)
                    };
                });
            }
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
                app1.UseHttpsRedirection();
                app1.UseStaticFiles();

                app1.UseBlazorFrameworkFiles("/red");

                app1.UseRouting();

                app1.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToPage("/red/{*path:nonfile}", "/_Red");
                });

            });

            app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/purple"), app2 =>
            {
                app2.UseHttpsRedirection();
                app2.UseStaticFiles();

                app2.UseBlazorFrameworkFiles("/purple");

                app2.UseRouting();

                app2.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToPage("/purple/{*path:nonfile}", "/_Purple");
                });

            });


            app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/CEC_Blazor_Examples_Server"), app3 =>
            {
                app3.UseHttpsRedirection();
                app3.UseStaticFiles();

                app3.UseBlazorFrameworkFiles("/CEC_Blazor_Examples_WASM");

                app3.UseRouting();

                app3.UseEndpoints(endpoints =>
                {
                    endpoints.MapFallbackToPage("/CEC_Blazor_Examples_WASM/{*path:nonfile}", "/CEC_Blazor_Examples_WASM");
                });

            });

            //app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/grey"), appgrey =>
            //{
            //    appgrey.UseHttpsRedirection();
            //    appgrey.UseStaticFiles();

            //    appgrey.UseBlazorFrameworkFiles();

            //    appgrey.UseRouting();

            //    appgrey.UseEndpoints(endpoints =>
            //    {
            //        endpoints.MapBlazorHub();
            //        endpoints.MapFallbackToPage("/grey/{*path:nonfile}", "/_Grey");
            //    });

            //});

            app.UseRouting();

            app.UseBlazorFrameworkFiles();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapRazorPages();
                endpoints.MapFallbackToPage("/CEC_Blazor_Examples_Server/{*path:nonfile}", "/_CEC_Blazor_Examples_Server");
                endpoints.MapFallbackToPage("/grey/{*path:nonfile}", "/_Grey");
                endpoints.MapFallbackToPage("/blue/{*path:nonfile}", "/_Blue");
                //endpoints.MapFallbackToPage("/cec.routing/{*path:nonfile}", "/_CEC.Routing");
                endpoints.MapFallbackToPage("/Index");
            });
        }
    }
}
