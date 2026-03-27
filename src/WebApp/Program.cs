using eShop.WebApp.Components;
using eShop.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// 🔐 AUTH (SAFE CONFIG)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie()
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = "http://identity-service"; // internal K8s service

    options.ClientId = "webapp";
    options.ResponseType = "code";

    options.SaveTokens = true;
    options.RequireHttpsMetadata = false;

    options.Scope.Add("openid");
    options.Scope.Add("profile");

    // 🔥 IMPORTANT: prevent crash if identity not ready
    options.Events.OnRemoteFailure = context =>
    {
        context.HandleResponse();
        context.Response.Redirect("/");
        return Task.CompletedTask;
    };
});

builder.Services.AddAuthorization();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.AddApplicationServices();

var app = builder.Build();

app.MapDefaultEndpoints();

// =========================
// 🔧 PIPELINE
// =========================
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

// ❌ REMOVED FORCED LOGIN (THIS WAS CRASH CAUSE)

// =========================
// 🌐 UI
// =========================
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// =========================
// 🔁 FORWARDER (FIXED)
// =========================
app.MapForwarder(
    "/product-images/{id}",
    "http://catalog-service",
    "/api/catalog/items/{id}/pic"
);

app.Run();
