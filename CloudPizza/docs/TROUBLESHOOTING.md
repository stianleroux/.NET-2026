# Troubleshooting Guide

## Common Issues and Solutions

### Build Issues

#### Error: "The type or namespace name 'Aspire' could not be found"
**Solution:**
```bash
dotnet workload update
dotnet workload install aspire
```

#### Error: "Could not find a part of the path"
**Solution:** Ensure you're running from the solution root:
```bash
cd CloudPizza
dotnet build
```

### Database Issues

#### Error: "Failed to connect to database"
**Solution:**
1. Ensure Docker is running
2. Check PostgreSQL container is up:
   ```bash
   docker ps | grep postgres
   ```
3. If using Aspire, check the dashboard for service status

#### Error: "Database migration failed"
**Solution:**
```bash
# Manual migration
cd src/CloudPizza.Api
dotnet ef database update
```

#### Trigger not firing
**Solution:**
1. Connect to PostgreSQL:
   ```bash
   docker exec -it cloudpizza-postgres psql -U postgres -d cloudpizza
   ```
2. Verify trigger exists:
   ```sql
   SELECT * FROM pg_trigger WHERE tgname = 'order_created_trigger';
   ```
3. Recreate trigger:
   ```sql
   -- See DatabaseInitializer.cs for trigger SQL
   ```

### SSE Issues

#### Orders not appearing in real-time
**Checklist:**
1. Check browser console for errors
2. Verify SSE connection in Network tab (should show "EventStream")
3. Check API logs for "PostgreSQL LISTEN service starting"
4. Verify PostgresNotificationService is running in Aspire dashboard

**Common causes:**
- Browser connection limit reached (close other tabs)
- Proxy blocking SSE (try without proxy)
- PostgreSQL connection dropped (check logs)

#### SSE connection closes immediately
**Solution:**
- Check CORS configuration in API
- Verify API is accessible from Blazor frontend
- Check for firewall/antivirus blocking

### Aspire Issues

#### Aspire dashboard won't start
**Solution:**
```bash
# Reinstall Aspire workload
dotnet workload update
dotnet workload install aspire
```

#### Services not showing in dashboard
**Solution:**
1. Check `AppHost/Program.cs` configuration
2. Ensure service discovery is configured correctly
3. Verify projects are referenced correctly

### Cloudflare Tunnel Issues

#### Tunnel URL returns 502 Bad Gateway
**Solution:**
1. Ensure your app is running first
2. Point tunnel to the correct port:
   ```bash
   cloudflared tunnel --url https://localhost:7174
   ```
3. Verify HTTPS redirect is not causing issues

#### QR code generation fails
**Solution:**
- Provide full URL including https://
- Ensure URL is publicly accessible
- Check API logs for error messages

### Performance Issues

#### Slow database queries
**Solution:**
1. Enable query logging:
   ```json
   "Logging": {
     "LogLevel": {
       "Microsoft.EntityFrameworkCore.Database.Command": "Information"
     }
   }
   ```
2. Check for N+1 queries
3. Add database indexes if needed

#### High memory usage
**Solution:**
- Check for memory leaks in SSE connections
- Verify background services are disposing properly
- Use `dotnet-counters` to monitor:
   ```bash
   dotnet tool install --global dotnet-counters
   dotnet counters monitor --process-id <PID>
   ```

### Deployment Issues

#### Container won't start
**Solution:**
1. Check logs:
   ```bash
   docker logs <container-id>
   ```
2. Verify environment variables are set
3. Check database connection string

#### Missing environment variables
**Solution:**
Add to `appsettings.Production.json`:
```json
{
  "ConnectionStrings": {
    "pizzadb": "${DATABASE_URL}"
  }
}
```

## Logging and Diagnostics

### Enable Detailed Logging
Add to `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information",
      "CloudPizza": "Debug"
    }
  }
}
```

### View Aspire Telemetry
1. Open Aspire dashboard (https://localhost:17000)
2. Navigate to:
   - **Traces**: See request flow
   - **Metrics**: CPU, memory, request counts
   - **Logs**: Structured logs from all services
   - **Console**: Standard output/error

### Database Query Logging
```bash
# PostgreSQL logs
docker logs cloudpizza-postgres -f

# Or connect and enable statement logging
docker exec -it cloudpizza-postgres psql -U postgres
ALTER SYSTEM SET log_statement = 'all';
SELECT pg_reload_conf();
```

## Health Checks

### Check Service Health
```bash
# API health
curl https://localhost:5001/health

# Aspire health
curl https://localhost:17000/health
```

### Check Database Connection
```bash
# From within API container
dotnet ef dbcontext info -p src/CloudPizza.Infrastructure -s src/CloudPizza.Api
```

## Performance Profiling

### Use dotnet-trace
```bash
dotnet tool install --global dotnet-trace
dotnet trace collect --process-id <PID> --profile cpu-sampling
```

### Use dotnet-dump
```bash
dotnet tool install --global dotnet-dump
dotnet dump collect --process-id <PID>
dotnet dump analyze <dump-file>
```

## Getting Help

If you're still stuck:

1. **Check Logs**: Aspire dashboard → Logs tab
2. **Enable Debugging**: Set breakpoints in VS/VS Code
3. **Isolate the Issue**: Test each component separately
4. **GitHub Issues**: Search existing issues or create new one
5. **Stack Overflow**: Tag with `.net`, `aspire`, `blazor`, etc.

## Quick Diagnostic Commands

```bash
# Check .NET version
dotnet --version

# Check installed workloads
dotnet workload list

# Check running containers
docker ps

# Check ports in use
netstat -an | findstr "5000 5001 7174"  # Windows
netstat -an | grep "5000\|5001\|7174"   # Linux/Mac

# Test database connection
docker exec cloudpizza-postgres pg_isready -U postgres

# View recent logs
docker logs cloudpizza-postgres --tail 50
```

## Reset Everything

If all else fails, nuclear option:

```bash
# Stop all containers
docker stop $(docker ps -aq)
docker rm $(docker ps -aq)

# Clean .NET
dotnet clean
rm -rf bin obj

# Rebuild
dotnet restore
dotnet build

# Run from Aspire
dotnet run --project src/CloudPizza.AppHost
```
