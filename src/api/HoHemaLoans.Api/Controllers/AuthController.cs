using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HoHemaLoans.Api.Models;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            IdNumber = model.IdNumber,
            DateOfBirth = model.DateOfBirth,
            Address = model.Address,
            MonthlyIncome = model.MonthlyIncome,
            PhoneNumber = model.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Assign User role by default
            await _userManager.AddToRoleAsync(user, "User");
            
            var token = await GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new AuthResponseDto
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IdNumber = user.IdNumber,
                    DateOfBirth = user.DateOfBirth,
                    Address = user.Address,
                    MonthlyIncome = user.MonthlyIncome,
                    PhoneNumber = user.PhoneNumber,
                    IsVerified = user.IsVerified,
                    Roles = roles
                }
            });
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.Code, error.Description);
        }

        return BadRequest(ModelState);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto model)
    {
        try
        {
            _logger.LogInformation($"[LOGIN] Attempting login for email: {model.Email}");
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning($"[LOGIN] Invalid model state for email: {model.Email}");
                return BadRequest(ModelState);
            }

            _logger.LogInformation($"[LOGIN] Looking up user: {model.Email}");
            var user = await _userManager.FindByEmailAsync(model.Email);
            
            if (user == null)
            {
                _logger.LogWarning($"[LOGIN] User not found: {model.Email}");
                return BadRequest("Invalid email or password");
            }

            _logger.LogInformation($"[LOGIN] User found, checking password for: {model.Email}");
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            
            if (result.Succeeded)
            {
                _logger.LogInformation($"[LOGIN] Password check succeeded for: {model.Email}");
                var token = await GenerateJwtToken(user);
                var roles = await _userManager.GetRolesAsync(user);
                
                _logger.LogInformation($"[LOGIN] Login successful for: {model.Email}");
                return Ok(new AuthResponseDto
                {
                    Token = token,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Email = user.Email!,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        IdNumber = user.IdNumber,
                        DateOfBirth = user.DateOfBirth,
                        Address = user.Address,
                        MonthlyIncome = user.MonthlyIncome,
                        PhoneNumber = user.PhoneNumber,
                        IsVerified = user.IsVerified,
                        Roles = roles
                    }
                });
            }

            _logger.LogWarning($"[LOGIN] Password check failed for: {model.Email}");
            return BadRequest("Invalid email or password");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[LOGIN] Exception during login for: {model?.Email}");
            throw;
        }
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!)
        };

        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["JwtSettings:Issuer"] ?? "HoHemaLoans",
            Audience = _configuration["JwtSettings:Audience"] ?? "HoHemaLoans"
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public decimal MonthlyIncome { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public decimal MonthlyIncome { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsVerified { get; set; }
    public IEnumerable<string>? Roles { get; set; }
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}