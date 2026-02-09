using HoHemaLoans.Api.Models;
using HoHemaLoans.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContractsController : ControllerBase
{
    private readonly ContractService _contractService;
    private readonly ILogger<ContractsController> _logger;
    private readonly IConfiguration _configuration;

    public ContractsController(
        ContractService contractService,
        ILogger<ContractsController> logger,
        IConfiguration configuration)
    {
        _contractService = contractService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Generate a credit agreement for an approved loan application
    /// </summary>
    [HttpPost("generate/{loanApplicationId}")]
    public async Task<IActionResult> GenerateContract(Guid loanApplicationId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var contract = await _contractService.GenerateCreditAgreementAsync(loanApplicationId, userId);

            return Ok(new
            {
                success = true,
                message = "Contract generated successfully",
                contractId = contract.Id,
                contract = new
                {
                    contract.Id,
                    contract.LoanApplicationId,
                    contract.ContractType,
                    contract.Status,
                    contract.CreatedAt,
                    contract.ExpiresAt,
                    contract.ContractContent
                }
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating contract for loan application {LoanApplicationId}", loanApplicationId);
            return StatusCode(500, new { success = false, message = "An error occurred while generating the contract" });
        }
    }

    /// <summary>
    /// Get contract by ID
    /// </summary>
    [HttpGet("{contractId}")]
    public async Task<IActionResult> GetContract(int contractId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var contract = await _contractService.GetContractAsync(contractId, userId);

            if (contract == null)
            {
                return NotFound(new { success = false, message = "Contract not found" });
            }

            return Ok(new
            {
                success = true,
                contract = new
                {
                    contract.Id,
                    contract.LoanApplicationId,
                    contract.ContractType,
                    contract.Status,
                    contract.CreatedAt,
                    contract.SentAt,
                    contract.SignedAt,
                    contract.ExpiresAt,
                    contract.ContractContent,
                    contract.Version,
                    loanApplication = contract.LoanApplication != null ? new
                    {
                        contract.LoanApplication.Id,
                        contract.LoanApplication.Amount,
                        contract.LoanApplication.TermMonths,
                        contract.LoanApplication.MonthlyPayment,
                        contract.LoanApplication.TotalAmount,
                        contract.LoanApplication.Status
                    } : null,
                    digitalSignature = contract.DigitalSignature != null ? new
                    {
                        contract.DigitalSignature.Id,
                        contract.DigitalSignature.SignatureMethod,
                        contract.DigitalSignature.SignedAt,
                        contract.DigitalSignature.IsValid,
                        contract.DigitalSignature.PinExpiresAt
                    } : null
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contract {ContractId}", contractId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the contract" });
        }
    }

    /// <summary>
    /// Get all contracts for the current user
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserContracts()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var contracts = await _contractService.GetUserContractsAsync(userId);

            return Ok(new
            {
                success = true,
                contracts = contracts.Select(c => new
                {
                    c.Id,
                    c.LoanApplicationId,
                    c.ContractType,
                    c.Status,
                    c.CreatedAt,
                    c.SentAt,
                    c.SignedAt,
                    c.ExpiresAt,
                    loanAmount = c.LoanApplication?.Amount,
                    isSigned = c.DigitalSignature?.IsValid ?? false
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user contracts");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving contracts" });
        }
    }

    /// <summary>
    /// Get contract for a specific loan application
    /// </summary>
    [HttpGet("loan/{loanApplicationId}")]
    public async Task<IActionResult> GetContractByLoanApplication(Guid loanApplicationId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var contract = await _contractService.GetContractByLoanApplicationAsync(loanApplicationId, userId);

            if (contract == null)
            {
                return NotFound(new { success = false, message = "Contract not found for this loan application" });
            }

            return Ok(new
            {
                success = true,
                contract = new
                {
                    contract.Id,
                    contract.LoanApplicationId,
                    contract.ContractType,
                    contract.Status,
                    contract.CreatedAt,
                    contract.SentAt,
                    contract.SignedAt,
                    contract.ExpiresAt,
                    contract.ContractContent,
                    isSigned = contract.DigitalSignature?.IsValid ?? false,
                    pinExpired = contract.DigitalSignature?.PinExpiresAt < DateTime.UtcNow
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contract for loan application {LoanApplicationId}", loanApplicationId);
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving the contract" });
        }
    }

    /// <summary>
    /// Send contract for signing via WhatsApp PIN
    /// </summary>
    [HttpPost("{contractId}/send-pin")]
    public async Task<IActionResult> SendSigningPin(int contractId, [FromBody] SendPinRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(request.PhoneNumber))
            {
                return BadRequest(new { success = false, message = "Phone number is required" });
            }

            var isDevelopment = _configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development";

            var (success, message, pin) = await _contractService.SendContractForSigningAsync(
                contractId,
                userId,
                request.PhoneNumber,
                isDevelopment);

            var response = new
            {
                success,
                message,
                pin = isDevelopment ? pin : null // Only return PIN in development
            };

            return success ? Ok(response) : BadRequest(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending signing PIN for contract {ContractId}", contractId);
            return StatusCode(500, new { success = false, message = "An error occurred while sending the signing PIN" });
        }
    }

    /// <summary>
    /// Verify PIN and sign contract
    /// </summary>
    [HttpPost("{contractId}/verify-pin")]
    public async Task<IActionResult> VerifyPinAndSign(int contractId, [FromBody] VerifyPinRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrEmpty(request.Pin))
            {
                return BadRequest(new { success = false, message = "PIN is required" });
            }

            // Get IP address and user agent for audit
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            var (success, message) = await _contractService.VerifyPinAndSignContractAsync(
                contractId,
                userId,
                request.Pin,
                ipAddress,
                userAgent);

            return success ? Ok(new { success, message }) : BadRequest(new { success, message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying PIN for contract {ContractId}", contractId);
            return StatusCode(500, new { success = false, message = "An error occurred while verifying the PIN" });
        }
    }

    /// <summary>
    /// [Admin] Get all contracts
    /// </summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllContracts([FromQuery] string? status = null)
    {
        try
        {
            // This would need to be implemented in ContractService
            return Ok(new { success = true, message = "Admin endpoint - to be implemented" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all contracts");
            return StatusCode(500, new { success = false, message = "An error occurred while retrieving contracts" });
        }
    }
}

// Request DTOs
public class SendPinRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
}

public class VerifyPinRequest
{
    public string Pin { get; set; } = string.Empty;
}
