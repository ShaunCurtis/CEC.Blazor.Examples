# Blazor Hydra - Hosting Multiple Blazor SPAs on a single Site

Building a green field site with a new framework is great, but we don't all get that luxury. What about those with a classic Razor/Server Page ASPNetCore site wanting to migrate where the Big Bang isn't feasible.  You want to run most of your site in classic mode and migrate sections over in bite sized chunks.  Maybe run a pilot on a small section first.

This article shows you how to do just that.  I'll describe how Blazor Server, WASM and Razor sites/applications interact, and how you can run them together on a single AspNetCore web site.

The first half of this article examines the technical challenges, looking in depth at how some key bits of Blazor work.  In the second half we'll look at a simple practical deployment using the out-of-the-box project templates.

![Hydra](https://github.com/ShaunCurtis/CEC.Blazor.Examples/blob/master/Images/Hydra.png?raw=true)

## Code Repository

**Hydra** is an implementation of the concepts discussed here and the code will be used extensively in the discussions.

The code is in two repositories

 - [CEC.Blazor.Examples](https://github.com/ShaunCurtis/Examples) is the main repository.  It contains the code for several articles including this one and is the source code repo for the [Demo Site on Azure](https://cec-blazor-examples.azurewebsites.net/).  It uses the methods discussed in this article to host multiple WASM and Server SPAs on one web site.  Mongrel is the specific site for this article.
 - [Hydra](https://github.com/ShaunCurtis/Hydra) contains the code for the second half of this article.  A version of Hydra runs on the Demo Site.

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

We've already seen the configuration of the main site's `Startup.cs`, I included the Blazor bit in the discussion above.  It:
1. Adds the specific Blazor Services by calling `services.AddServerSideBlazor()` in `ConfigureServices`.
2. Adds the Blazor Hub Middleware by calling `endpoints.MapBlazorHub()` in `Configure`.

The key bot to understand here is there's one Blazor hub for all the SPAs.  Load all the services for each SPA in `ConfigureServices`.  This does place some restrictions and controls on what you can and can't run together, but for most instances this won't be an issue.  Remember Services are only loaded as needed, and cleaned up as they are disposed.  Make sure you get the scope of your service right.

Each Server SPA has an *endpoint*.  You don't need to configure these with `MapWhen` as they all use the same middleware train.Thet're all defined in the default `UseEndpoints`.  You've seen these already in `Startup.cs`.

The client side of the Server SPA is again defined as a single page.  Unlike the WASM page, which is static Html wrapped in a Razor page, the Server SPA page is a razor page.  It has a `Component` entry point that is read by the TagHelper and processed by the server.  What Razor does with the page is dictated by `rendermode`. 

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

Once the browser receives what got produced by the server, it renders the initial static DOM provided and then calls the loaded Javascript.  This where the magic starts.  `blazor.server.js` loads, reads configuration data incorporated in the page and establishes a session with the BlazorHub over SignalR and requests the root `IComponent` defined on the page by `<component>` - normally `App` defined by `App.Razor`.  The Blazor middleware, configured on the server by `endpoints.MapBlazorHub()` receives the request and establishes the session.  The Blazor Hub renders the requested `IComponent` component tree and passes the changes back over SignalR to the client: on initial re-render this is the whole DOM.  The SPA is now live.  Events happen on the page, get passed back to the Hub Session.  It handles the events and passes any DOM changes back to the client.  Navigation events to known routes get handled by the SPA router: the SPA session persists.  A URL outside known routes causes the SPA Router to submit a full http get for the URL: the SPA session ends.  I watch the page tab in the browser to see if a request is routed or causes a page refresh.

So what's going on in the SignalR session.  The AspNetCore compiled code library for the website contains all the Blazor component code - they're just standard classes.  The Service container runs the services, again standard classes within the compiled code library.  The heart of the signalR session is the Renderer for the specific SPA session.  It builds and rebuilds the DOM from the ComponentTree.  Consider a Blazor Server SPA as two entities: one running in the Blazor Hub on the Server doing most of the work; the other bit on the client, intercepting events and passing them back to the server, then re-rendering the page with the DOM changes returned. Events client-to-Server, DOM changes Server-to-Client.

## Building the Hydra Site

I've tried to keep this as simple as possible, basing the sites on the out-of-the-box templates.  I've made no attempt to consolidate code into shared libraries.

Build the starting solution as follows (all are built from the standard VS 2019 templates):
1. **Hydra.Web** - A Razor MVC project.  This is the start up project.
3. **Hydra.Grey** - A Razor library project 
4. **Hydra.Blue** - Another Razor library project
5. **Hydra.Steel** - A Blazor WASM project (standalone - not NetCore hosted)
6. **Hydra.Red** - A Blazor WASM project (standalone - not NetCore hosted)

![Initial Project View](https://raw.githubusercontent.com/ShaunCurtis/CEC.Blazor.Examples/master/Images/Hydra-Solution.png)

Set *Hydra.Web* as the startup project.  If you're working along with me run the project to check the Razor project compiles and runs.

#### Clear Out the Two Libraries

Clear the following files from the two libraries

- *wwwroot* and contents
- *Component1.razor*
- *ExampleJsInterop.cs*

Start with the WASM Projects

### Hydra.Red

![Initial Project View](https://raw.githubusercontent.com/ShaunCurtis/CEC.Blazor.Examples/master/Images/Hydra-Red.png)

Update the project file.  Note `StaticWebAssetBasePath` is set to *red*.

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
    <StaticWebAssetBasePath>red</StaticWebAssetBasePath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="5.0.3" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="5.0.3" PrivateAssets="all" />
    <PackageReference Include="System.Net.Http.Json" Version="5.0.0" />
  </ItemGroup>

</Project>
```

Update *NavMenu.razor* by adding the following link at the top of the links.

```html
<div class="@NavMenuCssClass" @onclick="ToggleNavMenu">
    <ul class="nav flex-column">
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/Hydra" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Hydra
            </NavLink>
        </li>
......
```

Update *MainLayout.razor.css* by changing the `.sidebar` background.

```css
.sidebar {
    background-image: linear-gradient(180deg, #400 0%, #800 70%);
}
```

### Hydra.Steel

![Initial Project View](https://raw.githubusercontent.com/ShaunCurtis/CEC.Blazor.Examples/master/Images/Hydra-Steel.png)

Update the project file.  Note the added `StaticWebAssetBasePath` set to *steel*.

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
    <StaticWebAssetBasePath>steel</StaticWebAssetBasePath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="5.0.3" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="5.0.3" PrivateAssets="all" />
    <PackageReference Include="System.Net.Http.Json" Version="5.0.0" />
  </ItemGroup>

</Project>
```

Update `NavMenu.razor` by adding the following link at the top of the links.

```html
<div class="@NavMenuCssClass" @onclick="ToggleNavMenu">
    <ul class="nav flex-column">
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/Hydra" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Hydra
            </NavLink>
        </li>
......
```
Update the *sidebar* in `MainLayout`.
```html
    <div class="sidebar sidebar-steel">
        <NavMenu />
    </div>
```

```html
// Hydra.Steel/Pages/FetchData.razor
....

    protected override async Task OnInitializedAsync()
    {
// Note /sample-data
        forecasts = await Http.GetFromJsonAsync<WeatherForecast[]>("/sample-data/weather.json");
    }
....
```

We will use CSS from *Hydra.Web* for this SPA.

Delete:
1. The *wwwroot* folder structure
2. */Shared/MainLayout.razor.css*
3. */Shared/NavMenu.razor.css*

### Hydra.Web

![Initial Project View](https://raw.githubusercontent.com/ShaunCurtis/CEC.Blazor.Examples/master/Images/Hydra-Web.png)

Update the project file.  We add in all projects and a package reference to the Blazor WASM Server library.

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="5.0.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Hydra.Blue\Hydra.Blue.csproj" />
    <ProjectReference Include="..\Hydra.Red\Hydra.Red.csproj" />
    <ProjectReference Include="..\Hydra.Steel\Hydra.Steel.csproj" />
    <ProjectReference Include="..\Razor.Grey\Hydra.Grey.csproj" />
  </ItemGroup>

</Project>
```

Pull the following folders with files from the Repo:
1. *wwwroot/css*
2. *wwwroot/sample-data*

*wwwroot/css/app.css* is a custom built BootStrap distribution.

Add two Razor Pages to the Pages folder:

1. *_Blue.cshtml* 
2. *_Red.cshtml* 
3. *_Steel.cshtml* 
4. *_Grey.cshtml* 

Delete the associated model cs files assocated with these.

```html
// Hydra.Web/Pages/_Red.cshtml
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
    <title>Hydra.RED</title>
    <base href="/red/" />
    <link href="/red/css/bootstrap/bootstrap.min.css" rel="stylesheet" />
    <link href="/red/css/app.css" rel="stylesheet" />
    <link href="/red/Hydra.Red.styles.css" rel="stylesheet" />
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

```html
// Hydra.Web/Pages/_Steel.cshtml
@page "/spasteel"
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{
    Layout = null;
}

<!DOCTYPE html>
<html>

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Hydra.Steel</title>
    <base href="/steel/" />
    <link href="/css/app.css" rel="stylesheet" />
    <link href="Hydra.Web.styles.css" rel="stylesheet" />
</head>

<body>
    <div id="app">
        <div class="mt-4" style="margin-right:auto; margin-left:auto; width:100%;">
            <div class="loader"></div>
            <div style="width:100%; text-align:center;"><h4>Web Application Loading</h4></div>
        </div>
    </div>

    <script src="/steel/_framework/blazor.webassembly.js"></script>
</body>

</html>
```

```html
// Hydra.Web/Pages/_Grey.cshtml & _Blue.cshtml_
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{
    Layout = null;
}
<!DOCTYPE html>
<html>
<head>
</head>
<body>
Holding page
</body>
</html>
```

#### Startup.cs

We're:
1. Adding in Support for Razor Pages to the Services.
2. Add a specific `Endpoint` configuration for the Red and Steel SPAs.
   i. Mounts the *_framework* files associated with the SPA `<StaticWebAssetBasePath>`.
   ii. Sets a Fallback to *_Colour.cshtml* for all /purple/* URLs
3. Add Blazor Support at the same time.
   i.  Add the Blazor Services.
   ii. Add the Blazor Hub.
   iii. Add the Endpoints for the Blazor Server SPAs.

```c#
// Hydra.Web/StartUp.cs
public void ConfigureServices(IServiceCollection services)
{
    services.AddRazorPages();
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
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    ....
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

    app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/steel"), app1 =>
    {
        app1.UseBlazorFrameworkFiles("/steel");
        app1.UseRouting();
        app1.UseEndpoints(endpoints =>
        {
            endpoints.MapFallbackToPage("/steel/{*path:nonfile}", "/_Steel");
        });

    });

    app.UseRouting();

    // default EndPoint Configuration
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapBlazorHub();
        endpoints.MapRazorPages();
        endpoints.MapFallbackToPage("/grey/{*path:nonfile}", "/_Grey");
        endpoints.MapFallbackToPage("/blue/{*path:nonfile}", "/_Blue");
        endpoints.MapFallbackToPage("/Index");
    });
}
```
#### Index.cshtml

Update the `@page` directive and add some buttons for navigation.

```html
@page "/"
....
<div class="text-center">
    <h1 class="display-4">Welcome</h1>
    <p>Learn about <a href="https://docs.microsoft.com/aspnet/core">building Web apps with ASP.NET Core</a>.</p>
    <div class="container">
        <a class="btn btn-danger" href="/red">Hydra Red</a>
        <a class="btn btn-dark" href="/steel">Hydra Steel</a>
        <a class="btn btn-primary" href="/blue">Hydra Blue</a>
        <a class="btn btn-secondary" href="/grey">Hydra Grey</a>
    </div>
</div>
```

This should all now compile and run.

## Hydra.Blue

![Initial Project View](https://raw.githubusercontent.com/ShaunCurtis/CEC.Blazor.Examples/master/Images/Hydra-Blue.png)

This is the library were we'll use to build one of the Blazor Server SPAs.

#### Project File

Include the following Packages and projects.

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="5.0.3" />
  </ItemGroup>

</Project>
```

#### File Moves and Renames

Copy the *Pages* and *Shared* folders from *Hydra.Red*.
1. *Pages* folder.
2. *Shared* folder.
4. *App.razor*.
3. *_Imports.razor*.

#### _Imports.razor

```html
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using Hydra.Blue
@using Hydra.Blue.Shared
```

```html
// Hydra.Blue/Pages/Index.razor
@page "/"
@page "/blue"
@page "/blue/index"
....
```

```html
// Hydra.Blue/Pages/Counter.razor
@page "/counter"
@page "/blue/counter"
....
```

```html
// Hydra.Blue/Pages/FetchData.razor
@page "/fetchdata"
@page "/blue/fetchdata"
....

    protected override async Task OnInitializedAsync()
    {
// Note /sample-data
        forecasts = await Http.GetFromJsonAsync<WeatherForecast[]>("/sample-data/weather.json");
    }
....
```

Update the *sidebar* in `MainLayout`.

```html
    <div class="sidebar sidebar-blue">
        <NavMenu />
    </div>
```

Update `NavMenu.razor`

```html
<div class="@NavMenuCssClass" @onclick="ToggleNavMenu">
    <ul class="nav flex-column">
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/Hydra" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Hydra
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="blue/index" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Home
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="blue/counter">
                <span class="oi oi-plus" aria-hidden="true"></span> Counter
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="blue/fetchdata">
                <span class="oi oi-list-rich" aria-hidden="true"></span> Fetch data
            </NavLink>
        </li>
    </ul>
</div>
```


Update *App.razor*, changing `AppAssembly`, which is set to *Hydra.Blue.App*, so it finds the routes in this assembly.  

```html
// Hydra.Blue/App.razor
<Router AppAssembly="@typeof(Hydra.Blue.App).Assembly" PreferExactMatches="@true">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(MainLayout)">
            <p>Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>
```

## Hydra.Grey

![Initial Project View](https://raw.githubusercontent.com/ShaunCurtis/CEC.Blazor.Examples/master/Images/Hydra-Grey.png)


#### Project File

Include the following Packages and projects.

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="5.0.3" />
  </ItemGroup>

</Project>
```

#### File Moves and Renames

Copy the *Pages* and *Shared* folders from *Hydra.Blue*.
1. *Pages* folder.
2. *Shared* folder.
4. *App.razor*.
3. *_Imports.razor*.

#### _Imports.razor

```html
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using Hydra.Grey
@using Hydra.Grey.Shared
```

```html
// Hydra.Grey/Pages/Index.razor
@page "/"
@page "/Grey"
@page "/Grey/index"
....
```

```html
// Hydra.Grey/Pages/Counter.razor
@page "/Grey/counter"
....
```

```html
// Hydra.Grey/Pages/FetchData.razor
@page "/fetchdata"
@page "/Grey/fetchdata"
....

    protected override async Task OnInitializedAsync()
    {
// Note /sample-data
        forecasts = await Http.GetFromJsonAsync<WeatherForecast[]>("/sample-data/weather.json");
    }
....
```

```
Update the *sidebar* in `MainLayout`.
```html
    <div class="sidebar sidebar-grey">
        <NavMenu />
    </div>
```

Update `NavMenu.razor`

```html
<div class="top-row pl-4 navbar navbar-dark">
    <a class="navbar-brand" href="">Hydra.Grey</a>
    <button class="navbar-toggler" @onclick="ToggleNavMenu">
        <span class="navbar-toggler-icon"></span>
    </button>
</div>

<div class="@NavMenuCssClass" @onclick="ToggleNavMenu">
    <ul class="nav flex-column">
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/Hydra" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Hydra
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="grey/index" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Home
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="grey/counter">
                <span class="oi oi-plus" aria-hidden="true"></span> Counter
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="grey/fetchdata">
                <span class="oi oi-list-rich" aria-hidden="true"></span> Fetch data
            </NavLink>
        </li>
    </ul>
</div>
```

Update *App.razor*, changing `AppAssembly`, which is set to *Hydra.Grey.App*, so it finds the routes in this assembly.  

```html
// Hydra.Grey/App.razor
<Router AppAssembly="@typeof(Hydra.Grey.App).Assembly" PreferExactMatches="@true">
    <Found Context="routeData">
        <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
    </Found>
    <NotFound>
        <LayoutView Layout="@typeof(MainLayout)">
            <p>Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>
```

### Hydra.Web

```html
// Hydra.Web/Pages/_Blue.html
@page "/spablue"
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{
    Layout = null;
}

<!DOCTYPE html>
<html>

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Hydra.Blue</title>
    <base href="/blue" />
    <link href="/css/app.css" rel="stylesheet" />
    <link href="/CEC.Blazor.Hydra.styles.css" rel="stylesheet" />
</head>

<body>
    <component type="typeof(Hydra.Blue.App)" render-mode="ServerPrerendered" />

    <script src="_framework/blazor.server.js"></script>
</body>

</html>
```

```html
// Hydra.Web/Pages/_Grey.html
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
    <title>Hydra.Grey</title>
    <base href="/grey" />
    <link href="/css/app.css" rel="stylesheet" />
    <link href="/CEC.Blazor.Hydra.styles.css" rel="stylesheet" />
</head>

<body>
    <component type="typeof(Hydra.Grey.App)" render-mode="ServerPrerendered" />

    <script src="_framework/blazor.server.js"></script>
</body>

</html>
```

This should all now compile and run.


## Wrap Up

There's a lot to take in in this article.  It took me a while to pull this article together once I'd proven the concepts.  Som important concepts to understand:

1. To write Blazor SPA's you need to get out of the "Web" paradigm.  Think old-school desktop application.  A bit retro!  If you don't, you SPA could be a bit of a dogs breakfast.  Version x will bear little resemblance to version 1.
2. A WASM SPA is a compiled executable - just like a desktop application.  The startup page is a shortcut.
3. A Server SPA is a pointer to a class in the library running on the web site.  The startup page is a server-side shortcut to get it up and running.  Once started, the SPA is a two part affair: one half is the browser SPA, the other a session on the Blazor Hub on the server, inexorably joined by a SignalR session.
4. You need to be very careful in your Url referencing: a missing "/" or a capital letter can blow you out of the water!  Stick to the rule that all urls and things that define bits of Urls only used small letters.  It's pretty easy to start digging holes  I've been there!
