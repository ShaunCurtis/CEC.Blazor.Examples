# The Blazor Hydra

I've tried to keep this as simple as possible, basing the sites on the out-of-the-box templates.  I've made no attempt to consolidate code into shared libraries.

![Hydra](https://github.com/ShaunCurtis/CEC.Blazor.Examples/blob/master/Images/Hydra.png?raw=true)

## Building the Hydra Site

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

