# HelpDeskSystem - How to Run Guide

A comprehensive, enterprise-grade help desk ticketing system with ITSM, marketing, scaling, and enterprise modules.

## Prerequisites

- **.NET 8.0 SDK** or later
- **SQL Server** (LocalDB, Express, or full instance)
- **Redis** (optional, for distributed caching)
- **Node.js 18+** (for frontend, if applicable)

## Quick Start

### 1. Clone and Restore

```bash
git clone <repository-url>
cd helpdesk-ticketing-system
dotnet restore HelpDeskSystem.slnx
```

### 2. Database Setup

#### Option A: Entity Framework Migrations

```bash
# Navigate to API project
cd HelpDeskSystem.API

# Create initial migration (if not exists)
dotnet ef migrations add InitialCreate --project ../HelpDeskSystem.Infrastructure

# Update database
dotnet ef database update --project ../HelpDeskSystem.Infrastructure
```

#### Option B: SQL Scripts

```bash
# Run the database creation script (if available)
sqlcmd -S localhost -d master -i "scripts/CreateDatabase.sql"
```

### 3. Configure Connection Strings

Update `HelpDeskSystem.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=HelpDeskSystem;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "JWT": {
    "Secret": "your-256-bit-secret-key-here-change-in-production",
    "Issuer": "HelpDeskSystem",
    "Audience": "HelpDeskUsers"
  }
}
```

### 4. Run the Application

#### Run API Only

```bash
cd HelpDeskSystem.API
dotnet run
```

API will be available at:
- **HTTP**: http://localhost:5000
- **HTTPS**: https://localhost:5001
- **Swagger UI**: https://localhost:5001/swagger

#### Run All Projects

```bash
dotnet run --project HelpDeskSystem.API
dotnet run --project HelpDeskSystem.WorkerService  # If background jobs needed
```

## Docker Deployment

### Build and Run with Docker

```bash
# Build the image
docker build -t helpdesk-system -f HelpDeskSystem.API/Dockerfile .

# Run with docker-compose
docker-compose up -d
```

### Docker Compose Services

- **API**: http://localhost:8080
- **SQL Server**: localhost:1433
- **Redis**: localhost:6379
- **Seq** (logging): http://localhost:5341

## Running Tests

### All Tests

```bash
dotnet test HelpDeskSystem.slnx
```

### Specific Test Projects

```bash
# Unit tests
dotnet test HelpDeskSystem.Tests/HelpDeskSystem.Tests.csproj

# Integration tests
dotnet test HelpDeskSystem.IntegrationTests/HelpDeskSystem.IntegrationTests.csproj
```

### With Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Feature-Specific Setup

### Marketing Module

Configure HubSpot integration in `appsettings.json`:

```json
{
  "HubSpot": {
    "ApiKey": "your-hubspot-api-key",
    "BaseUrl": "https://api.hubapi.com"
  }
}
```

### Scaling Module

Enable auto-scaling (requires Azure/AWS):

```json
{
  "AutoScaling": {
    "Enabled": true,
    "MinInstances": 2,
    "MaxInstances": 10,
    "TargetCpuPercent": 70
  }
}
```

### Enterprise Integrations

Configure Salesforce/HubSpot sync:

```json
{
  "Salesforce": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "Username": "admin@company.com",
    "Password": "secure-password"
  }
}
```

## Production Deployment

### 1. Publish

```bash
dotnet publish HelpDeskSystem.API -c Release -o ./publish
```

### 2. Environment Variables

Set production secrets:

```bash
# Windows
setx ASPNETCORE_ENVIRONMENT "Production"
setx ConnectionStrings__DefaultConnection "Server=prod-server;Database=HelpDeskSystem;User Id=sa;Password=secure-pass;"

# Linux/macOS
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Server=prod-server;Database=HelpDeskSystem;User Id=sa;Password=secure-pass;"
```

### 3. IIS Deployment

1. Install ASP.NET Core Hosting Bundle
2. Create IIS site pointing to publish folder
3. Configure app pool for .NET 8
4. Set environment variables in web.config

### 4. Linux Deployment (systemd)

```bash
# Create service file
sudo nano /etc/systemd/system/helpdesk.service
```

```ini
[Unit]
Description=HelpDeskSystem API
After=network.target

[Service]
WorkingDirectory=/var/www/helpdesk
ExecStart=/usr/bin/dotnet /var/www/helpdesk/HelpDeskSystem.API.dll
Restart=always
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

```bash
# Start service
sudo systemctl enable helpdesk
sudo systemctl start helpdesk
```

## Health Checks

Monitor system health:

```bash
# API Health
curl https://localhost:5001/health

# Database Health
curl https://localhost:5001/health/db

# Redis Health
curl https://localhost:5001/health/redis
```

## Background Jobs (Hangfire)

Access job dashboard:
- **Dashboard**: https://localhost:5001/hangfire

Jobs include:
- Ticket SLA monitoring
- Marketing campaign automation
- Data synchronization with external systems
- Performance metrics collection

## Troubleshooting

### Database Connection Issues

```bash
# Test SQL Server connection
sqlcmd -S localhost -Q "SELECT @@VERSION"

# Check connection string
# Ensure SQL Server Browser service is running
# Verify firewall rules for port 1433
```

### Redis Connection Issues

```bash
# Test Redis
redis-cli ping

# Check if Redis is running
sudo systemctl status redis
```

### Build Errors

```bash
# Clean and rebuild
dotnet clean HelpDeskSystem.slnx
dotnet restore HelpDeskSystem.slnx --force
dotnet build HelpDeskSystem.slnx

# Clear NuGet cache
dotnet nuget locals all --clear
```

### Port Conflicts

If ports 5000/5001 are in use:

```bash
dotnet run --urls "http://localhost:5002;https://localhost:5003"
```

## API Documentation

Once running, access:

- **Swagger UI**: http://localhost:5000/swagger
- **ReDoc**: http://localhost:5000/api-docs
- **OpenAPI JSON**: http://localhost:5000/swagger/v1/swagger.json

### Key Endpoints

| Module | Base Path | Description |
|--------|-----------|-------------|
| Tickets | /api/tickets | Core ticketing system |
| Incidents | /api/incidents | ITSM incident management |
| Problems | /api/problems | Problem management |
| Changes | /api/changes | Change management |
| CMDB | /api/cmdb | Configuration items |
| SLA | /api/sla | Service level agreements |
| Marketing | /api/marketing | Campaigns & leads |
| Reports | /api/reports | Analytics & reporting |

## Support

For issues or questions:
1. Check logs in `/logs` folder or Seq dashboard
2. Review error details in Swagger responses
3. Enable detailed errors in Development environment

---

**Built with .NET 8, Entity Framework Core, and enterprise-grade architecture.**
