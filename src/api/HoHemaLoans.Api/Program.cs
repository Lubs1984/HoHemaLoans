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

// Add services to the container.
builder.Services.AddControllers();

// Get connection string - Railway uses DATABASE_URL env var
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    // Try to get from Railway's DATABASE_URL environment variable
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        Console.WriteLine("[DEBUG] Found DATABASE_URL environment variable, converting to connection string...");
        // Convert PostgreSQL URI to connection string
        // Format: postgresql://user:password@host:port/database
        connectionString = ConvertPostgresUriToConnectionString(databaseUrl);
    }
    else
    {
        Console.WriteLine("[DEBUG] No DATABASE_URL found, will use fallback");
    }
}
if (string.IsNullOrEmpty(connectionString))
{
    // Fallback for local development
    Console.WriteLine("[DEBUG] Using local development connection string");
    connectionString = "Host=localhost;Database=hohema_loans;Username=hohema_user;Password=hohema_password_2024!;Port=5432";
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
        var frontendUrl = builder.Configuration["FRONTEND_URL"];
        var envFrontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL");
        
        Console.WriteLine($"[DEBUG] FRONTEND_URL from config: {frontendUrl}");
        Console.WriteLine($"[DEBUG] FRONTEND_URL from env: {envFrontendUrl}");
        
        // Build list of allowed origins
        var allowedOrigins = new List<string>
        {
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:3000",
            "https://hohemaweb-development.up.railway.app"
        };
        
        // Add FRONTEND_URL if configured
        if (!string.IsNullOrEmpty(frontendUrl) && !allowedOrigins.Contains(frontendUrl))
        {
            allowedOrigins.Add(frontendUrl);
        }
        
        // Add from environment variable if set
        if (!string.IsNullOrEmpty(envFrontendUrl) && !allowedOrigins.Contains(envFrontendUrl))
        {
            allowedOrigins.Add(envFrontendUrl);
        }
        
        Console.WriteLine($"[DEBUG] CORS allowed origins: {string.Join(", ", allowedOrigins)}");
        
        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add Affordability Service
builder.Services.AddScoped<IAffordabilityService, AffordabilityService>();

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

// CRITICAL: Run database migrations BEFORE the app starts accepting requests
Console.WriteLine("[STARTUP] Initializing database schema...");
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("[STARTUP] Applying database migrations...");
        context.Database.Migrate();
        logger.LogInformation("[STARTUP] Database migrations applied successfully");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] Failed to apply database migrations: {ex}");
    Console.WriteLine($"[ERROR] Stack trace: {ex.StackTrace}");
    throw; // Don't start the app if migrations fail
}

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS must come before authentication
app.UseCors("AllowFrontend");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Run database seeding in background after app starts
_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(1000); // Wait 1 second for app to start
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            try
            {
                logger.LogInformation("[STARTUP] Seeding test users...");
                
                // Initialize database with test users if needed
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

app.Run();
