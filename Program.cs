using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using Shortly.Application.Helpers;
using Shortly.Application.Interfaces;
using Shortly.Application.Services;
using Shortly.Endpoints;
using Shortly.Infrastructure;
using Shortly.Infrastructure.Persistence;
using Shortly.Infrastructure.Repositories;

// Creates the ASP.NET Core application builder with initial configuration
var builder = WebApplication.CreateBuilder(args);

// Configures Serilog as the global bootstrap logger, reading all settings from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

// Tells the host to use Serilog as its logging system
builder.Host.UseSerilog((context, services, config) =>
{
    config
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProcessId()
        .Enrich.WithThreadId();
});

// Registers Razor Pages services
builder.Services.AddRazorPages();

// Registers the OpenAPI document generator with version 3.1 and API metadata
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Shortly API",
            Description = "A URL shortener service with user authentication and link management.",
            Version = "v1"
        };
        return Task.CompletedTask;
    });
});

// Gets a list of strings from appsetting to obtain allowed origins into a variable
var allowedOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

//Register Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("RestrictedCors", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .WithMethods(
                "GET",
                "POST",
                "DELETE")
            .WithHeaders(
                "Content-Type",
                "Accept",
                "Authorization")
            .WithExposedHeaders(
                "ETag",
                "Last-Modified",
                "X-Response-Time")
            .SetPreflightMaxAge(
                TimeSpan.FromHours(1));
    });
});

// Enables Swagger support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Enables and configures cookies
builder.Services.AddAuthentication("Cookies")
    .AddCookie(options =>
    {
        options.Cookie.Name = "__Host-auth";
        options.Cookie.Path = "/";

        options.Cookie.HttpOnly = true;

        options.Cookie.SecurePolicy =
            CookieSecurePolicy.Always;

        options.Cookie.SameSite =
            SameSiteMode.Strict;

        options.SlidingExpiration = true;
    });
builder.Services.Configure<CookiePolicyOptions>(
    options =>
    {
        options.MinimumSameSitePolicy =
            SameSiteMode.Strict;

        options.HttpOnly =
            HttpOnlyPolicy.Always;

        options.Secure =
            CookieSecurePolicy.Always;
    });

// Adds and configures response caching
builder.Services.AddResponseCaching();

// Adds rate limiter
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("api", config =>
    {
        config.PermitLimit = 100;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueLimit = 0;
    });

    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;
});

// Registers and configures compression services brotli and gzip
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;

    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();

    options.MimeTypes =
        ResponseCompressionDefaults.MimeTypes.Concat(
        [
            "application/json"
        ]);
});
builder.Services.Configure<BrotliCompressionProviderOptions>(
    options =>
    {
        options.Level = CompressionLevel.Fastest;
    });

builder.Services.Configure<GzipCompressionProviderOptions>(
    options =>
    {
        options.Level = CompressionLevel.Fastest;
    });

// Registers the SQLite database context using Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AppDbContext")));

// Configures a volatile server-side ticket store (auth state lost on restart)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<MemoryCacheTicketStore>();

// Configures cookie authentication with a server-side ticket store
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Error";
    });

// Injects the ticket store into the cookie options after the service provider is built
builder.Services.AddSingleton<IConfigureOptions<CookieAuthenticationOptions>>(sp =>
{
    var store = sp.GetRequiredService<MemoryCacheTicketStore>();
    return new ConfigureNamedOptions<CookieAuthenticationOptions>(
        CookieAuthenticationDefaults.AuthenticationScheme,
        options => options.SessionStore = store);
});

// Register Health Checks
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>(
        name: "sqlite");

// Registers the authorization service
builder.Services.AddAuthorization();

// Registers repositories and services for dependency injection (scoped lifetime)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILinkRepository, LinkRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILinkService, LinkService>();

// Builds the application with all registered configurations
var app = builder.Build();

// In non-development environments, uses a friendly error page
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

// Implements Security header inline middleware
app.Use(async (context, next) =>
{
    //Strict-Transport-Security forces browsers to use HTTPS
    context.Response.Headers["Strict-Transport-Security"] =
        "max-age=31536000; includeSubDomains";
    //X-Content-Type-Options prevents MIME type sniffing attacks
    context.Response.Headers["X-Content-Type-Options"] =
        "nosniff";
    //X-Frame-Options prevents clickjacking by blocking iframes
    context.Response.Headers["X-Frame-Options"] =
        "DENY";
    //Referrer-Policy limits referrer information sent to other sites
    context.Response.Headers["Referrer-Policy"] =
        "strict-origin-when-cross-origin";
    //Permissions-Policy disables browser features
    context.Response.Headers["Permissions-Policy"] =
        "camera=(), microphone=(), geolocation=()";

    await next();
});

// Map health checks endpoint
app.MapHealthChecks(
    "/health",
    new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType =
                "application/json";

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(
                    new
                    {
                        status =
                            report.Status.ToString(),
                        checks =
                            report.Entries.Select(x =>
                                new
                                {
                                    name = x.Key,
                                    status =
                                        x.Value.Status
                                            .ToString(),
                                    duration =
                                        x.Value.Duration
                                            .TotalMilliseconds
                                })
                    }));
        }
    });

// robots.txt endpoint to disallow crawling of the shortener and avoid indexing of shortened URLs
app.MapGet("/robots.txt", () =>
{
    return Results.Text(
        """
        User-agent: *
        Disallow: /
        """,
        "text/plain");
});

// sitemaps.xml endpoint to avoid indexing of shortened URLs
app.MapGet("/sitemap.xml", () =>
{
    const string sitemap = """
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
</urlset>
""";

    return Results.Text(sitemap, "application/xml");
});

// Enables CORS with restrictive policy. UseCors must be called before UseResponseCaching
app.UseCors("RestrictedCors");

// Enables response caching
app.UseResponseCaching();

// Enables rate limiter
app.UseRateLimiter();

// Enables cookie policy
app.UseCookiePolicy();

// Enforces that all cookies contains path=/
app.Use(async (context, next) =>
{
    await next();

    foreach (var cookie in context.Response.Headers.SetCookie)
    {
        if(cookie!=null)
        {
            if (!cookie.Contains("Path=/"))
            {
                throw new InvalidOperationException(
                    "All cookies must use Path=/");
            } 
        }
    }
});

// Register Request Tracing middleware
app.UseMiddleware<RequestTracingMiddleware>();

// Register performance middleware
app.UsePerformanceMeasurement();

// Register Serilog request logging
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext =
        (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set(
                "TraceId",
                Activity.Current?.TraceId.ToString());

            diagnosticContext.Set(
                "CorrelationId",
                httpContext.Response.Headers["X-Correlation-Id"].ToString());

            diagnosticContext.Set(
                "RequestHost",
                httpContext.Request.Host.ToString());

            diagnosticContext.Set(
                "Scheme",
                httpContext.Request.Scheme);
        };
});

// Redirects HTTP requests to HTTPS automatically
// app.UseHttpsRedirection();

// Enables response compression
app.UseResponseCompression();

// Serves static files from the wwwroot/ folder
app.UseStaticFiles();

// Enables request routing
app.UseRouting();

// Enables authentication (must come after UseRouting)
app.UseAuthentication();

// Enables authorization (must come after UseAuthentication)
app.UseAuthorization();

// Maps static assets with automatic versioning
app.MapStaticAssets();

// Maps Razor Pages with static asset support
app.MapRazorPages().WithStaticAssets();

// Exposes the OpenAPI document at /openapi/v1.json
app.MapOpenApi();

// Enables Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Serves the Scalar interactive API reference UI at /scalar/v1
app.MapScalarApiReference();

// Maps the redirect endpoint GET /{shortUrl} from Endpoints/UrlRedirectEndpoint.cs
app.MapUrlRedirect();

// Maps the LAB_3 endpoints from Endpoints/LAB3Endpoints.cs
app.MapLab3Endpoints();

// Creates a scope for scoped services (e.g. AppDbContext)
using (var scope = app.Services.CreateScope())
{
    // Gets the database context from the DI container
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Creates the database and tables if they do not exist
    db.Database.EnsureCreated();
    // Reads the admin password from configuration or uses a default value
    var seedPassword = app.Configuration["Seed:AdminPassword"] ?? "admin123";
    // Seeds initial data (admin user and sample links)
    await DbInitializer.InitializeAsync(db, seedPassword);
}

// Starts the application and begins listening for HTTP requests
await app.RunAsync();
