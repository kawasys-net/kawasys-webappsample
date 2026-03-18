using kawasys.webappsample.Components;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(kawasys.webappsample.Client._Imports).Assembly);

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// this is for authentication / authorization, to make sure the remote will ONLY connect to the shell and nothing else, will later be used to propagate proper auth state across remotes
app.Use(async (context, next) =>
{
    var cache = context.RequestServices.GetRequiredService<IMemoryCache>();
    var path = context.Request.Path.Value ?? "";
    var shellId = context.Request.Query["shellId"];
    var referer = context.Request.Headers["Referer"].ToString();

    // Create a unique key based on the user's IP address
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var cacheKey = $"Auth_{clientIp}";

    // 1. Validation Logic
    bool hasValidKey = shellId == "MySuperSecretAppGuid_12345";
    bool isAuthorizedByParent = referer.Contains("shellId=MySuperSecretAppGuid_12345");
    bool isRecentlyAuthenticated = cache.TryGetValue(cacheKey, out _);

    if (hasValidKey || isAuthorizedByParent || isRecentlyAuthenticated)
    {
        // 2. If they just used the key, remember this IP for 60 seconds
        // to allow all the "bundle.scp.css" and "blazor.js" files to load.
        if (hasValidKey)
        {
            cache.Set(cacheKey, true, TimeSpan.FromSeconds(60));
        }

        context.Response.Headers.Append("Content-Security-Policy", "frame-ancestors 'self' https://0.0.0.1 https://localhost:7268;");
        await next();
    }
    else
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("Access Denied.");
    }
});

app.Run();
