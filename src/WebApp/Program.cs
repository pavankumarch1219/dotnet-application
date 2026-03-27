using eShop.WebApp.Components;
using eShop.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ✅ AUTH — .AddCookie() is already registered by AddServiceDefaults(), do NOT call it again
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "oidc";
})
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = builder.Configuration["IdentityUrl"] ?? "http://identity-service";
    options.ClientId = "webapp";
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.RequireHttpsMetadata = false;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
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

// ✅ No HTTPS redirection — pod runs HTTP-only inside K8s
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAntiforgery();

// ✅ Auth middleware in correct order
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapForwarder(
    "/product-images/{id}",
    "http://catalog-service",
    "/api/catalog/items/{id}/pic"
);

app.Run();
