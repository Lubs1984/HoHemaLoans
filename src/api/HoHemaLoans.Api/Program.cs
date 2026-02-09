using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using HoHemaLoans.Api.Services;

// Helper function to convert PostgreSQL URI to connection string
string ConvertPostgresUriToConnectionString(string uri)
{
    try
    {
        Console.WriteLine($"[DEBUG] Raw DATABASE_URL length: {uri?.Length ?? 0}");
        
        if (string.IsNullOrEmpty(uri))
        {
            throw new ArgumentException("DATABASE_URL is empty or null");
        }

        if (!uri.StartsWith("postgresql://"))
        {
            throw new ArgumentException($"DATABASE_URL must start with 'postgresql://' but starts with: {uri.Substring(0, Math.Min(20, uri.Length))}");
        }

        // Remove postgresql:// prefix
        var withoutProtocol = uri.Replace("postgresql://", "");
        
        // Split by @ to separate credentials from host
        var parts = withoutProtocol.Split('@');
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid format - expected 1 @ separator, found {parts.Length - 1}");
        }

        var credentials = parts[0];
        var hostAndDb = parts[1];

        // Parse credentials - split only on FIRST colon
        var colonIndex = credentials.IndexOf(':');
        if (colonIndex < 0)
        {
            throw new ArgumentException("Invalid credentials format - missing password");
        }

        var username = System.Net.WebUtility.UrlDecode(credentials.Substring(0, colonIndex));
        var password = System.Net.WebUtility.UrlDecode(credentials.Substring(colonIndex + 1));

        // Parse host and database
        var slashIndex = hostAndDb.IndexOf('/');
        if (slashIndex < 0)
        {
            throw new ArgumentException("Invalid DATABASE_URL format - missing database");
        }

        var hostPort = hostAndDb.Substring(0, slashIndex);
        var database = hostAndDb.Substring(slashIndex + 1).Split('?')[0]; // Remove query params

        var colonIndexHost = hostPort.LastIndexOf(':');
        string host;
        int port;
        
        if (colonIndexHost > 0)
        {
            host = hostPort.Substring(0, colonIndexHost);
            if (!int.TryParse(hostPort.Substring(colonIndexHost + 1), out port))
            {
                port = 5432;
            }
        }
        else
        {
            host = hostPort;
            port = 5432;
        }

        Console.WriteLine($"[DEBUG] Parsed values:");
        Console.WriteLine($"[DEBUG]   Host: {host}");
        Console.WriteLine($"[DEBUG]   Port: {port}");
        Console.WriteLine($"[DEBUG]   Username: {username}");
        Console.WriteLine($"[DEBUG]   Database: {database}");
        Console.WriteLine($"[DEBUG]   Password length: {password.Length}");

        // Build connection string manually (safer than builder)
        var connString = $"Host={host};Port={port};Username={username};Password={password};Database={database};SslMode=Require;TrustServerCertificate=true;";
        
        Console.WriteLine($"[DEBUG] ✓ Successfully created connection string");
        return connString;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to parse DATABASE_URL: {ex.Message}");
        Console.WriteLine($"[ERROR] Stack: {ex.StackTrace}");
        throw;
    }
}

var builder = WebApplication.CreateBuilder(args);

// Ensure PORT is set for Railway
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
    Console.WriteLine($"[STARTUP] Binding to Railway PORT: {port}");
}
else
{
    Console.WriteLine($"[STARTUP] No PORT environment variable, using default Kestrel configuration");
}

// Add services to the container.
builder.Services.AddControllers();

// Get connection string - Railway uses DATABASE_URL env var
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrEmpty(databaseUrl))
{
    Console.WriteLine("[DEBUG] Found DATABASE_URL environment variable, converting to connection string...");
    // Convert PostgreSQL URI to connection string
    // Format: postgresql://user:password@host:port/database
    connectionString = ConvertPostgresUriToConnectionString(databaseUrl);
}
else
{
    // Try configuration (for local development)
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    
    if (string.IsNullOrEmpty(connectionString) || connectionString.StartsWith("Data Source="))
    {
        // Fallback for local development with PostgreSQL
        Console.WriteLine("[DEBUG] Using local development PostgreSQL connection string");
        connectionString = "Host=localhost;Database=hohema_loans;Username=hohema_user;Password=hohema_password_2024!;Port=5432";
    }
}

Console.WriteLine($"[DEBUG] Final connection string: Host=***, Database=***, Username=***");

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireDigit = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "HoHemaLoans",
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"] ?? "HoHemaLoans",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var corsOrigins = builder.Configuration["CORS_ORIGINS"] ?? Environment.GetEnvironmentVariable("CORS_ORIGINS");
        var frontendUrl = builder.Configuration["FRONTEND_URL"] ?? Environment.GetEnvironmentVariable("FRONTEND_URL");
        var environment = builder.Configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        
        Console.WriteLine($"[DEBUG] Environment: {environment}");
        Console.WriteLine($"[DEBUG] CORS_ORIGINS from config/env: {corsOrigins}");
        Console.WriteLine($"[DEBUG] FRONTEND_URL from config/env: {frontendUrl}");
        
        // Build list of allowed origins
        var allowedOrigins = new List<string>();
        
        // Always allow localhost for development
        if (environment == "Development" || environment == "Staging")
        {
            allowedOrigins.AddRange(new[]
            {
                "http://localhost:5173",
                "http://localhost:5174",
                "http://localhost:3000",
                "http://localhost:4200"
            });
        }
        
        // ALWAYS add Railway development URLs (hardcoded for reliability)
        allowedOrigins.Add("https://hohemaweb-development.up.railway.app");
        
        // Parse CORS_ORIGINS (comma-separated list)
        if (!string.IsNullOrEmpty(corsOrigins))
        {
            var origins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var origin in origins)
            {
                if (!string.IsNullOrEmpty(origin) && !allowedOrigins.Contains(origin))
                {
                    Console.WriteLine($"[DEBUG] Adding CORS origin from config: {origin}");
                    allowedOrigins.Add(origin);
                }
            }
        }
        
        // Add FRONTEND_URL if configured
        if (!string.IsNullOrEmpty(frontendUrl) && !allowedOrigins.Contains(frontendUrl))
        {
            Console.WriteLine($"[DEBUG] Adding FRONTEND_URL: {frontendUrl}");
            allowedOrigins.Add(frontendUrl);
        }
        
        Console.WriteLine($"[DEBUG] ==========================================");
        Console.WriteLine($"[DEBUG] CORS Configuration:");
        Console.WriteLine($"[DEBUG] Total allowed origins: {allowedOrigins.Count}");
        foreach (var origin in allowedOrigins)
        {
            Console.WriteLine($"[DEBUG]   - {origin}");
        }
        Console.WriteLine($"[DEBUG] ==========================================");
        
        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Add Affordability Service
builder.Services.AddScoped<IAffordabilityService, AffordabilityService>();

// Add Omnichannel Loan Service
builder.Services.AddScoped<IOmnichannelLoanService, OmnichannelLoanService>();

// Add Document Storage and Verification Services
builder.Services.AddScoped<IDocumentStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IProfileVerificationService, ProfileVerificationService>();

// Add Test Data Seeder (for development)
builder.Services.AddScoped<TestDataSeeder>();

// Add WhatsApp Service
builder.Services.Configure<WhatsAppSettings>(builder.Configuration.GetSection("WhatsApp"));
builder.Services.AddHttpClient<IWhatsAppService, WhatsAppService>();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "HoHema Loans API", Version = "v1" });
    
    // Add JWT Bearer token support to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Log startup information
var actualPort = Environment.GetEnvironmentVariable("PORT") ?? "8080";
Console.WriteLine($"[STARTUP] ==========================================");
Console.WriteLine($"[STARTUP] HoHema Loans API Starting...");
Console.WriteLine($"[STARTUP] Environment: {app.Environment.EnvironmentName}");
Console.WriteLine($"[STARTUP] PORT: {actualPort}");
Console.WriteLine($"[STARTUP] Binding: http://0.0.0.0:{actualPort}");
Console.WriteLine($"[STARTUP] ==========================================");

// Run database migrations in background (don't block startup)
_ = Task.Run(async () =>
{
    await Task.Delay(1000); // Let the app start listening first
    Console.WriteLine("[STARTUP] Applying database migrations in background...");
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            // Manually create SystemSettings and UserDocuments tables FIRST (before migrations)
            // This ensures they exist even if migrations fail on existing tables
            logger.LogInformation("[STARTUP] Ensuring required tables exist...");
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    CREATE TABLE IF NOT EXISTS ""SystemSettings"" (
                        ""Id"" integer PRIMARY KEY GENERATED BY DEFAULT AS IDENTITY,
                        ""InterestRatePercentage"" numeric(5,2) NOT NULL,
                        ""AdminFee"" numeric(10,2) NOT NULL,
                        ""MaxLoanPercentage"" numeric(5,2) NOT NULL,
                        ""MinLoanAmount"" numeric(10,2) NOT NULL,
                        ""MaxLoanAmount"" numeric(10,2) NOT NULL,
                        ""LastModifiedDate"" timestamp with time zone NOT NULL,
                        ""LastModifiedBy"" text
                    );
                    
                    CREATE TABLE IF NOT EXISTS ""UserDocuments"" (
                        ""Id"" uuid NOT NULL PRIMARY KEY,
                        ""UserId"" text NOT NULL,
                        ""DocumentType"" integer NOT NULL,
                        ""FileName"" character varying(255) NOT NULL,
                        ""FilePath"" character varying(500) NOT NULL,
                        ""FileSize"" bigint NOT NULL,
                        ""ContentType"" character varying(100) NOT NULL,
                        ""FileContentBase64"" text NULL,
                        ""Status"" integer NOT NULL DEFAULT 0,
                        ""UploadedAt"" timestamp with time zone NOT NULL,
                        ""VerifiedAt"" timestamp with time zone NULL,
                        ""VerifiedByUserId"" text NULL,
                        ""RejectionReason"" character varying(500) NULL,
                        ""Notes"" text NULL,
                        ""IsDeleted"" boolean NOT NULL DEFAULT false,
                        CONSTRAINT ""FK_UserDocuments_AspNetUsers_UserId"" FOREIGN KEY (""UserId"") 
                            REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE,
                        CONSTRAINT ""FK_UserDocuments_AspNetUsers_VerifiedByUserId"" FOREIGN KEY (""VerifiedByUserId"") 
                            REFERENCES ""AspNetUsers"" (""Id"") ON DELETE SET NULL
                    );
                    
                    CREATE INDEX IF NOT EXISTS ""IX_UserDocuments_UserId"" ON ""UserDocuments"" (""UserId"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserDocuments_Status"" ON ""UserDocuments"" (""Status"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserDocuments_DocumentType"" ON ""UserDocuments"" (""DocumentType"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserDocuments_UploadedAt"" ON ""UserDocuments"" (""UploadedAt"");
                    CREATE INDEX IF NOT EXISTS ""IX_UserDocuments_VerifiedByUserId"" ON ""UserDocuments"" (""VerifiedByUserId"");
                ");
                logger.LogInformation("[STARTUP] Required tables ensured");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[STARTUP] Failed to create tables: {Message}", ex.Message);
            }
            
            // Now run migrations (might fail on existing tables, but that's ok)
            logger.LogInformation("[STARTUP] Running database migrations...");
            try
            {
                context.Database.Migrate();
                logger.LogInformation("[STARTUP] Database migrations completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[STARTUP] Migrations failed (expected if tables exist): {Message}", ex.Message);
            }
            
            // Seed SystemSettings if it doesn't exist
            logger.LogInformation("[STARTUP] Checking SystemSettings...");
            if (!await context.SystemSettings.AnyAsync())
            {
                logger.LogInformation("[STARTUP] Creating default SystemSettings...");
                var defaultSettings = new SystemSettings
                {
                    InterestRatePercentage = 5.00m,  // 5%
                    AdminFee = 50.00m,                // R50
                    MaxLoanPercentage = 50.00m,       // 50% of monthly earnings
                    MinLoanAmount = 100.00m,          // R100
                    MaxLoanAmount = 10000.00m,        // R10,000
                    LastModifiedDate = DateTime.UtcNow,
                    LastModifiedBy = "system"
                };
                context.SystemSettings.Add(defaultSettings);
                await context.SaveChangesAsync();
                logger.LogInformation("[STARTUP] Default SystemSettings created successfully");
            }
            else
            {
                logger.LogInformation("[STARTUP] SystemSettings already exist");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to apply database migrations: {ex.Message}");
        Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
    }
});

// Add global exception handling middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Unhandled exception: {ex}");
        Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
        
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        
        await context.Response.WriteAsJsonAsync(new 
        { 
            error = ex.Message,
            details = app.Environment.IsDevelopment() ? ex.StackTrace : null
        });
    }
});

// Configure the HTTP request pipeline.

// CORS must be the VERY FIRST middleware (before swagger, before anything)
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Skip HTTPS redirection on Railway (Railway handles SSL)
if (!builder.Configuration.GetValue<bool>("Railway:SkipHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// Add health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapControllers();

// Run database seeding in background after app starts
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2000); // Wait for app to be fully ready
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            try
            {
                logger.LogInformation("[STARTUP] Seeding test users...");
                await DbInitializer.InitializeAsync(app);
                logger.LogInformation("[STARTUP] Database seeding completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[STARTUP] An error occurred while seeding database.");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Background database seeding failed: {ex}");
    }
});

Console.WriteLine($"[STARTUP] ✅ Application started successfully!");
Console.WriteLine($"[STARTUP] 🚀 Listening on: http://0.0.0.0:{actualPort}");
Console.WriteLine($"[STARTUP] 💚 Health check: http://0.0.0.0:{actualPort}/health");

app.Run();
