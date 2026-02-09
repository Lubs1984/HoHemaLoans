using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HoHemaLoans.Api.Models;
using HoHemaLoans.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly IWhatsAppService _whatsAppService;
    
    // In-memory store for PIN codes (use Redis or database in production)
    private static readonly Dictionary<string, (string Pin, DateTime Expiry)> _pinStore = new();

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        IWhatsAppService whatsAppService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _logger = logger;
        _whatsAppService = whatsAppService;
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
                    Roles = roles,
                    StreetAddress = user.StreetAddress,
                    Suburb = user.Suburb,
                    City = user.City,
                    Province = user.Province,
                    PostalCode = user.PostalCode,
                    EmployerName = user.EmployerName,
                    EmployeeNumber = user.EmployeeNumber,
                    PayrollReference = user.PayrollReference,
                    EmploymentType = user.EmploymentType,
                    BankName = user.BankName,
                    AccountType = user.AccountType,
                    AccountNumber = user.AccountNumber,
                    BranchCode = user.BranchCode,
                    NextOfKinName = user.NextOfKinName,
                    NextOfKinRelationship = user.NextOfKinRelationship,
                    NextOfKinPhone = user.NextOfKinPhone
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
                        Roles = roles,
                        StreetAddress = user.StreetAddress,
                        Suburb = user.Suburb,
                        City = user.City,
                        Province = user.Province,
                        PostalCode = user.PostalCode,
                        EmployerName = user.EmployerName,
                        EmployeeNumber = user.EmployeeNumber,
                        PayrollReference = user.PayrollReference,
                        EmploymentType = user.EmploymentType,
                        BankName = user.BankName,
                        AccountType = user.AccountType,
                        AccountNumber = user.AccountNumber,
                        BranchCode = user.BranchCode,
                        NextOfKinName = user.NextOfKinName,
                        NextOfKinRelationship = user.NextOfKinRelationship,
                        NextOfKinPhone = user.NextOfKinPhone
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

    [HttpPost("login-mobile-request")]
    public async Task<IActionResult> LoginMobileRequest(PhoneLoginDto model)
    {
        try
        {
            _logger.LogInformation($"[MOBILE-LOGIN] PIN request for phone: {model.PhoneNumber}");
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find user by phone number
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);

            if (user == null)
            {
                _logger.LogWarning($"[MOBILE-LOGIN] User not found for phone: {model.PhoneNumber}");
                return BadRequest("Phone number not registered");
            }

            // Generate 6-digit PIN
            var random = new Random();
            var pin = random.Next(100000, 999999).ToString();
            
            // Store PIN with 5-minute expiry
            _pinStore[model.PhoneNumber] = (pin, DateTime.UtcNow.AddMinutes(5));
            
            // Clean up expired PINs
            CleanExpiredPins();

            // Send PIN via WhatsApp using the hohemalogin template
            try
            {
                // Try template first
                var sent = await _whatsAppService.SendTemplateMessageAsync(
                    model.PhoneNumber, 
                    "hohemalogin", 
                    new List<string> { pin }
                );

                // If template fails, fall back to plain text message
                if (!sent)
                {
                    _logger.LogWarning($"[MOBILE-LOGIN] Template failed, trying plain text to: {model.PhoneNumber}");
                    sent = await _whatsAppService.SendMessageAsync(
                        model.PhoneNumber,
                        $"Your HoHema Loans login PIN is: {pin}\n\nThis PIN expires in 5 minutes. Do not share this code with anyone."
                    );
                }

                if (!sent)
                {
                    _logger.LogError($"[MOBILE-LOGIN] Both template and plain text failed for: {model.PhoneNumber}");
                    return StatusCode(500, new { 
                        error = "Failed to send verification PIN", 
                        details = "WhatsApp service failed. Check: 1) WhatsApp credentials in appsettings.json, 2) Phone number is registered in WhatsApp, 3) Check API logs for more details",
                        phoneNumber = model.PhoneNumber,
                        suggestion = "Create template 'hohemalogin' in WhatsApp Business Manager for production use"
                    });
                }

                _logger.LogInformation($"[MOBILE-LOGIN] PIN sent successfully to: {model.PhoneNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[MOBILE-LOGIN] Exception sending WhatsApp PIN to: {model.PhoneNumber}");
                return StatusCode(500, new { 
                    error = "Failed to send verification PIN", 
                    details = ex.Message,
                    phoneNumber = model.PhoneNumber
                });
            }
            
            return Ok(new { 
                message = "PIN sent to your WhatsApp", 
                phoneNumber = model.PhoneNumber 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[MOBILE-LOGIN] Exception during PIN request for: {model?.PhoneNumber}");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    [HttpPost("login-mobile-verify")]
    public async Task<IActionResult> LoginMobileVerify(PhoneVerifyDto model)
    {
        try
        {
            _logger.LogInformation($"[MOBILE-LOGIN] PIN verification for phone: {model.PhoneNumber}");
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if PIN exists and is valid
            if (!_pinStore.TryGetValue(model.PhoneNumber, out var pinData))
            {
                _logger.LogWarning($"[MOBILE-LOGIN] No PIN found for phone: {model.PhoneNumber}");
                return BadRequest("Invalid or expired PIN");
            }

            // Check if PIN has expired
            if (DateTime.UtcNow > pinData.Expiry)
            {
                _pinStore.Remove(model.PhoneNumber);
                _logger.LogWarning($"[MOBILE-LOGIN] Expired PIN for phone: {model.PhoneNumber}");
                return BadRequest("PIN has expired. Please request a new one");
            }

            // Verify PIN
            if (pinData.Pin != model.Pin)
            {
                _logger.LogWarning($"[MOBILE-LOGIN] Invalid PIN attempt for phone: {model.PhoneNumber}");
                return BadRequest("Invalid PIN");
            }

            // PIN is valid, remove it from store
            _pinStore.Remove(model.PhoneNumber);

            // Find user
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.PhoneNumber == model.PhoneNumber);

            if (user == null)
            {
                _logger.LogError($"[MOBILE-LOGIN] User disappeared for phone: {model.PhoneNumber}");
                return BadRequest("User not found");
            }

            // Generate JWT token
            var token = await GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);

            _logger.LogInformation($"[MOBILE-LOGIN] Login successful for: {model.PhoneNumber}");
            
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
                    Roles = roles,
                    StreetAddress = user.StreetAddress,
                    Suburb = user.Suburb,
                    City = user.City,
                    Province = user.Province,
                    PostalCode = user.PostalCode,
                    EmployerName = user.EmployerName,
                    EmployeeNumber = user.EmployeeNumber,
                    PayrollReference = user.PayrollReference,
                    EmploymentType = user.EmploymentType,
                    BankName = user.BankName,
                    AccountType = user.AccountType,
                    AccountNumber = user.AccountNumber,
                    BranchCode = user.BranchCode,
                    NextOfKinName = user.NextOfKinName,
                    NextOfKinRelationship = user.NextOfKinRelationship,
                    NextOfKinPhone = user.NextOfKinPhone
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[MOBILE-LOGIN] Exception during PIN verification for: {model?.PhoneNumber}");
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    [HttpPost("test-whatsapp")]
    public async Task<IActionResult> TestWhatsApp(PhoneLoginDto model)
    {
        try
        {
            _logger.LogInformation($"[TEST] Testing WhatsApp to: {model.PhoneNumber}");
            
            // Generate test PIN
            var testPin = "123456";
            
            // Try template first
            var templateSent = await _whatsAppService.SendTemplateMessageAsync(
                model.PhoneNumber, 
                "hohemalogin", 
                new List<string> { testPin }
            );

            // If template fails, try plain text
            var plainTextSent = false;
            if (!templateSent)
            {
                _logger.LogWarning($"[TEST] Template failed, trying plain text to: {model.PhoneNumber}");
                plainTextSent = await _whatsAppService.SendMessageAsync(
                    model.PhoneNumber,
                    "Test message from HoHema Loans. Your WhatsApp integration is working!"
                );
            }

            if (templateSent || plainTextSent)
            {
                return Ok(new { 
                    success = true,
                    templateWorked = templateSent,
                    plainTextWorked = plainTextSent,
                    message = templateSent 
                        ? "Template message sent successfully!" 
                        : "Template failed but plain text message worked. Create 'hohemalogin' template in WhatsApp Business Manager.",
                    phoneNumber = model.PhoneNumber,
                    testPin = testPin
                });
            }
            else
            {
                return Ok(new { 
                    success = false,
                    message = "Both template and plain text failed. Check WhatsApp credentials.",
                    phoneNumber = model.PhoneNumber,
                    checks = new[] {
                        "1. Verify WhatsApp credentials in appsettings.json",
                        "2. Check phone number is registered on WhatsApp",
                        "3. Create 'hohemalogin' template in WhatsApp Business Manager",
                        "4. Check API logs for detailed error messages",
                        "5. Verify phone number format is E.164 (+27xxxxxxxxx)",
                        "6. Test with your own WhatsApp number first"
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[TEST] Exception testing WhatsApp to: {model.PhoneNumber}");
            return StatusCode(500, new { 
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }

    private void CleanExpiredPins()
    {
        var expired = _pinStore.Where(kvp => DateTime.UtcNow > kvp.Value.Expiry)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in expired)
        {
            _pinStore.Remove(key);
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

public class PhoneLoginDto
{
    public string PhoneNumber { get; set; } = string.Empty;
}

public class PhoneVerifyDto
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
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

    // NCR-required Address fields
    public string StreetAddress { get; set; } = string.Empty;
    public string Suburb { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Province { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;

    // NCR-required Employment fields
    public string EmployerName { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string PayrollReference { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;

    // NCR-required Banking fields
    public string BankName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;

    // NCR-required Next of Kin fields
    public string NextOfKinName { get; set; } = string.Empty;
    public string NextOfKinRelationship { get; set; } = string.Empty;
    public string NextOfKinPhone { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}