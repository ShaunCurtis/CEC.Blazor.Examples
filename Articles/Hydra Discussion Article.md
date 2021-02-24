# Blazor Hydra - Hosting Multiple Blazor SPAs on a single Site

Building a green field site with a new framework is great, but we don't all get that luxury. What about those with a classic Razor/Server Page ASPNetCore site wanting to migrate where the Big Bang isn't feasible.  You want to run most of your site in classic mode and migrate sections over in bite sized chunks.  Maybe run a pilot on a small section first.

This article shows you how to do just that.  I'll describe how Blazor Server, WASM and Razor sites/applications interact, and how you can run them together on a single AspNetCore web site.

The first half of this article examines the technical challenges, looking in depth at how some key bits of Blazor work.  In the second half we'll look at a simple practical deployment using the out-of-the-box project templates.

## Code Repository

**Hydra** is an implementation of the concepts discussed here and the code will be used extensively in the discussions.

The code is in two repositories

 - [CEC.Blazor.Examples](https://github.com/ShaunCurtis/Examples) is the main repository.
 - [Hydra](https://github.com/ShaunCurtis/Hydra) contains the code for the second half of this article.

## Misconceptions

Before we start, we need to get one key concept straight.  Blazor Server and Blazor WASM are **APPLICATIONS**: they are not web sites.  Throughout this articles I'll refer to the as SPAs [Single Page Applications].  The Razor site is the only conventional **Web Site**. 

The only true web page in a Blazor SPA is the startup page.  Once started it's an application.  It doesn't do posts and gets for pages.

**Blazor Pages** aren't web pages, they're components.

## The Web Server

If you look at the code repository and the test site you'll see a project call *Hydra*.  This is the host web site.  It's an out-of-the-box AspNetCore Pages template web site.

Lets look at *Startup.cs*.

AspNetCore has an Inversion Of Control/Dependancy Injection *Services* container defined by `IServiceCollection`.  If you are unsure what an IOC/DI container is, then do some background reading.  We configure this in `ConfigureServices`.

```c#
    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRazorPages();
        services.AddControllersWithViews();
        services.AddServerSideBlazor();
        services.AddServerSideHttpClient();
        services.AddSingleton<IWeatherForecastService, WeatherForecastService>();
        services.AddCECRouting();
    }
```

What you see are a set of services configured directly - such as the `WeatherForecastService` and calls to a set of ServiceCollectionExtensions like `AddServerSideBlazor` and `AddCECRouting`.

To de-mystify these, the code for `AddCECRouting` is shown below.  

```c#
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCECRouting(this IServiceCollection services)
    {
        services.AddScoped<RouterSessionService>();
        return services;
    }
}
```

ServiceCollectionExtensions are just ways to configure all the services for a specific function under one roof.  They are Extension methods for `IServiceCollection`.  `AddServerSideBlazor` just adds all the necessary services for Blazor Server, such as `NavigationManager` and `IJSRuntime`.

`AddServerSideHttpClient` is defined in CEC.Blazor.Hydra/Extensions if you want to see a ServiceCollectionExtension implementation.

`Configure` sets up the *Middleware* run by the web server.  We'll break this down into sections.

The first section is ASPNetCore web server standard implementation.

```c#
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

```

Next we define a `IApplicationBuilder.MapWhen` for each WASM SPA.  I've shown the one for *red* here.  These define the middleware to run for specific site segments - defined by the Url.  This is for the Red WASM.  Note:
1. We configure `UseBlazorFrameworkFiles` to a specific Url segment- we'll look at the significance of this in the WASM section below. It means the frameworks file path will be *wwwroot/red/_framework/*, providing a unique path for this WASM SPA.
2. The fallback Page for the segment is `/_Red.cshtml`.     

```c#

    app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/red"), app1 =>
    {
        app1.UseBlazorFrameworkFiles("/red");
        app1.UseRouting();
        app1.UseEndpoints(endpoints =>
        {
            endpoints.MapFallbackToPage("/red/{*path:nonfile}", "/_Red");
        });
    });

```

Finally we define the default segment middleware.  Note:
1. We define `MapBlazorHub` to map all SignalR traffic to the Blazor Hub.
2. We define a set of segment specific fallback pages for each Blazor Server SPA with the specific startup page for the SPA.

```c#
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
```


### Blazor WASM

Each WASM SPA must have a separate project. `<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
` declares the project as a WASM SPA and instructs the compiler to build the project as a WASM SPA.  There's a single `RootComponent` defined in `Program`.

```c#
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<WASMRedApp>("#app");

        builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        await builder.Build().RunAsync();
    }
}
```

You don't need to stick with `app`.  This declaration defines a different class to `App`.  It basically says render the component of `TComponent` in the HTML tag with `id` of `app`.  Don't be misled by `RootComponents` and `Add`.  Declare one.  You can declare more than one, but they must all be present on the rendered page, so it's fairly pointless: Layouts achieve the same result with far more flexibility.

The key to multi SPA hosting is specifying a unique `StaticWebAssetBasePath` in the project definition file. 

```xml
  <PropertyGroup>
    <StaticWebAssetBasePath>red</StaticWebAssetBasePath>
  </PropertyGroup>
```

Remember we used `UseBlazorFrameworkFiles("/red")` in the middleware map in `Startup,cs`.  This is where things tie together.  This is case sensitive!

![Mongrel Red Project](../images/Mongrel-Red-Project.png)

The project view above shows all the files in the project.

1. The Layout and navigation are common to all the Mongrel projects so have moved up to the shared library.
2. `App.razor` has moved into *Components* and been renamed to make it unique.  I don't like `Apps` all over the place.
3. *wwwroot* has gone as the startup `index.html` has moved up to Hydra, as has the CSS.

Once compiled the *bin* looks like this:

![Mongrel Red Project](../images/Mongrel-Red-bin.png)

Note the *_framework* directory containing `blazor.webaseembly.js` and `blazor.boot.json`.  When this project is referenced by Hydra this directory will be available as *red/_framework*.  Without unique `StaticWebAssetBasePath`s all SPA's would be mappsed to *_framework*.

The "Startup" page for the SPA is on Hydra. *_Red.cshtml*

```html
@page "/spared"
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{
    Layout = null;
}

<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Mongrel.WASM.RED</title>
    <base href="/red/" />
    <link href="/css/site.css" rel="stylesheet" />
    <link href="/CEC.Blazor.Hydra.styles.css" rel="stylesheet" />
</head>
<body>
    <div id="app">
        <div class="mt-4" style="margin-right:auto; margin-left:auto; width:100%;">
            <div class="loader"></div>
            <div style="width:100%; text-align:center;"><h4>Web Application Loading</h4></div>
        </div>
    </div>

    <script src="/red/_framework/blazor.webassembly.js"></script>
</body>
</html>
```

Note:
1. The `base` is set to `"/red/"`.  **Important** - You need to leading and trailing slashes.
2. `site.css` is a SASS built CSS file.
3. `CEC.Blazor.Hydra.styles.css` is the system SASS built CSS incorporating all the reference projects component styles.
4. We define the specific webassembly script `src="/red/_framework/blazor.webassembly.js"`.  It looks for it's configuration file in *\{base\}/_framework/blazor.boot.json*, where *\{base\}* is defined in `<base href="">`. *./blazor.boot.json* is the configuration file for loading the WASM executables.  It's important to realise that *base* needs to have a trailing "/" for this to work.

The final bit of the jigsaw is the mapping in Hydra's `Startup.cs`.  The middleware is configured with the correct *BlazorFrameworkFiles* and any get requests to */red/* are directed to */_Red.cshtml*.

Once the SPA starts, any navigation to known Urls within the application are routed by the SPA.  Any external Urls are Http gets.  In Hydra and Urls to */red/** are directed to */_Red.cshtml*.  So */red/counter* will start the SPA on the counter page, while  */red/nopage* will load the SPA but you will see the "Nothing at this Address" message.

You need to be a little careful with the route "/".  I prefer to set the SPA home page up with a route "/index" and always reference it that way.

### Blazor Server

A Blazor Server SPA is configured a little differently.  We don't need a `Program.cs` with a single entry point.  We configure the startup page to load any class implementing `IComponent`, so our entry point can come from anywhere.  For code and route management define a Razor Library project per SPA: keep everything compartmentalized.

We've aleady seen the configuration of the `Startup.cs` in the first section, so we don't need to cover it here.  There's one Blazor hub for all the SPAs, so the services required by all the SPAs are defined together.  it's easy to overexcited about this: won't it be huge?  That all depends on what your services are and their scope.  They're only loaded as needed.

You need to configure an *endpoint* for each Server SPA.  You've seen these already in `Startup.cs`.

The client side of the SPA is again defined as a single page.  Unlike the WASM page, which is just static Html, the Server page is a razor page.  You can see the `Component` entry point for the application.  What Razor does with the page depends on the `rendermode`. 

```html
@page "/spagrey"
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{
    Layout = null;
}
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Mongrel.Server.GREY</title>
    <base href="/grey" />
    <link href="/css/site.css" rel="stylesheet" />
    <link href="/CEC.Blazor.Hydra.styles.css" rel="stylesheet" />
</head>
<body>
    <component type="typeof(Mongrel.Server.Grey.Components.ServerGreyApp)" render-mode="ServerPrerendered" />

    <script src="_framework/blazor.server.js"></script>
</body>
</html>
```

Once the browser receives whatever got produced by the server, it runs the initial render and then calls the loaded Javascript files.  This where the magic starts.  `blazor.server.js` loads, reads configuration data incorporated in the page and makes a call back to the BlazorHub over SignalR.  The Blazor Hub renders the component tree and passes the changes back over SignalR to the client.  Events happen on the page, get passed back to the Hub Session which hadles the events and passes any DOM changes back to the client.  While any navigation events can be routed by the SPA router, the SPA session persists, but as soon as the URL is outside the router's scope it instructs the browser to submit a full http get for the URL.  The SPA session ends.

So what's going on in the SignalR session.  The AspNetCore compiled code for the website contains all the Blazor component code - they are just standard classes.  The Service container runs the services, again standard classes within the compiled code.  The heart of the signalR session is the specific Renderer for that session.  It builds and rebuilds the DOM from the ComponentTree.  You need to think of the Server SPA as two entities, one running in the Blazor Hub on the Server doing most of the work.  The other bit on the client, intercepting events and passing them back to the server, and re-rendering the page with the DOM changes that are returned. Events one way, DOM changes the other.

## Hydra Build Article

[Hydra Build Article](https://github.com/ShaunCurtis/CEC.Blazor.Examples/blob/master/Articles/Hydra%20Build%20Article.md)

## Wrap Up

There's a lot to take in in this article.  It took me a week, on and off, to pull it all together once I'd proved the concept.  There are some important key concepts to understand:

1. To write Blazor SPA's you need to get out of the "Web" paradigm.  Think old-school desktop application.  A bit retro!  If you don't, it'll be a bit of a dogs breakfast.
2. A WASM SPA is a compiled executable - just like a desktop application.  The startup page is just like a Shortcut.
3. A Server SPA is a pointer to a class in the code running on the web site.  The startup page is a server-side shortcut to get it up and running.  Once started, the SPA is a two part affair: one half is the browser SPA, the other a session on the Blazor Hub on the server, inexorably joined by a SignalR session.
4. You need to be very careful in your Url referencing: a missing "/" can blow you out of the water!
