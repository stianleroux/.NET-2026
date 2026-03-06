// Blazor Web App with InteractiveServer components
// Demonstrates: Modern Blazor architecture, service discovery via Aspire
using CloudPizza.Web.Components;
using CloudPizza.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults
builder.AddServiceDefaults();

// Add Blazor components with InteractiveServer render mode
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add HTTP clients for API communication with service discovery
builder.Services.AddHttpClient<ApiClient>(client =>
{
    // Aspire service discovery - 'api' is the service name from AppHost
    client.BaseAddress = new Uri("https+http://api");
});

var app = builder.Build();

// Map Aspire defaults
app.MapDefaultEndpoints();

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
