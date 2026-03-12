// Blazor Web App with InteractiveServer components
// Demonstrates: Modern Blazor architecture, service discovery via Aspire
using CloudBurger.Web.Components;
using CloudBurger.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Blazor components with InteractiveServer render mode
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HTTP clients for API communication with service discovery
builder.Services.AddHttpClient<ApiClient>(client =>
{
    // Local/dev fallback for running Web + API without AppHost
    var configuredApiBaseUrl = builder.Configuration["ApiBaseUrl"];

    // Aspire service discovery - 'api' is the service name from AppHost
    // Used when ApiBaseUrl is not configured.
    client.BaseAddress = string.IsNullOrWhiteSpace(configuredApiBaseUrl)
        ? new Uri("https+http://api")
        : new Uri(configuredApiBaseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
