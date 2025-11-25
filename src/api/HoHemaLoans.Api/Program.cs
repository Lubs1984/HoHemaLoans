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
        Console.WriteLine($"[DEBUG] Parsing DATABASE_URL: {uri.Substring(0, Math.Min(50, uri.Length))}...");
        
        if (!uri.StartsWith("postgresql://"))
        {
            throw new ArgumentException("DATABASE_URL must start with 'postgresql://'");
        }

        // Remove postgresql:// prefix
        var withoutProtocol = uri.Replace("postgresql://", "");
        
        // Split by @ to separate credentials from host
        var parts = withoutProtocol.Split('@');
        if (parts.Length != 2)
        {
            throw new ArgumentException("Invalid DATABASE_URL format - missing @ separator");
        }

        var credentials = parts[0];
        var hostAndDb = parts[1];

        // Parse credentials
        var credParts = credentials.Split(':');
        if (credParts.Length < 2)
        {
            throw new ArgumentException("Invalid credentials format - missing password");
        }

        var username = System.Net.WebUtility.UrlDecode(credParts[0]);
        var password = System.Net.WebUtility.UrlDecode(string.Join(":", credParts.Skip(1)));

        // Parse host and database
        var hostDbParts = hostAndDb.Split('/');
        if (hostDbParts.Length < 2)
        {
            throw new ArgumentException("Invalid DATABASE_URL format - missing database");
        }

        var hostPort = hostDbParts[0];
        var database = hostDbParts[1].Split('?')[0]; // Remove query params

        var hostPortParts = hostPort.Split(':');
        var host = hostPortParts[0];
        var port = hostPortParts.Length > 1 ? hostPortParts[1] : "5432";

        // Build connection string with proper escaping
        var connString = $"Host={host};Port={port};Username={username};Password={password};Database={database};SslMode=Require;";
        
        Console.WriteLine($"[DEBUG] ✓ Successfully parsed DATABASE_URL");
        Console.WriteLine($"[DEBUG]   Host: {host}");
        Console.WriteLine($"[DEBUG]   Port: {port}");
        Console.WriteLine($"[DEBUG]   Database: {database}");
        Console.WriteLine($"[DEBUG]   Username: {username}");
        
        return connString;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] Failed to parse DATABASE_URL: {ex.Message}");
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
        var frontendUrl = builder.Configuration["FRONTEND_URL"] ?? "http://localhost:5173";
        policy.WithOrigins(frontendUrl, "http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Run database migrations and seed test users (only on first run or development)
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        logger.LogInformation("Setting up database...");
        
        // Apply migrations (safe for production - only creates schema if needed)
        context.Database.Migrate();
        
        logger.LogInformation("Database schema updated successfully");
        
        // Initialize database with test users if needed
        await DbInitializer.InitializeAsync(app);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while setting up database.");
        throw;
    }
}

app.Run();
