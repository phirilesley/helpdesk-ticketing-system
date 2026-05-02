using System.Text;
using HelpDeskSystem.API.Jobs;
using HelpDeskSystem.API.Security;
using HelpDeskSystem.API.Services;
using HelpDeskSystem.API.Setup;
using HelpDeskSystem.Application.Configuration;
using HelpDeskSystem.Application.Interfaces;
using HelpDeskSystem.Application.Services;
using HelpDeskSystem.Persistence.Context;
using HelpDeskSystem.SLA.Jobs;
using HelpDeskSystem.SLA.Services;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "HelpDeskSystem API", Version = "v1" });

    var bearerScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT bearer token",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    options.AddSecurityDefinition("Bearer", bearerScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { bearerScheme, Array.Empty<string>() }
    });
});

// Add DbContext
builder.Services.AddDbContext<HelpDeskDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.Key))
{
    throw new InvalidOperationException("JWT configuration is missing. Set Jwt:Key in appsettings.");
}

builder.Services.AddSingleton(jwtOptions);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpClient();

var notificationChannels = builder.Configuration.GetSection(NotificationChannelOptions.SectionName).Get<NotificationChannelOptions>()
    ?? new NotificationChannelOptions();
builder.Services.AddSingleton(notificationChannels);
var auditRetentionOptions = builder.Configuration.GetSection(AuditRetentionOptions.SectionName).Get<AuditRetentionOptions>()
    ?? new AuditRetentionOptions();
builder.Services.AddSingleton(auditRetentionOptions);
var inboundEmailOptions = builder.Configuration.GetSection(InboundEmailOptions.SectionName).Get<InboundEmailOptions>()
    ?? new InboundEmailOptions();
builder.Services.AddSingleton(inboundEmailOptions);

// Add application services
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<ITicketMessageService, TicketMessageService>();
builder.Services.AddScoped<IMfaService, MfaService>();
builder.Services.AddScoped<ITenantSecurityPolicyService, TenantSecurityPolicyService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAutomationRuleService, AutomationRuleService>();
builder.Services.AddScoped<ISlaService, SlaService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuditRetentionService, AuditRetentionService>();
builder.Services.AddScoped<IBusinessTimeService, BusinessTimeService>();
builder.Services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
builder.Services.AddScoped<IEmailIngestionService, EmailIngestionService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<CheckSlaBreachesJob>();
builder.Services.AddScoped<PurgeAuditLogsJob>();

// Add enterprise services
builder.Services.AddScoped<IExternalAuthService, ExternalAuthService>();
builder.Services.AddWebhookServices();

builder.Services.AddHangfire(config =>
{
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new SqlServerStorageOptions
            {
                PrepareSchemaIfNecessary = true
            });
});
builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await SeedDataInitializer.InitializeAsync(app.Services, app.Configuration);

var slaJobOptions = app.Configuration.GetSection(SlaJobOptions.SectionName).Get<SlaJobOptions>() ?? new SlaJobOptions();
var auditRetentionJobOptions = app.Configuration.GetSection(AuditRetentionOptions.SectionName).Get<AuditRetentionOptions>() ?? new AuditRetentionOptions();
if (slaJobOptions.Enabled)
{
    var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<CheckSlaBreachesJob>(
        "sla-breach-check",
        job => job.ExecuteAsync(),
        slaJobOptions.Cron);
}

if (auditRetentionJobOptions.Enabled)
{
    var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
    recurringJobs.AddOrUpdate<PurgeAuditLogsJob>(
        "audit-retention-purge",
        job => job.ExecuteAsync(CancellationToken.None),
        auditRetentionJobOptions.Cron);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
});
app.MapControllers();

app.Run();
