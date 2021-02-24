# The Blazor Mongrel

Building a green field site with a new framework is great, but we don't all get that luxury. What about those with a classic Razor/Server Page ASPNetCore site that they would like to migrate.  The Big Bang is not often not feasible.  You want to run most of your site in classic mode and migrate sections over in bite sized chunks.  Maybe run a pilot on a small section first.

This article shows you how to do just that.  I'll describe how Blazor Server, WASM and Razor sites/applications interact, and how you can run them together on a single AspNetCore web site.

The first half of this article  will examine the technical challenges, looking in depth at how some key bits of Blazor work.  In the second half we'll look at a practical deployment.

**Hydra** is an implmentation of the concepts discussed here and will be used extensively in the discussions.

Before we start, we need to get one key concept straight.  Blazor Server and Blazor WASM are **APPLICATIONS**, not web sites.  So throughout this articles I'll refer to the as SPAs [Single Page Applications].  The MVC Blazor site is the only WEBSITE. 

## Code Repository

The code is in a Github Repository [here](https://github.com/ShaunCurtis/Examples).  This is a combined repository with the code from an earlier articles on the same subject.  The code specific to this article is in the *Mongrel* projects.

## Misconceptions

There are a couple of misconceptions you nned to understand before delving deeper.

**Blazor is a Web Site**.  It's not.  The startup page and some resources it uses may reside on a web site, but it's an application.  It doesn't do posts and gets for pages.

**Blazor Pages** aren't web pages, they're components.

## The Web Server

If you look at the code repository and the test site you'll see a project call *Hydra*.  This is the host web site.  It's a out-of-the-box AspNetCore Pages template web site.

Lets look at *Startup.cs*.

AspNetCore defines an Inversion Of Control/Dependancy Injection *Services* container defined as a `IServiceCollection`.  If you are unsure what an IOC/DI container is, then do some background reading.  We configure this in `ConfigureServices`.

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

To de-mystify these `AddCECRouting` is shown below.  ServiceCollectionExtensions are just ways to configure all the services for a specific function under one roof.  They are Extension methods for `IServiceCollection`.  `AddServerSideBlazor` just adds all the necessary services for Blazor Server, such as `NavigationManager` and `IJSRuntime`.

`AddServerSideHttpClient` is defined in CEC.Blazor.Hydra/Extensions if you want to see a ServiceCollectionExtension implementation.

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

`Config` sets up the *Middleware* that gets run by the web server.  We'll break this down into sections

The first section is bulk standard implementation for a ASPNetCore web server.

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

We define a `MapWhen` for each WASM SPA.  I've only shown one here.  These define specific middleware sets to run for specific site segments - defined by the Url.  This is for the Red WASM in Mongrel.  Note:
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

Each WASM SPA needs a separate project. `<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
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

You don't need to stick to `app`.  This declaration defines a different class to `App`.  It basically says render the component of `TComponent` in the HTML tag with `id` of `app`.  Don't be misled by `RootComponents` and `Add`.  Declare one.  You can declare more than one, but they must all be present on the rendered page, so it's fairly pointless: Layouts achieve the same result with far more flexibility.

The key section to multi SPA hosting is specifying a unique `StaticWebAssetBasePath` in the project definition file. 

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
3. *wwwroot* has gone as the startup `index.html` has moved up to Hydra as has our CSS.

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
4. We define the specific webassembly script `src="/red/_framework/blazor.webassembly.js"`.  This is important as it looks for it's configuration as *./blazor.boot.json*, i.e. in the same directory.  *./blazor.boot.json* is the configurstion file for loaidng the WASM executables.

The final bit of the jigsaw is the mapping on Hydra's `Startup.cs`.  The middleware is configured with the correct *BlazorFrameworkFiles* and any get requests to */red/* are directed to */_Red.cshtml*.

Once the SPA starts any navigation to known Urls withinin the application are routed by the SPA.  Any external Urls are issued as http gets.  In Hydra and Urls to */red/** are directed to */_Red.cshtml*.  So */red/counter* will start the SPA on the counter page, while  */red/nopage* will load the SPA but you will see the "Nothing at this Address" message.

You need to be a little careful with the route "/".  I prefer to set the SPA home page up with a route "/index" and always reference it that way in links.

### Blazor Server

A Blazor Server SPA is configured a little differently.  We don't need to build a `Program.cs` with a single entry point.  We can configure the startup page to load any class implementing `IComponent`, so our entry point can come from anywhere.  For code and route management I define a Razor Library project per SPA.

We've aleady seen the configuration of the `Startup.cs` in the first section, so we don't need to cover it here.  The key point to understadn is that there's one Blazor hub for all the SPAs, so the services required by all the SPAs are defined together.  Ut's easy to overexited about this.  Won't it be huge.  That all depends on what your services are and their scope.  They will only be loaded as needed.

You need to configure an *endpoint* for each Server SPA.  You've seen these already.

The client side of the SPA is again defined as a single page, but unlike the WASM page which is just static Html, the Server page is a razor page.  You can see the `Component` entrypoint for the application.  What Razor does with the page depends on the `rendermode`. 

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




=====================================================================



## Building A Hydra Web Site  The Base Project

I've built the starting solution as follows (all are built from the standard VS 2019 templates):
1. **Mongrel.Web** - A Razor MVC project.  This is the start up project.
2. **Mongrel.Shared** - A Razor library for shared code.
3. **Mongel.Server.Grey** - A Razor library project - **note not a Blazor Server project**!
4. **Mongrel.Server.Blue** - Another Razor library project that will build a second Blazor Server SPA.
5. **Mongrel.WASM.Purple** - A Blazor WASM project (standalone - not NetCore hosted)
6. **Mongrel.WASM.Red** - A Blazor WASM project (standalone - not NetCore hosted)

[Initial Project View](images/mongrel-starting-solution.png)

Set *Mongel.Web* as the startup project.  If you're working along with me run the project to check the Razor project compiles and runs.

#### Clear Out the Three Libraries

First clear the following files out of the three libraries

- wwwroot/background.png
- wwwroot/exampleJjInterop.js
- wwwroot
- Component1.razor
- ExampleJsInterop.cs

We'll start by incorporating the Purple WASM project.

### Mongrel.Shared

1. Create a *Components* folder.
2. Copy the following files into *components*.
   
- *Mongrel.WASM.Weather/Pages/Index.razor* rename as *IndexComponent.razor*.
- *Mongrel.WASM.Weather/Pages/Counter.razor* rename as *CounterComponent.razor*.
- *Mongrel.WASM.Weather/Pages/FetchData.razor* rename as *FetchDataComponent.razor*.
- *Mongrel.WASM.Weather/Shared/MainLayout.razor*.
- *Mongrel.WASM.Weather/Shared/MainLayout.razor.css*.
- *Mongrel.WASM.Weather/Shared/NavMenu.razor*.
- *Mongrel.WASM.Weather/Shared/NavMenu.razor.css*.
- *Mongrel.WASM.Weather/Shared/SurveyPrompt.razor*.

#### Dependencies

Include the following Packages.
```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="5.0.3" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="5.0.3" />
    <PackageReference Include="Microsoft.AspNetCore.WebUtilities" Version="2.2.0" />
  </ItemGroup>
```

#### _Imports.razor

The *_Imports.razor* should look like this:

```html
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.AspNetCore.Components.WebAssembly.Http
@using Microsoft.JSInterop
@using Mongrel.Shared.Components
```

#### Code Changes

Remove the `@page` directives from `IndexComponent`, `CounterComponent` and `FetchDataComponent`.  These are now normal components and not routed components.

```html
Remove ==>> @page "/counter"
<h1>Counter</h1>

<p>Current count: @currentCount</p>

<button class="btn btn-primary" @onclick="IncrementCount">Click me</button>

@code {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}
```

Update the NavMenu.  We add all the links we'll need in one go.  Red, Grey and Blue won't work initially.  We've added a `CascadingParameter` to capture the Site Title.

```html
// Mongrel.Shared/Components/NavMenu.razor
<div class="top-row pl-4 navbar navbar-dark">
    <a class="navbar-brand" href="">@MenuTitle</a>
    <button class="navbar-toggler" @onclick="ToggleNavMenu">
        <span class="navbar-toggler-icon"></span>
    </button>
</div>

<div class="@NavMenuCssClass" @onclick="ToggleNavMenu">
    <ul class="nav flex-column">
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/">
                <span class="oi oi-home" aria-hidden="true"></span>/ Home
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/Home" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Razor MVC Home
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/red" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Red Home
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/purple" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Purple Home
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/grey" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Grey Home
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/blue" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Blue Home
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="counter" Match="NavLinkMatch.All">
                <span class="oi oi-plus" aria-hidden="true"></span> Counter
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="fetchdata" Match="NavLinkMatch.All">
                <span class="oi oi-list-rich" aria-hidden="true"></span> Fetch data
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/grey/fetchdata" Match="NavLinkMatch.All">
                <span class="oi oi-list-rich" aria-hidden="true"></span> Grey Fetch data
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/red/fetchdata" Match="NavLinkMatch.All">
                <span class="oi oi-list-rich" aria-hidden="true"></span> Red Fetch data
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/purple/fetchdata" Match="NavLinkMatch.All">
                <span class="oi oi-list-rich" aria-hidden="true"></span> Purple Fetch data
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/blue?View=Mongrel.Server.Blue.Components.Views.FetchDataView" Match="NavLinkMatch.All">
                <span class="oi oi-list-rich" aria-hidden="true"></span> Blue Fetch data
            </NavLink>
        </li>
    </ul>
</div>
```
```c#
@code {

    [CascadingParameter(Name ="MenuTitle")] string MenuTitle { get; set; } = "Mongrel";

    private bool collapseNavMenu = true;

    private string NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }
}
```

Next we update *MainLayout.razor*. Again we capture a `CacadingParameter` to control the SideBar CSS.

```html
// Mongrel.Shared/Components/MainLayout.razor
@inherits LayoutComponentBase

<div class="page">
    <div class="@SideBar">
        <NavMenu></NavMenu>
    </div>

    <div class="main">
        <div class="top-row px-4">
            <a href="http://blazor.net" target="_blank" class="ml-md-auto">About</a>
        </div>

        <div class="content px-4">
            @Body
        </div>
    </div>
</div>
```
```c#
@code {
    [CascadingParameter(Name ="SideBar")] string SideBar { get; set; } = "sidebar";
}
```

We need to add some CSS to the component CSS

```css
// Mongrel.Shared/Components/MainLayout.razor.css
/*Existing sidebar*/
.sidebar {
    background-image: linear-gradient(180deg, rgb(5, 39, 103) 0%, #3a0647 70%);
}
/*New sidebar*/
.sidebar-red {
    background-image: linear-gradient(180deg, rgb(32, 0, 0) 0%, #400 70%);
}

.sidebar-purple {
    background-image: linear-gradient(180deg, rgb(5, 39, 103) 0%, #3a0647 70%);
}

.sidebar-grey {
    background-image: linear-gradient(180deg, rgb(16, 16, 16) 0%, #444 70%);
}
```


Finally build the individual project to make sure VS updates with all the new classes.

### Mongrel.WASM.Purple

This WASM SPA will use the normal blue/purple menu background.

Create two new folders - *Routes* and *Components*.

1. Copy the following files into *Routes*.
   
- *Mongrel.WASM.Weather/Pages/Index.razor* rename as *IndexRoute.razor*.
- *Mongrel.WASM.Weather/Pages/Counter.razor* rename as *CounterRoute.razor*.
- *Mongrel.WASM.Weather/Pages/FetchData.razor* rename as *FetchDataRoute.razor*.

2. Copy the following files into *Components*

- *Mongrel.WASM.Weather/App.razor* rename as *WASMPurpleApp.razor*.

3. Delete *Shared* and *Pages*.

4. Move the following files in *wwwroot* into *Mongrel.Web/wwwroot*.  Don't overwrite exisitng files.

- Rename *wwwroot/css/app.css* to *blazor.css*.
- *wwwroot/css* and *wwwroot/sample-data*
- Remove the *Mongrel.WASM.Weather/wwwroot* and all the files.

#### Dependencies

Add the *StaticWebAssetBasePath* and include the following Packages and projects.
```xml
<PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
    <StaticWebAssetBasePath>Purple</StaticWebAssetBasePath>
</PropertyGroup>

<ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="5.0.3" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="5.0.3" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Mongrel.Shared\Mongrel.Shared.csproj" />
  </ItemGroup>
```

#### _Imports.razor

```html
// Mongrel.WASM.Weather/_Imports.razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.AspNetCore.Components.WebAssembly.Http
@using Microsoft.JSInterop
@using Mongrel.WASM.Weather
@using Mongrel.WASM.Weather.Components
@using Mongrel.Shared.Components
```

#### Code Updates

Update the files in *Routes*.  These are here for the Router to find.  They load their respective component from the Shared Library.

```html
// Mongrel.WASM.Weather/Routes/IndexRoute.razor
@page "/"
@page "/purple"
<IndexComponent></IndexComponent>
```
```html
// Mongrel.WASM.Weather/Routes/CounterRoute.razor
@page "/counter"
<CounterComponent></CounterComponent>
```
```html
// Mongrel.WASM.Weather/Routes/FetchDataRoute.razor
@page "/fetchdata"
<FetchDataComponent></FetchDataComponent>
```

Update *WASMPurpleApp.razor*. Add the `CascadingValues` and code.  These control the Navbar title and background colour.

```html
// Mongrel.WASM.Purple/Components/WASMPurpleApp.razor
<CascadingValue Name="SideBar" Value="this.SideBar">
    <CascadingValue Name="MenuTitle" Value="this.MenuTitle">

        <Router AppAssembly="@typeof(Program).Assembly" PreferExactMatches="@true">
            <Found Context="routeData">
                <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
            </Found>
            <NotFound>
                <LayoutView Layout="@typeof(MainLayout)">
                    <p>Sorry, there's nothing at this address.</p>
                </LayoutView>
            </NotFound>
        </Router>

    </CascadingValue>
</CascadingValue>

@code {
    string SideBar = "sidebar sidebar-purple";
    string MenuTitle = "Mongrel.Server.Purple";
}
```

Update *Program.cs*, updating the root component to *WASMWeatherApp*.

```c#
// Mongrel.WASM.Purple/program.cs
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<WASMPurpleApp>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            await builder.Build().RunAsync();
        }
```

Build the individual project.  Fix any typos/errors.

### Mongrel.Web

#### Dependencies

Include the following Packages and projects.

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="5.0.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Mongrel.Server.Views\Mongrel.Server.Views.csproj" />
    <ProjectReference Include="..\Mongrel.Server.Weather\Mongrel.Server.Weather.csproj" />
    <ProjectReference Include="..\Mongrel.Shared\Mongrel.Shared.csproj" />
    <ProjectReference Include="..\Mongrel.WASM.Weather\Mongrel.WASM.Weather.csproj" />
  </ItemGroup>
```

Add a *Pages* folder and add four Razor Pages.  Remove the code behind files and model references.

- *_Purple.cshtml*
- *_Red.cshtml*
- *_Grey.cshtml*
- *_Blue.cshtml*

```html
// Mongrel.Web/Pages/_Purple.html
@page "/purple"
@{
    Layout = null;
}

<!DOCTYPE html>
<html>

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Mongrel.WASM.PURPLE</title>
    <base href="/purple/" />
    <link href="/css/bootstrap/bootstrap.min.css" rel="stylesheet" />
    <link href="/css/blazor.css" rel="stylesheet" />
    <link href="/Mongrel.Web.styles.css" rel="stylesheet" />
</head>

<body>
    <div id="app">Loading...</div>

    <script src="/purple/_framework/blazor.webassembly.js"></script>
</body>

</html>
```

This is *index.html* from the WASM project wrapped in a cshtml file:
1. The CSS references have changed slightly
2. Set the Layout to `null`.
3. Set the `<base ref>` to */purple/*.
4. Updated the *blazor.webassembly.js* URL.

  We'll come back to the other files later.

#### index.cshtml

Update the Index View.

```html
// Mongrel.Web/Views/Home/Index.cshtml
@{
    ViewData["Title"] = "Home Page";
}

<div class="text-center">
    <h1 class="display-4">Welcome to Mongrel</h1>
    <h4 class="text-secondary">A classic Razor MVC Site, hosting multiple Blazor Server and Blazor WASM SPA's.</h4>
    <p>Learn about <a href="https://docs.microsoft.com/aspnet/core">building Web apps with ASP.NET Core</a>.</p>
    <div>
        <a href="/red" class="btn btn-danger"> WASM Red</a>
        <a href="/purple" class="btn btn-dark"> WASM Purple</a>
        <a href="/grey" class="btn btn-secondary"> Server Grey</a>
        <a href="/blue" class="btn btn-primary"> Server Blue</a>
    </div>
</div>
```

#### Startup.cs

We're:
1. Adding in Support for Razor Pages to the Services.
2. Add a specific `Endpoint` configuration for */purple/** URLs.
   i. Mounts the *_framework* files associated with a referenced project with a `<StaticWebAssetBasePath>` of Purple.
   2. Sets a Fallback to *_Purple.cshtml* for all /purple/* URLs
3. Adds the discovered site RazorPages to the default EndPoint.

```c#
// Mongrel.Web/StartUp.cs
public void ConfigureServices(IServiceCollection services)
{
    // add
    services.AddRazorPages();
    ....
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    ....
    app.UseStaticFiles();
    // Add Specific EndPoint configuration
    app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/purple"), apppurple =>
    {
        apppurple.UseHttpsRedirection();
        apppurple.UseStaticFiles();

        apppurple.UseBlazorFrameworkFiles("/purple");

        apppurple.UseRouting();

        apppurple.UseEndpoints(endpoints =>
        {
            endpoints.MapFallbackToPage("/purple/{*path:nonfile}", "/_Purple");
        });

    });

    .....
    // default EndPoint Configuration
    app.UseEndpoints(endpoints =>
    {
        // Add
        endpoints.MapRazorPages();

        endpoints.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
    });
}
```

This should all now compile and run.

## Mongrel.WASM.Red

Go though the same setup with the *Mongrel.WASM.Red* project.  Making the folloing changes:

Make sure to call your root component *WASMRedApp.razor*


```html
// Mongrel.WASM.Red/Components/WASMRedApp.razor
......
@code {
    string SideBar = "sidebar sidebar-red";
    string MenuTitle = "Mongrel.Server.Red";
}
```
```c#
// Mongrel.WASM.Red/program.cs
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<WASMRedApp>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            await builder.Build().RunAsync();
        }
```

```xml
// Mongrel.WASM.Red/Mongrel.WASM.Red.csproj
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">

  <PropertyGroup>
    <TargetFramework>net5.0</TargetFramework>
    <StaticWebAssetBasePath>red</StaticWebAssetBasePath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="5.0.3" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="5.0.3" PrivateAssets="all" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Mongrel.Shared\Mongrel.Shared.csproj" />
  </ItemGroup>

</Project>
```
```html
// Mongrel.Web/Pages/_Red.html
@page "/red"
@{
    Layout = null;
}

<!DOCTYPE html>
<html>

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Mongrel.WASM.RED</title>
    <base href="/purple/" />
    <link href="/css/bootstrap/bootstrap.min.css" rel="stylesheet" />
    <link href="/css/blazor.css" rel="stylesheet" />
    <link href="/Mongrel.Web.styles.css" rel="stylesheet" />
</head>

<body>
    <div id="app">Loading...</div>

    <script src="/red/_framework/blazor.webassembly.js"></script>
</body>

</html>
```
```c#
// Mongrel.Web/StartUp.cs

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    ....
    app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/red"), app1 =>
    {
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app1.UseBlazorFrameworkFiles("/red");
        app1.UseRouting();
        app1.UseEndpoints(endpoints =>
        {
            endpoints.MapFallbackToPage("/red/{*path:nonfile}", "/_Red");
        });
    });
    ....
}
```

Again compile the project, then compile the solution.

## Mongrel.Server.Grey

This is the library were we'll use to build one of the Blazor Server SPAs.

#### Dependencies

Include the following Packages and projects.

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="5.0.3" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="5.0.3" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Mongrel.Shared\Mongrel.Shared.csproj" />
  </ItemGroup>
```

#### File Moves and Renames

Create two new folders - *Routes* and *Components*.

1. Copy the following files into *Routes*.
   
- *Mongrel.WASM.Purple/Pages/IndexRoute.razor*.
- *Mongrel.WASM.Purple/Pages/CounterRoute.razor*.
- *Mongrel.WASM.Purple/Pages/FetchDataRoute.razor*.

2. Copy the following files into *Components*

- *Mongrel.WASM.Purple/WASMPurpleApp.razor* rename as *ServerGreyApp.razor*.

#### _Imports.razor

```html
// Mongrel.Server.Grey/_Imports.razor
@using System.Net.Http
@using System.Net.Http.Json
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using Mongrel.Shared.Components
```

#### Code Updates

Update the files in *Routes*.  These are here for the Router to find.  They load their respective component from the Shared Library.

```html
// Mongrel.Server.Weather/Routes/IndexRoute.razor
@page "/"
@page "/grey"
<IndexComponent></IndexComponent>
```

Update *ServerGreyApp.razor*, changing the layouts and the `AppAssembly`, which is set to *Mongrel.Server.Grey*, so it finds the routes in the *Routes* folder only.  

```html
// Mongrel.Server.Grey/Components/ServerGreyApp.razor
<CascadingValue Name="SideBar" Value="this.SideBar">
    <CascadingValue Name="MenuTitle" Value="this.MenuTitle">

        <Router AppAssembly="@typeof(Mongrel.Server.Grey.Routes.IndexRoute).Assembly" PreferExactMatches="@true">
            <Found Context="routeData">
                <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
            </Found>
            <NotFound>
                <LayoutView Layout="@typeof(MainLayout)">
                    <p>Sorry, there's nothing at this address.</p>
                </LayoutView>
            </NotFound>
        </Router>

    </CascadingValue>
</CascadingValue>
```
```c#
@code {
    string SideBar = "sidebar sidebar-grey";

    string MenuTitle = "Mongrel.Server.Grey";
}
```

### Mongrel.Web

```html
// Mongrel.Web/Pages/_Grey.html
@page "/grey"
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
    <base href="/grey/" />
    <link href="/css/bootstrap/bootstrap.min.css" rel="stylesheet" />
    <link href="/css/blazor.css" rel="stylesheet" />
    <link href="/Mongrel.Web.styles.css" rel="stylesheet" />
</head>

<body>
    <component type="typeof(Mongrel.Server.Grey.Components.ServerGreyApp)" render-mode="ServerPrerendered" />

    <script src="_framework/blazor.server.js"></script>
</body>

</html>```
This is *_Index.cshtml* that you gwet if you create a Blazor Server project.  The CSS references have changed slightly, and we've set the Layout to `null`.  We've also specified the root component as `Mongrel.Server.Grey.Components.ServerWeatherApp`, and set the `base`.


#### Startup.cs

We're:
1. Adding ServerSideBlazor into the services.
3. Adding a Map for all *grey/* URLs.

```c#
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Net.Http;

public void ConfigureServices(IServiceCollection services)
{
    .....
    // add
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
    ....
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
.....
    app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/grey"), appgrey =>
    {
        appgrey.UseHttpsRedirection();
        appgrey.UseStaticFiles();

        appgrey.UseBlazorFrameworkFiles();

        appgrey.UseRouting();

        appgrey.UseEndpoints(endpoints =>
        {
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/grey/{*path:nonfile}", "/_Grey");
        });
    });
```

This should all now compile and run.

## The View Project

This project will use a view manager in place of routing.  This is available as a package *CEC.Blazor.Core*.

### Mongrel.Server.Blue  

#### Dependencies

Include the following Packages and projects.

```xml
  <ItemGroup>
    <PackageReference Include="CEC.Blazor.Core" Version="1.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="5.0.3" />
    <PackageReference Include="Microsoft.AspNetCore.Components.Web" Version="5.0.3" />
    <PackageReference Include="Microsoft.AspNetCore.WebUtilities" Version="2.2.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Mongrel.Shared\Mongrel.Shared.csproj" />
  </ItemGroup>
```
#### _Imports.razor

```html
// Mongrel.Server.Views/_Imports.razor
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Mongrel.Shared.Components
@using Mongrel.Server.Views.Components.Controls
@using Mongrel.Server.Views.Components.Views
@using CEC.Blazor.Core
```

#### File Moves and Renames

Create a folder with two sub-folders - *Components*, *Components/Controls* and *Components/Views*.

1. Copy the following files into *Controls*.
   
- *Mongrel.Server.Grey/Components/ServerWeatherApp.razor*, rename as *ServerBlueApp.razor*.
- *Mongrel.Shared/Components/MainLayout.razor*, rename as *ServerBlueLayout.razor*.
- *Mongrel.Shared/Components/NavMenu.razor*, rename as *ServerBlueNavMenu.razor*.
- Add a new file *ViewLink.cs*.

2. Copy the following files into *Views*.

- *Mongrel.WASM.Weather/Routes/IndexRoute.razor*, rename as *IndexView.razor*.
- *Mongrel.WASM.Weather/Routes/CounterRoute.razor*, rename as *CounterView.razor*.
- *Mongrel.WASM.Weather/Routes/FetchDataRoute.razor*, rename as *FetchDataView.razor*.



#### Code Updates

Update the files in *Views*.  These are here for the Router to find.  They load their respective component from the Shared Library.

```html
// Mongrel.Server.Views/Components/Views/IndexView.razor
@inherits ViewBase
<IndexComponent></IndexComponent>
```
```html
// Mongrel.Server.Views/Components/Views/CounterView.razor
@inherits ViewBase
<CounterComponent></CounterComponent>
```
```html
// Mongrel.Server.Views/Components/Views/FetchDataView.razor
@inherits ViewBase
<FetchDataComponent></FetchDataComponent>
```

Update *ServerBlueApp.razor*, changing the layouts.  

```html
// Mongrel.Server.Blue/Components/Controls/ServerViewerApp.razor
<ViewManager DefaultViewData="this.viewData" DefaultLayout="typeof(ServerViewerLayout)">
</ViewManager>

@code {
    public ViewData viewData = new ViewData(typeof(IndexView), null);
}
```

Change the CSS in `ServerViewerLayout` to make the background Grey.

```css
// Mongrel.Server.Views/Components/Controls/ServerViewerLayout.razor.css
.sidebar {
    background-image: linear-gradient(180deg, rgb(0, 16, 64) 0%, #028 70%);
}
```

#### ServerViewerNavMenu.razor

```html
// Mongrel.Server.Blue/Components/Controls/ServerBlueNavMenu.razor
.....
<div class="@NavMenuCssClass" @onclick="ToggleNavMenu">
    <ul class="nav flex-column">
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> / Home
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/Home" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Razor MVC Home
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/purple" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Purple Home
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/red" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Red Home
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <NavLink class="nav-link" href="/grey" Match="NavLinkMatch.All">
                <span class="oi oi-home" aria-hidden="true"></span> Grey Home
            </NavLink>
        </li>
        <li class="nav-item px-3">
            <ViewLink class="nav-link" ViewType="typeof(IndexView)">
                <span class="oi oi-home" aria-hidden="true"></span> Home
            </ViewLink>
        </li>
        <li class="nav-item px-3">
            <ViewLink class="nav-link" ViewType="typeof(CounterView)">
                <span class="oi oi-home" aria-hidden="true"></span> Counter
            </ViewLink>
        </li>
        <li class="nav-item px-3">
            <ViewLink class="nav-link" ViewType="typeof(FetchDataView)">
                <span class="oi oi-home" aria-hidden="true"></span> Fetch Data
            </ViewLink>
        </li>
    </ul>
</div>
....
```

#### ServerViewerLayout.razor

```html
// Mongrel.Server.Blue/Components/Controls/ServerBlueLayout.razor
....
    <div class="sidebar">
        <ServerBlueNavMenu />
    </div>
....
```

#### ViewLink.cs

This control is the Viewer's equivalent to `NavLink`.
```c#
// Mongrel.Server.Blue/Components/Controls/ViewLink.razor
using System;
using System.Collections.Generic;
using System.Globalization;
using CEC.Blazor.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace Mongrel.Shared.Components
{
    /// <summary>
    /// Builds a Bootstrap View Link
    /// </summary>
    public class ViewLink : ComponentBase
    {
        /// <summary>
        /// View Type to Load
        /// </summary>
        [Parameter] public Type ViewType { get; set; }

        /// <summary>
        /// View Paremeters for the View
        /// </summary>
        [Parameter] public Dictionary<string, object> ViewParameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Child Content to add to Component
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Cascaded ViewManager
        /// </summary>
        [CascadingParameter] public ViewManager ViewManager { get; set; }

        /// <summary>
        /// Boolean to check if the ViewType is the current loaded view
        /// if so it's used to mark this component's CSS with "active" 
        /// </summary>
        private bool IsActive => this.ViewManager.IsCurrentView(this.ViewType);
        
        [Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object> AdditionalAttributes { get; set; }

        /// <summary>
        /// inherited
        /// Builds the render tree for the component
        /// </summary>
        /// <param name="builder"></param>
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var css = string.Empty;
            var viewData = new ViewData(ViewType, ViewParameters);

            if (AdditionalAttributes != null && AdditionalAttributes.TryGetValue("class", out var obj))
            {
                css = Convert.ToString(obj, CultureInfo.InvariantCulture);
            }
            if (this.IsActive) css = $"{css} active";
            builder.OpenElement(0, "a");
            builder.AddAttribute(1, "class", css);
            builder.AddMultipleAttributes(2, AdditionalAttributes);
            builder.AddAttribute(3, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, e => this.ViewManager.LoadViewAsync(viewData)));
            builder.AddContent(4, ChildContent);
            builder.CloseElement();
        }
    }
}
```

### Mongrel.Web

#### _Blue.html

This is the same as *_Grey.cshtml*, but the root component is now `Mongrel.Server.Blue.Components.ServerBlueApp`.

```html
// Mongrel.Web/Pages/_Blue.html
@page "/blue"
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@{
    Layout = null;
}

<!DOCTYPE html>
<html>

<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no" />
    <title>Mongrel.Server.BLUE</title>
    <base href="/blue" />
    <link href="/css/bootstrap/bootstrap.min.css" rel="stylesheet" />
    <link href="/css/blazor.css" rel="stylesheet" />
    <link href="/Mongrel.Web.styles.css" rel="stylesheet" />
</head>

<body>
    <component type="typeof(Mongrel.Server.Blue.Components.Controls.ServerBlueApp)" render-mode="ServerPrerendered" />

    <script src="_framework/blazor.server.js"></script>
</body>

</html>
```
  
#### Startup.cs

We add the Map Handler for all /blue/* URLs.

```c#
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    .....
    app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/blue"), appblue =>
    {
        appblue.UseHttpsRedirection();
        appblue.UseStaticFiles();

        appblue.UseBlazorFrameworkFiles();

        appblue.UseRouting();

        appblue.UseEndpoints(endpoints =>
        {
            endpoints.MapBlazorHub();
            endpoints.MapFallbackToPage("/blue/{*path:nonfile}", "/_Blue");
        });

    });
    ....
}
```

This should all now compile and run.

