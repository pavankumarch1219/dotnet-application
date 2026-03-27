using eShop.WebApp.Components;
using eShop.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 🔐 AUTH CONFIG
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie()
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = "http://20.220.149.85/identity"; // 🔥 replace with your VM IP

    options.ClientId = "webapp";
    options.ResponseType = "code";

    options.SaveTokens = true;
    options.RequireHttpsMetadata = false;

    options.Scope.Add("openid");
    options.Scope.Add("profile");

    // 🔥 IMPORTANT (avoid redirect issues)
    options.CallbackPath = "/signin-oidc";
});

builder.Services.AddAuthorization();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.AddApplicationServices();

var app = builder.Build();

app.MapDefaultEndpoints();

// ---------------- PIPELINE ----------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// 🔥 SAFE LOGIN REDIRECT (NO LOOP)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;

    // allow these paths without auth
    if (path.StartsWith("/signin-oidc") ||
        path.StartsWith("/identity") ||
        path.StartsWith("/_framework") ||
        path.StartsWith("/css") ||
        path.StartsWith("/js") ||
        path.StartsWith("/images"))
    {
        await next();
        return;
    }

    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        await context.ChallengeAsync("oidc");
        return;
    }

    await next();
});

// UI
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// API forwarder
app.MapForwarder(
    "/product-images/{id}",
    "https+http://catalog-api",
    "/api/catalog/items/{id}/pic"
);

app.Run();
