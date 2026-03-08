// Modern .NET 10 Minimal API with clean architecture
// Demonstrates: Minimal APIs, route groups, typed results, OpenAPI 3.1, Scalar
using CloudPizza.Api.Features.Orders;
using CloudPizza.Api.Features.QrCode;
using CloudPizza.Api.Infrastructure;
using CloudPizza.Infrastructure;
using Microsoft.AspNetCore.Http.HttpResults;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add database with Aspire Npgsql integration
builder.AddNpgsqlDbContext<CloudPizza.Infrastructure.Data.PizzaDbContext>("pizzadb");

// Add Infrastructure services (Result pattern, SSE, QR codes, LISTEN/NOTIFY)
// Skip DbContext registration since it's already added by Aspire
builder.Services.AddInfrastructure(builder.Configuration, skipDbContext: true);

// .NET 10 built-in validation for Minimal APIs
builder.Services.AddValidation();

// Add OpenAPI 3.1 with rich metadata
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "CloudPizza API",
            Version = "v1",
            Description = """
                Modern pizza ordering API showcasing .NET 10 capabilities:
                - Minimal APIs with route groups
                - Server-Sent Events for real-time updates
                - PostgreSQL LISTEN/NOTIFY for change detection
                - Result pattern for explicit error handling
                - Strongly-typed IDs
                - Clean Architecture principles
                """,
            Contact = new()
            {
                Name = "CloudPizza Team",
                Url = new Uri("https://github.com/yourorg/cloudpizza")
            }
        };
        return Task.CompletedTask;
    });
});

// Add CORS for Blazor frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Add problem details for better error responses
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

// Add global exception handling middleware
app.UseExceptionHandler();
app.UseStatusCodePages();

// Enable CORS
app.UseCors();

// Configure OpenAPI and Scalar (replaces Swagger)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // Use Scalar instead of Swagger - modern, fast, beautiful
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("CloudPizza API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithPreferredScheme("https");
    });
}

// API route groups for clean organization
var api = app.MapGroup("/api")
    .WithOpenApi();

// Map feature endpoints using extension methods (feature-based organization)
api.MapOrderEndpoints();
api.MapQrCodeEndpoints();

// Root endpoint
app.MapGet("/", () => TypedResults.Ok(new
{
    Service = "CloudPizza API",
    Version = "1.0",
    Documentation = "/scalar/v1",
    Features = new[]
    {
        "Pizza Ordering",
        "Real-time SSE Updates",
        "PostgreSQL LISTEN/NOTIFY",
        "QR Code Generation"
    }
})).WithName("GetRoot").WithOpenApi();

app.Run();
