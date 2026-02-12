using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Hangfire;
using Hangfire.Dashboard;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Serilog;
using SmartTaskManagement.API.Filters;
using SmartTaskManagement.API.HealthChecks;
using SmartTaskManagement.API.Middleware;
using SmartTaskManagement.Infrastructure.BackgroundJobs;

namespace SmartTaskManagement.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add API versioning
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("X-Api-Version"),
                new QueryStringApiVersionReader("api-version"));
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        // Add controllers with JSON options
        services.AddControllers(options =>
        {
            options.Filters.Add<ModelValidationFilter>();
            options.Filters.Add<AuditLogActionFilter>();
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });

        // Add CORS
        services.AddCors(options =>
        {
            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
                ?? new[] { "http://localhost:3000", "http://localhost:4200" };

            options.AddPolicy("CorsPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .WithExposedHeaders("X-Tenant-Id", "X-Pagination", "X-Total-Count");
            });
        });

        // Add Swagger/OpenAPI
        services.AddSwaggerGen(options =>
        {
            // Define the Swagger document for v1
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1.0",
                Title = configuration["ApiSettings:Title"] ?? "Smart Task Management API",
                Description = configuration["ApiSettings:Description"] ?? "Enterprise-grade Task Management API with Clean Architecture",
                Contact = new OpenApiContact
                {
                    Name = configuration["ApiSettings:ContactName"] ?? "API Support",
                    Email = configuration["ApiSettings:ContactEmail"] ?? "support@smarttaskmanagement.com",
                    Url = new Uri("https://smarttaskmanagement.com/support")
                },
                License = new OpenApiLicense
                {
                    Name = configuration["ApiSettings:LicenseName"] ?? "MIT",
                    Url = new Uri(configuration["ApiSettings:LicenseUrl"] ?? "https://opensource.org/licenses/MIT")
                },
                TermsOfService = new Uri("https://smarttaskmanagement.com/terms")
            });

            // Add JWT Bearer authentication definition
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.\n\nExample: 'Bearer 12345abcdef'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            // Add security requirement – all endpoints that have [Authorize] will require the Bearer token
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        },
                        Scheme = "oauth2",
                        Name = "Bearer",
                        In = ParameterLocation.Header
                    },
                    new List<string>() // no scopes needed for API key
                }
            });

            // Use full type names to avoid conflicts
            options.CustomSchemaIds(type => type.FullName?.Replace("+", "_") ?? type.Name);

            // Include XML comments
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add operation filter for default values & descriptions
            options.OperationFilter<SwaggerDefaultValues>();

            // Enable annotations (e.g., [SwaggerOperation], [SwaggerResponse])
            options.EnableAnnotations();

            // Order actions by HTTP method for cleaner grouping
            options.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");
        });

        // Add health checks
        services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("DefaultConnection")!,
                name: "database",
                tags: new[] { "ready", "database" })
            .AddCheck<HangfireHealthCheck>("hangfire", tags: new[] { "ready", "background-jobs" });

        // Add rate limiting
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
                
                return RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: ip,
                    factory: partition => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue<int?>("RateLimiting:PermitLimit") ?? 100,
                        Window = TimeSpan.FromSeconds(configuration.GetValue<int?>("RateLimiting:Window") ?? 60),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = configuration.GetValue<int?>("RateLimiting:QueueLimit") ?? 10,
                        SegmentsPerWindow = 10
                    });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests",
                    message = "Please try again later",
                    retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter) 
                        ? retryAfter.TotalSeconds 
                        : 60
                }, token);
            };
        });

        // Register filters
        services.AddScoped<ModelValidationFilter>();
        services.AddScoped<AuditLogActionFilter>();

        return services;
    }

    public static WebApplication UseApiServices(this WebApplication app)
    {
        var environment = app.Environment;

        // Configure the HTTP request pipeline
        if (environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Smart Task Management API v1");
                options.RoutePrefix = "swagger";
                options.DisplayOperationId();
                options.DisplayRequestDuration();
                options.EnableDeepLinking();
                options.EnableFilter();
                options.ShowExtensions();
                options.EnableValidator();
            });

            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        // Global exception handling
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        // Request/Response logging
        app.UseMiddleware<RequestResponseLoggingMiddleware>();

        // CORS
        app.UseCors("CorsPolicy");

        // Rate limiting
        app.UseRateLimiter();

        // Authentication & Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Tenant validation
        app.UseMiddleware<TenantMiddleware>();

        // Hangfire dashboard (admin only)
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { app.Services.GetRequiredService<HangfireDashboardAuthorizationFilter>() },
            DashboardTitle = "Smart Task Management - Background Jobs",
            DisplayStorageConnectionString = false,
            IsReadOnlyFunc = context =>
            {
                var httpContext = context.GetHttpContext();
                return !httpContext.User.IsInRole("Admin");
            }
        });

        // Health checks
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });

        // Map controllers
        app.MapControllers();

        // Welcome page
        app.MapGet("/", () => Results.Redirect("/swagger"));

        return app;
    }
}