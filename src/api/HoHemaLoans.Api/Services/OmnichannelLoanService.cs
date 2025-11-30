using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HoHemaLoans.Api.Services;

/// <summary>
/// Unified service for managing loan applications across Web and WhatsApp channels.
/// Ensures data consistency and real-time synchronization between channels.
/// </summary>
public interface IOmnichannelLoanService
{
    Task<LoanApplication> CreateDraftApplicationAsync(string userId, LoanApplicationChannel channel, string? phoneNumber = null);
    Task<LoanApplication> UpdateApplicationStepAsync(Guid applicationId, string userId, int stepNumber, Dictionary<string, object> stepData);
    Task<LoanApplication?> GetApplicationAsync(Guid applicationId, string userId);
    Task<List<LoanApplication>> GetUserApplicationsAsync(string userId);
    Task<LoanApplication> SubmitApplicationAsync(Guid applicationId, string userId, string? otp = null);
    Task<LoanApplication?> ResumeFromChannelAsync(string userId, LoanApplicationChannel targetChannel, string? phoneNumber = null);
    Task SyncAffordabilityAsync(string userId);
}

public class OmnichannelLoanService : IOmnichannelLoanService
{
    private readonly ApplicationDbContext _context;
    private readonly IAffordabilityService _affordabilityService;
    private readonly ILogger<OmnichannelLoanService> _logger;

    public OmnichannelLoanService(
        ApplicationDbContext context,
        IAffordabilityService affordabilityService,
        ILogger<OmnichannelLoanService> logger)
    {
        _context = context;
        _affordabilityService = affordabilityService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new draft application. Can be initiated from Web or WhatsApp.
    /// </summary>
    public async Task<LoanApplication> CreateDraftApplicationAsync(
        string userId,
        LoanApplicationChannel channel,
        string? phoneNumber = null)
    {
        var application = new LoanApplication
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Status = LoanStatus.Draft,
            ApplicationDate = DateTime.UtcNow,
            ChannelOrigin = channel,
            CurrentStep = 0,
            StepData = new Dictionary<string, object>()
        };

        if (channel == LoanApplicationChannel.Web)
        {
            application.WebInitiatedDate = DateTime.UtcNow;
        }
        else if (channel == LoanApplicationChannel.WhatsApp)
        {
            application.WhatsAppInitiatedDate = DateTime.UtcNow;
            
            // Create WhatsApp session if phone number provided
            if (!string.IsNullOrEmpty(phoneNumber))
            {
                var session = new WhatsAppSession
                {
                    Id = Guid.NewGuid(),
                    PhoneNumber = phoneNumber,
                    UserId = userId,
                    DraftApplicationId = application.Id,
                    SessionStatus = "Active",
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };

                _context.WhatsAppSessions.Add(session);
                application.WhatsAppSessionId = session.Id;
            }
        }

        _context.LoanApplications.Add(application);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Draft application {application.Id} created via {channel} for user {userId}");

        return application;
    }

    /// <summary>
    /// Update a specific step in the application. Works for both Web and WhatsApp.
    /// Automatically syncs affordability assessment when needed.
    /// </summary>
    public async Task<LoanApplication> UpdateApplicationStepAsync(
        Guid applicationId,
        string userId,
        int stepNumber,
        Dictionary<string, object> stepData)
    {
        var application = await _context.LoanApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

        if (application == null)
        {
            throw new InvalidOperationException("Application not found");
        }

        if (application.Status != LoanStatus.Draft)
        {
            throw new InvalidOperationException("Only draft applications can be updated");
        }

        // Update step tracking
        application.CurrentStep = stepNumber;

        // Merge new step data with existing
        if (application.StepData == null)
        {
            application.StepData = new Dictionary<string, object>();
        }

        foreach (var kvp in stepData)
        {
            application.StepData[kvp.Key] = kvp.Value;
        }

        // Update specific fields based on step number and data
        UpdateApplicationFields(application, stepNumber, stepData);

        // Step 3 (Affordability Review) - Sync affordability assessment
        if (stepNumber == 3)
        {
            await SyncAffordabilityAsync(userId);
            application.IsAffordabilityIncluded = true;
        }

        // Step 4 (Preview Terms) - Calculate final amounts
        if (stepNumber == 4 && application.Amount > 0 && application.TermMonths > 0)
        {
            CalculateLoanTerms(application);
        }

        _context.LoanApplications.Update(application);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Application {applicationId} updated to step {stepNumber}");

        return application;
    }

    /// <summary>
    /// Get a specific application by ID
    /// </summary>
    public async Task<LoanApplication?> GetApplicationAsync(Guid applicationId, string userId)
    {
        return await _context.LoanApplications
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);
    }

    /// <summary>
    /// Get all applications for a user
    /// </summary>
    public async Task<List<LoanApplication>> GetUserApplicationsAsync(string userId)
    {
        return await _context.LoanApplications
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.ApplicationDate)
            .ToListAsync();
    }

    /// <summary>
    /// Submit a loan application for review
    /// </summary>
    public async Task<LoanApplication> SubmitApplicationAsync(
        Guid applicationId,
        string userId,
        string? otp = null)
    {
        var application = await _context.LoanApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

        if (application == null)
        {
            throw new InvalidOperationException("Application not found");
        }

        if (application.Status != LoanStatus.Draft)
        {
            throw new InvalidOperationException("Only draft applications can be submitted");
        }

        // Validate required fields
        ValidateApplicationForSubmission(application);

        // TODO: Validate OTP in production
        // For now, we skip OTP validation

        // Calculate final loan terms if not already done
        if (application.InterestRate == 0 || application.MonthlyPayment == 0)
        {
            CalculateLoanTerms(application);
        }

        // Update status and finalize
        application.Status = LoanStatus.Pending;
        application.ApplicationDate = DateTime.UtcNow;
        application.CurrentStep = 7; // All steps completed

        // Update WhatsApp session if exists
        if (application.WhatsAppSessionId.HasValue)
        {
            var session = await _context.WhatsAppSessions
                .FirstOrDefaultAsync(s => s.Id == application.WhatsAppSessionId);

            if (session != null)
            {
                session.SessionStatus = "Completed";
                session.CompletedAt = DateTime.UtcNow;
                _context.WhatsAppSessions.Update(session);
            }
        }

        _context.LoanApplications.Update(application);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Application {applicationId} submitted successfully");

        return application;
    }

    /// <summary>
    /// Resume an in-progress application from a different channel.
    /// Finds the most recent draft application and prepares it for the target channel.
    /// </summary>
    public async Task<LoanApplication?> ResumeFromChannelAsync(
        string userId,
        LoanApplicationChannel targetChannel,
        string? phoneNumber = null)
    {
        // Find most recent draft application
        var application = await _context.LoanApplications
            .Where(a => a.UserId == userId && a.Status == LoanStatus.Draft)
            .OrderByDescending(a => a.ApplicationDate)
            .FirstOrDefaultAsync();

        if (application == null)
        {
            return null;
        }

        // Update channel tracking
        if (targetChannel == LoanApplicationChannel.Web && !application.WebInitiatedDate.HasValue)
        {
            application.WebInitiatedDate = DateTime.UtcNow;
        }
        else if (targetChannel == LoanApplicationChannel.WhatsApp && !application.WhatsAppInitiatedDate.HasValue)
        {
            application.WhatsAppInitiatedDate = DateTime.UtcNow;

            // Create or update WhatsApp session
            if (!string.IsNullOrEmpty(phoneNumber))
            {
                var existingSession = application.WhatsAppSessionId.HasValue
                    ? await _context.WhatsAppSessions.FindAsync(application.WhatsAppSessionId.Value)
                    : null;

                if (existingSession == null)
                {
                    var newSession = new WhatsAppSession
                    {
                        Id = Guid.NewGuid(),
                        PhoneNumber = phoneNumber,
                        UserId = userId,
                        DraftApplicationId = application.Id,
                        SessionStatus = "Active",
                        CreatedAt = DateTime.UtcNow,
                        LastUpdatedAt = DateTime.UtcNow,
                        Notes = $"Resumed from {application.ChannelOrigin}"
                    };

                    _context.WhatsAppSessions.Add(newSession);
                    application.WhatsAppSessionId = newSession.Id;
                }
                else
                {
                    existingSession.SessionStatus = "Active";
                    existingSession.LastUpdatedAt = DateTime.UtcNow;
                    _context.WhatsAppSessions.Update(existingSession);
                }
            }
        }

        _context.LoanApplications.Update(application);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            $"Application {application.Id} resumed from {application.ChannelOrigin} to {targetChannel}");

        return application;
    }

    /// <summary>
    /// Synchronize affordability assessment across all channels.
    /// This ensures that affordability updates from WhatsApp or Web are reflected everywhere.
    /// </summary>
    public async Task SyncAffordabilityAsync(string userId)
    {
        try
        {
            // Recalculate affordability assessment
            var assessment = await _affordabilityService.CalculateAffordabilityAsync(userId);

            _logger.LogInformation(
                $"Affordability synced for user {userId}: Status={assessment.AffordabilityStatus}, " +
                $"MaxLoan={assessment.MaxRecommendedLoanAmount:C}");

            // Note: The assessment is already saved by AffordabilityService
            // Any draft applications will automatically see the updated assessment
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to sync affordability for user {userId}");
            throw;
        }
    }

    /// <summary>
    /// Update application fields based on step data
    /// </summary>
    private void UpdateApplicationFields(
        LoanApplication application,
        int stepNumber,
        Dictionary<string, object> stepData)
    {
        switch (stepNumber)
        {
            case 0: // Loan Amount
                if (stepData.ContainsKey("amount"))
                {
                    application.Amount = Convert.ToDecimal(stepData["amount"]);
                }
                break;

            case 1: // Term Months
                if (stepData.ContainsKey("termMonths"))
                {
                    application.TermMonths = Convert.ToInt32(stepData["termMonths"]);
                }
                break;

            case 2: // Purpose
                if (stepData.ContainsKey("purpose"))
                {
                    application.Purpose = stepData["purpose"].ToString();
                }
                if (stepData.ContainsKey("purposeDescription"))
                {
                    application.StepData["purposeDescription"] = stepData["purposeDescription"];
                }
                break;

            case 5: // Bank Details
                if (stepData.ContainsKey("bankName"))
                {
                    application.BankName = stepData["bankName"].ToString();
                }
                if (stepData.ContainsKey("accountNumber"))
                {
                    application.AccountNumber = stepData["accountNumber"].ToString();
                }
                if (stepData.ContainsKey("accountHolderName"))
                {
                    application.AccountHolderName = stepData["accountHolderName"].ToString();
                }
                break;
        }
    }

    /// <summary>
    /// Calculate loan terms (interest rate, monthly payment, total amount)
    /// </summary>
    private void CalculateLoanTerms(LoanApplication application)
    {
        if (application.Amount <= 0 || application.TermMonths <= 0)
        {
            return;
        }

        // Calculate interest rate based on amount and term
        var baseRate = 0.12m; // 12% base rate
        
        if (application.Amount > 100000) baseRate -= 0.01m;
        if (application.TermMonths > 24) baseRate += 0.005m;
        
        application.InterestRate = Math.Max(0.08m, Math.Min(0.18m, baseRate));

        // Calculate monthly payment using amortization formula
        var monthlyRate = application.InterestRate / 12;
        if (monthlyRate == 0)
        {
            application.MonthlyPayment = application.Amount / application.TermMonths;
        }
        else
        {
            var payment = application.Amount * 
                (monthlyRate * (decimal)Math.Pow((double)(1 + monthlyRate), application.TermMonths)) /
                ((decimal)Math.Pow((double)(1 + monthlyRate), application.TermMonths) - 1);
            
            application.MonthlyPayment = Math.Round(payment, 2);
        }

        application.TotalAmount = application.MonthlyPayment * application.TermMonths;
    }

    /// <summary>
    /// Validate that all required fields are present before submission
    /// </summary>
    private void ValidateApplicationForSubmission(LoanApplication application)
    {
        if (application.Amount <= 0)
        {
            throw new InvalidOperationException("Loan amount is required");
        }

        if (application.TermMonths <= 0)
        {
            throw new InvalidOperationException("Loan term is required");
        }

        if (string.IsNullOrEmpty(application.Purpose))
        {
            throw new InvalidOperationException("Loan purpose is required");
        }

        if (string.IsNullOrEmpty(application.BankName) || 
            string.IsNullOrEmpty(application.AccountNumber) ||
            string.IsNullOrEmpty(application.AccountHolderName))
        {
            throw new InvalidOperationException("Bank details are required");
        }
    }
}
