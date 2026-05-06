# Use the official ASP.NET Core runtime as a parent image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["HelpDeskSystem.API/HelpDeskSystem.API.csproj", "HelpDeskSystem.API/"]
COPY ["HelpDeskSystem.Application/HelpDeskSystem.Application.csproj", "HelpDeskSystem.Application/"]
COPY ["HelpDeskSystem.Domain/HelpDeskSystem.Domain.csproj", "HelpDeskSystem.Domain/"]
COPY ["HelpDeskSystem.Persistence/HelpDeskSystem.Persistence.csproj", "HelpDeskSystem.Persistence/"]
COPY ["HelpDeskSystem.Shared/HelpDeskSystem.Shared.csproj", "HelpDeskSystem.Shared/"]
COPY ["HelpDeskSystem.Integrations/HelpDeskSystem.Integrations.csproj", "HelpDeskSystem.Integrations/"]
COPY ["HelpDeskSystem.Auditing/HelpDeskSystem.Auditing.csproj", "HelpDeskSystem.Auditing/"]
COPY ["HelpDeskSystem.Files/HelpDeskSystem.Files.csproj", "HelpDeskSystem.Files/"]
COPY ["HelpDeskSystem.Notifications/HelpDeskSystem.Notifications.csproj", "HelpDeskSystem.Notifications/"]
COPY ["HelpDeskSystem.Realtime/HelpDeskSystem.Realtime.csproj", "HelpDeskSystem.Realtime/"]
COPY ["HelpDeskSystem.Reporting/HelpDeskSystem.Reporting.csproj", "HelpDeskSystem.Reporting/"]
COPY ["HelpDeskSystem.SLA/HelpDeskSystem.SLA.csproj", "HelpDeskSystem.SLA/"]
COPY ["HelpDeskSystem.Workflow/HelpDeskSystem.Workflow.csproj", "HelpDeskSystem.Workflow/"]
COPY ["HelpDeskSystem.Marketing/HelpDeskSystem.Marketing.csproj", "HelpDeskSystem.Marketing/"]
COPY ["HelpDeskSystem.Scaling/HelpDeskSystem.Scaling.csproj", "HelpDeskSystem.Scaling/"]
COPY ["HelpDeskSystem.Enterprise/HelpDeskSystem.Enterprise.csproj", "HelpDeskSystem.Enterprise/"]
COPY ["HelpDeskSystem.ITSM/HelpDeskSystem.ITSM.csproj", "HelpDeskSystem.ITSM/"]

RUN dotnet restore "HelpDeskSystem.API/HelpDeskSystem.API.csproj"

# Copy the rest of the application
COPY . .

# Build and publish the application
WORKDIR "/src/HelpDeskSystem.API"
RUN dotnet publish "HelpDeskSystem.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/logs /app/uploads
RUN chmod 777 /app/logs /app/uploads

# Create a non-root user
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "HelpDeskSystem.API.dll"]
