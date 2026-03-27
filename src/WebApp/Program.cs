using eShop.WebApp.Components;
using eShop.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 🔐 AUTH
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie()
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = "http://identity-service"; // internal service

    options.ClientId = "webapp";
    options.ResponseType = "code";

    options.SaveTokens = true;
    options.RequireHttpsMetadata = false;

    options.Scope.Add("openid");
    options.Scope.Add("profile");
});

builder.Services.AddAuthorization();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.AddApplicationServices();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// 🔐 AUTH MIDDLEWARE
app.UseAuthentication();
app.UseAuthorization();

// 🔥 SAFE LOGIN MIDDLEWARE (FIXED NULL + ERROR)
app.Use(async (context, next) =>
{
    if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
    {
        await context.ChallengeAsync("oidc");
        return;
    }

    await next();
});

// UI
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// API Forwarder
app.MapForwarder(
    "/product-images/{id}",
    "http://catalog-service",
    "/api/catalog/items/{id}/pic"
);

app.Run();
