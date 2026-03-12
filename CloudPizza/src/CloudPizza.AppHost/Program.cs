// Aspire AppHost - Modern .NET 10 orchestration with service discovery
// Demonstrates: Primary constructors, extension members, container-free local dev

using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var useLocalPostgres = builder.Configuration.GetValue("UseLocalPostgres", false);
var localPostgresConnectionString = builder.Configuration.GetConnectionString("burgerdb");

IResourceBuilder<ProjectResource> api;

if (useLocalPostgres)
{
    if (string.IsNullOrWhiteSpace(localPostgresConnectionString))
    {
        throw new InvalidOperationException(
            "UseLocalPostgres is true but ConnectionStrings:burgerdb is not configured for AppHost.");
    }

    // Use local PostgreSQL instance (no Docker)
    api = builder.AddProject("api", "../CloudPizza.Api/CloudBurger.Api.csproj")
        .WithEnvironment("ConnectionStrings__burgerdb", localPostgresConnectionString)
        .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true");
}
else
{
    // PostgreSQL with persistent data volume for local development
    // Using Aspire's built-in container hosting - no Dockerfile needed
    var postgres = builder.AddPostgres("postgres")
        .WithDataVolume("cloudburger-postgres-data")
        .WithPgAdmin(); // Add pgAdmin for database management

    var burgerDb = postgres.AddDatabase("burgerdb");

    // API service with health checks and OpenTelemetry
    api = builder.AddProject("api", "../CloudPizza.Api/CloudBurger.Api.csproj")
        .WithReference(burgerDb)
        .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true") // For Cloudflare Tunnel
        .WaitFor(burgerDb); // Ensure database is ready before starting API
}

// Blazor frontend with real-time SSE connection to API
var web = builder.AddProject("web", "../CloudPizza.Web/CloudBurger.Web.csproj")
    .WithReference(api)
    .WaitFor(api);

// Build and run the application
// Single command: dotnet run --project src/CloudPizza.AppHost
// Aspire dashboard at: http://localhost:15000
await builder.Build().RunAsync();
