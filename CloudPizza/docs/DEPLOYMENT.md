# Deployment Guide

## Deployment Options

CloudBurger can be deployed to various platforms. This guide covers the most common options.

## Table of Contents
- [Azure](#azure)
- [AWS](#aws)
- [Docker](#docker)
- [Kubernetes](#kubernetes)
- [Cloudflare Pages + Cloudflare Workers](#cloudflare)

---

## Prerequisites for All Deployments

1. PostgreSQL database (managed or containerized)
2. Connection string configured
3. Environment variables set
4. Application built in Release mode

```bash
dotnet publish -c Release
```

---

## Azure

### Azure Container Apps with Aspire

Aspire has first-class support for Azure Container Apps.

```bash
# Install Azure Developer CLI
winget install microsoft.azd

# Login to Azure
azd auth login

# Initialize Aspire deployment
azd init

# Deploy
azd up
```

This will:
- Create Azure Container Apps for each service
- Provision Azure PostgreSQL Flexible Server
- Configure environment variables
- Set up networking and service discovery

### Manual Azure Deployment

#### 1. Create Azure Resources
```bash
# Resource group
az group create --name rg-cloudburger --location eastus

# PostgreSQL
az postgres flexible-server create \
  --name cloudburger-db \
  --resource-group rg-cloudburger \
  --location eastus \
  --admin-user cloudburger \
  --admin-password <YourPassword> \
  --sku-name Standard_B1ms

# App Service Plan
az appservice plan create \
  --name cloudburger-plan \
  --resource-group rg-cloudburger \
  --is-linux \
  --sku B1

# Web Apps
az webapp create \
  --name cloudburger-api \
  --resource-group rg-cloudburger \
  --plan cloudburger-plan \
  --runtime "DOTNET|10.0"

az webapp create \
  --name cloudburger-web \
  --resource-group rg-cloudburger \
  --plan cloudburger-plan \
  --runtime "DOTNET|10.0"
```

#### 2. Deploy Applications
```bash
# Build and publish
dotnet publish src/CloudPizza.Api -c Release -o ./publish/api
dotnet publish src/CloudPizza.Web -c Release -o ./publish/web

# Deploy (using Azure CLI)
az webapp deploy --resource-group rg-cloudburger \
  --name cloudburger-api \
  --src-path publish/api.zip

az webapp deploy --resource-group rg-cloudburger \
  --name cloudburger-web \
  --src-path publish/web.zip
```

#### 3. Configure Connection Strings
```bash
az webapp config connection-string set \
  --name cloudburger-api \
  --resource-group rg-cloudburger \
  --settings burgerdb="Host=cloudburger-db.postgres.database.azure.com;Database=cloudburger;Username=cloudburger;Password=<YourPassword>" \
  --connection-string-type PostgreSQL
```

---

## AWS

### AWS Elastic Beanstalk

#### 1. Install EB CLI
```bash
pip install awsebcli
```

#### 2. Initialize EB
```bash
eb init -p docker cloudburger-api --region us-east-1
```

#### 3. Create Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/CloudPizza.Api/CloudBurger.Api.csproj", "src/CloudPizza.Api/"]
COPY ["src/CloudPizza.Shared/CloudBurger.Shared.csproj", "src/CloudPizza.Shared/"]
COPY ["src/CloudPizza.Infrastructure/CloudBurger.Infrastructure.csproj", "src/CloudPizza.Infrastructure/"]
RUN dotnet restore "src/CloudPizza.Api/CloudBurger.Api.csproj"
COPY . .
WORKDIR "/src/src/CloudPizza.Api"
RUN dotnet build "CloudBurger.Api.csproj" -c Release -o /app/build
RUN dotnet publish "CloudBurger.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CloudBurger.Api.dll"]
```

#### 4. Deploy
```bash
eb create cloudburger-api-env
eb deploy
```

### AWS RDS for PostgreSQL
```bash
aws rds create-db-instance \
  --db-instance-identifier cloudburger-db \
  --db-instance-class db.t3.micro \
  --engine postgres \
  --engine-version 17 \
  --master-username cloudburger \
  --master-user-password <YourPassword> \
  --allocated-storage 20
```

---

## Docker

### Docker Compose (Full Stack)

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:17
    environment:
      POSTGRES_USER: cloudburger
      POSTGRES_PASSWORD: cloudburger
      POSTGRES_DB: cloudburger
    ports:
      - "5432:5432"
    volumes:
      - postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U cloudburger"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: .
      dockerfile: src/CloudPizza.Api/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__burgerdb=Host=postgres;Database=cloudburger;Username=cloudburger;Password=cloudburger
    ports:
      - "5000:8080"
    depends_on:
      postgres:
        condition: service_healthy

  web:
    build:
      context: .
      dockerfile: src/CloudPizza.Web/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - services__api__http__0=http://api:8080
    ports:
      - "5001:8080"
    depends_on:
      - api

volumes:
  postgres-data:
```

Run:
```bash
docker-compose up -d
```

---

## Kubernetes

### Using Aspire's K8s Manifest Generation

```bash
# Install Aspire Kubernetes tools
dotnet tool install -g aspirate

# Generate manifests
aspirate generate

# Deploy to cluster
kubectl apply -f ./aspire-manifest.yaml
```

### Manual Kubernetes Deployment

#### 1. Create Deployment Files

`k8s/postgres.yaml`:
```yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
spec:
  serviceName: postgres
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
      - name: postgres
        image: postgres:17
        env:
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: postgres-secret
              key: password
        ports:
        - containerPort: 5432
        volumeMounts:
        - name: postgres-data
          mountPath: /var/lib/postgresql/data
  volumeClaimTemplates:
  - metadata:
      name: postgres-data
    spec:
      accessModes: [ "ReadWriteOnce" ]
      resources:
        requests:
          storage: 10Gi
---
apiVersion: v1
kind: Service
metadata:
  name: postgres
spec:
  selector:
    app: postgres
  ports:
  - port: 5432
```

`k8s/api.yaml`:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: cloudburger-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: cloudburger-api
  template:
    metadata:
      labels:
        app: cloudburger-api
    spec:
      containers:
      - name: api
        image: your-registry/cloudburger-api:latest
        env:
        - name: ConnectionStrings__burgerdb
          valueFrom:
            secretKeyRef:
              name: postgres-secret
              key: connection-string
        ports:
        - containerPort: 8080
---
apiVersion: v1
kind: Service
metadata:
  name: cloudburger-api
spec:
  type: LoadBalancer
  selector:
    app: cloudburger-api
  ports:
  - port: 80
    targetPort: 8080
```

#### 2. Deploy
```bash
kubectl apply -f k8s/
```

---

## Cloudflare

### Cloudflare Pages (Blazor WebAssembly) + Workers (API)

Note: Current implementation uses Blazor Server. For Cloudflare, you'd need to:
1. Convert Blazor to WebAssembly OR
2. Deploy Blazor Server to a container platform
3. Deploy API as Cloudflare Worker

This is a more advanced scenario. Consider Azure/AWS for easier Blazor Server deployment.

---

## Environment Variables

### Required for API
```bash
ConnectionStrings__burgerdb="Host=...;Database=...;Username=...;Password=..."
ASPNETCORE_ENVIRONMENT="Production"
```

### Required for Web
```bash
services__api__http__0="https://your-api-url"
ASPNETCORE_ENVIRONMENT="Production"
```

---

## Post-Deployment Checklist

- [ ] Database migrations applied
- [ ] PostgreSQL trigger created
- [ ] Connection strings configured
- [ ] CORS configured for production URLs
- [ ] Health checks responding
- [ ] Logging configured (Application Insights, CloudWatch, etc.)
- [ ] SSL/TLS certificates configured
- [ ] Environment variables secured (Key Vault, Secrets Manager, etc.)
- [ ] Monitoring and alerts set up
- [ ] Backup strategy in place

---

## Production Considerations

### Security
- Use managed secrets (Azure Key Vault, AWS Secrets Manager)
- Enable HTTPS only
- Configure CORS restrictively
- Use connection string encryption
- Enable authentication/authorization if needed

### Performance
- Enable response caching
- Configure compression
- Use CDN for static assets
- Scale horizontally with multiple instances
- Optimize database indexes

### Monitoring
- Set up Application Insights / CloudWatch
- Configure alerts for errors and performance
- Monitor SSE connection health
- Track database performance

### Backup
- Configure automated database backups
- Test restore procedures
- Document recovery time objectives (RTO)

---

## Need Help?

- **Azure**: https://learn.microsoft.com/azure/
- **AWS**: https://docs.aws.amazon.com/
- **Docker**: https://docs.docker.com/
- **Kubernetes**: https://kubernetes.io/docs/
- **Aspire Deployment**: https://learn.microsoft.com/dotnet/aspire/deployment/
