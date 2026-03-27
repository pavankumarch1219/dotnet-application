using eShop.WebApp.Components;
using eShop.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// ✅ Read callback URL from env (set in deployment)
var callbackUrl = builder.Configuration["CallBackUrl"] ?? "http://20.220.149.85:30007";

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = "oidc";
})
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = builder.Configuration["IdentityUrl"] ?? "http://identity-api";
    options.ClientId = "webapp";
    options.ClientSecret = "secret";
    options.ResponseType = OpenIdConnectResponseType.Code;
    options.SaveTokens = true;
    options.RequireHttpsMetadata = false;
    options.GetClaimsFromUserInfoEndpoint = true;

    // ✅ THIS IS THE KEY FIX — tells OIDC what redirect_uri to send to identity server
    options.CallbackPath = "/signin-oidc";

    // ✅ Override the redirect URI so identity server gets the correct external URL
    options.Events.OnRedirectToIdentityProvider = context =>
    {
        context.ProtocolMessage.RedirectUri = $"{callbackUrl}/signin-oidc";
        return Task.CompletedTask;
    };

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

// ✅ Required for Blazor SignalR to work behind proxy/NodePort
builder.Services.AddSignalR();

builder.AddApplicationServices();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapForwarder(
    "/product-images/{id}",
    "http://catalog-api",
    "/api/catalog/items/{id}/pic"
);

app.Run();
