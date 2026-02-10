using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HoHemaLoans.Api.Services;
using HoHemaLoans.Api.Models;
using System.Security.Claims;

namespace HoHemaLoans.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NCRController : ControllerBase
{
    private readonly INCRComplianceService _ncrComplianceService;
    private readonly IOmnichannelLoanService _loanService;
    private readonly ILogger<NCRController> _logger;

    public NCRController(
        INCRComplianceService ncrComplianceService,
        IOmnichannelLoanService loanService,
        ILogger<NCRController> logger)
    {
        _ncrComplianceService = ncrComplianceService;
        _loanService = loanService;
        _logger = logger;
    }

    /// <summary>
    /// Get NCR configuration settings
    /// </summary>
    [HttpGet("configuration")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<NCRConfiguration>> GetConfiguration()
    {
        try
        {
            var config = await _ncrComplianceService.GetNCRConfigurationAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting NCR configuration");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Validate loan application for NCR compliance
    /// </summary>
    [HttpPost("validate/{applicationId}")]
    public async Task<ActionResult<NCRComplianceValidationResponse>> ValidateCompliance(Guid applicationId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var application = await _loanService.ValidateNCRComplianceAsync(applicationId);
            
            var response = new NCRComplianceValidationResponse
            {
                ApplicationId = applicationId,
                IsCompliant = application.Status != LoanStatus.ComplianceReview,
                Status = application.Status.ToString(),
                Notes = application.Notes,
                ValidationDate = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating NCR compliance for application {ApplicationId}", applicationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Generate Form 39 (Credit Agreement) data
    /// </summary>
    [HttpGet("form39/{applicationId}")]
    public async Task<ActionResult<Form39Data>> GenerateForm39(Guid applicationId)
    {
        try
        {
            var form39Data = await _ncrComplianceService.GenerateForm39DataAsync(applicationId);
            return Ok(form39Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating Form 39 for application {ApplicationId}", applicationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Generate Pre-Agreement Statement
    /// </summary>
    [HttpPost("pre-agreement")]
    public async Task<ActionResult<PreAgreementStatementData>> GeneratePreAgreementStatement([FromBody] PreAgreementRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            // Get user from request or current user
            var user = new ApplicationUser
            {
                Id = userId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email
            };

            var calculation = new LoanCalculation
            {
                LoanAmount = request.LoanAmount,
                InterestRate = request.InterestRate,
                TermInMonths = request.TermInMonths,
                InitiationFee = request.InitiationFee,
                MonthlyServiceFee = request.MonthlyServiceFee,
                MonthlyInstallment = request.MonthlyInstallment,
                TotalAmountPayable = request.TotalAmountPayable
            };

            var preAgreementData = await _ncrComplianceService.GeneratePreAgreementStatementAsync(calculation, user);
            return Ok(preAgreementData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating pre-agreement statement");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cancel loan within cooling-off period
    /// </summary>
    [HttpPost("cancel/{applicationId}")]
    public async Task<ActionResult<CoolingOffResult>> CancelLoanInCoolingOff(Guid applicationId, [FromBody] CancelLoanRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var result = await _ncrComplianceService.CancelLoanWithinCoolingOffAsync(applicationId, userId, request.Reason);
            
            if (result.IsSuccessful)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(new { error = result.ErrorMessage });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling loan {ApplicationId} in cooling-off period", applicationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Check if loan is within cooling-off period
    /// </summary>
    [HttpGet("cooling-off/{applicationId}")]
    public async Task<ActionResult<CoolingOffStatusResponse>> CheckCoolingOffStatus(Guid applicationId)
    {
        try
        {
            var isWithinPeriod = await _ncrComplianceService.IsWithinCoolingOffPeriodAsync(applicationId);
            
            var response = new CoolingOffStatusResponse
            {
                ApplicationId = applicationId,
                IsWithinCoolingOffPeriod = isWithinPeriod,
                CheckedAt = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking cooling-off status for application {ApplicationId}", applicationId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get active consumer complaints
    /// </summary>
    [HttpGet("complaints")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<ConsumerComplaint>>> GetActiveComplaints()
    {
        try
        {
            var complaints = await _ncrComplianceService.GetActiveComplaintsAsync();
            return Ok(complaints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active complaints");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Create a new consumer complaint
    /// </summary>
    [HttpPost("complaints")]
    public async Task<ActionResult<ConsumerComplaint>> CreateComplaint([FromBody] CreateComplaintRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            request.UserId = userId; // Ensure complaint is for current user

            var complaint = await _ncrComplianceService.CreateComplaintAsync(request);
            return CreatedAtAction(nameof(GetComplaint), new { id = complaint.Id }, complaint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating complaint");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get a specific complaint
    /// </summary>
    [HttpGet("complaints/{id}")]
    public async Task<ActionResult<ConsumerComplaint>> GetComplaint(int id)
    {
        try
        {
            var complaints = await _ncrComplianceService.GetActiveComplaintsAsync();
            var complaint = complaints.FirstOrDefault(c => c.Id == id);
            
            if (complaint == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");
            
            // Users can only see their own complaints, admins can see all
            if (!isAdmin && complaint.UserId != userId)
            {
                return Forbid();
            }

            return Ok(complaint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting complaint {ComplaintId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Update a complaint (admin only)
    /// </summary>
    [HttpPut("complaints/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ConsumerComplaint>> UpdateComplaint(int id, [FromBody] UpdateComplaintRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            request.UpdatedBy = userId ?? "System";

            var complaint = await _ncrComplianceService.UpdateComplaintAsync(id, request);
            return Ok(complaint);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating complaint {ComplaintId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}