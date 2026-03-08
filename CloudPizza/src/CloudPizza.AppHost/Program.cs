// Aspire AppHost - Modern .NET 10 orchestration with service discovery
// Demonstrates: Primary constructors, extension members, container-free local dev

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL with persistent data volume for local development
// Using Aspire's built-in container hosting - no Dockerfile needed
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("cloudpizza-postgres-data")
    .WithPgAdmin(); // Add pgAdmin for database management

var pizzaDb = postgres.AddDatabase("pizzadb");

// API service with health checks and OpenTelemetry
var api = builder.AddProject("api", "../CloudPizza.Api/CloudPizza.Api.csproj")
    .WithReference(pizzaDb)
    .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true") // For Cloudflare Tunnel
    .WaitFor(pizzaDb); // Ensure database is ready before starting API

// Blazor frontend with real-time SSE connection to API
var web = builder.AddProject("web", "../CloudPizza.Web/CloudPizza.Web.csproj")
    .WithReference(api)
    .WaitFor(api);

// Build and run the application
// Single command: dotnet run --project src/CloudPizza.AppHost
// Aspire dashboard at: http://localhost:15000
await builder.Build().RunAsync();
