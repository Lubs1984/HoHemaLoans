using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;

namespace HoHemaLoans.Api.Services;

/// <summary>
/// Orchestrates the WhatsApp loan application process using Meta WhatsApp Flows.
/// Manages the pre-application checklist: Registration → Profile → Documents → Affordability → Application
/// Ensures omnichannel parity between web and WhatsApp.
/// </summary>
public interface IWhatsAppFlowOrchestrationService
{
    /// <summary>
    /// Main entry point: determines what the user needs to do next and launches the appropriate flow.
    /// </summary>
    Task HandleIncomingMessageAsync(string phoneNumber, string messageText, WhatsAppContact contact);

    /// <summary>
    /// Handles data_exchange callbacks from Meta WhatsApp Flows.
    /// Called by the flow endpoint when a flow screen submits data.
    /// </summary>
    Task<FlowResponse> HandleFlowDataExchangeAsync(string flowId, string screenId, Dictionary<string, object> payload, string userId);

    /// <summary>
    /// Handles flow completion callbacks.
    /// Called when a flow reaches its terminal screen and the user clicks "Done".
    /// </summary>
    Task HandleFlowCompletionAsync(string flowId, Dictionary<string, object> payload, string userId);
}

/// <summary>
/// Response sent back to Meta WhatsApp Flows on data_exchange actions.
/// </summary>
public class FlowResponse
{
    public string? Screen { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Tracks which stage of the pre-application checklist a user is at.
/// </summary>
public enum ApplicationReadiness
{
    NotRegistered,          // User has no account
    ProfileIncomplete,      // Registration exists but profile not finished
    DocumentsMissing,       // ID and/or proof of address not uploaded
    AffordabilityNeeded,    // No affordability assessment or it's stale
    ReadyToApply,           // All checks passed
    ApplicationInProgress   // User already has a draft application
}

public class WhatsAppFlowOrchestrationService : IWhatsAppFlowOrchestrationService
{
    private readonly ApplicationDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IAffordabilityService _affordabilityService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<WhatsAppFlowOrchestrationService> _logger;

    // Flow IDs — these must match what's configured in Meta Business Manager
    private const string FLOW_PROFILE_COMPLETION = "profile_completion_flow";
    private const string FLOW_DOCUMENT_UPLOAD = "document_upload_flow";
    private const string FLOW_AFFORDABILITY = "affordability_flow";
    private const string FLOW_LOAN_APPLICATION = "loan_application_flow";

    public WhatsAppFlowOrchestrationService(
        ApplicationDbContext context,
        IWhatsAppService whatsAppService,
        IAffordabilityService affordabilityService,
        UserManager<ApplicationUser> userManager,
        ILogger<WhatsAppFlowOrchestrationService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _affordabilityService = affordabilityService;
        _userManager = userManager;
        _logger = logger;
    }

    // ==================== MAIN ENTRY POINT ====================

    public async Task HandleIncomingMessageAsync(string phoneNumber, string messageText, WhatsAppContact contact)
    {
        var normalizedMessage = messageText.Trim().ToUpper();

        // Look up registered user
        var user = await FindUserByPhoneAsync(phoneNumber);

        if (user == null)
        {
            await HandleUnregisteredUserAsync(phoneNumber, normalizedMessage);
            return;
        }

        // Link contact to user if not already linked
        if (string.IsNullOrEmpty(contact.UserId))
        {
            contact.UserId = user.Id;
            await _context.SaveChangesAsync();
        }

        // Handle global commands
        if (normalizedMessage == "HELP" || normalizedMessage == "MENU")
        {
            await SendMainMenuAsync(phoneNumber, user);
            return;
        }

        if (normalizedMessage == "BALANCE" || normalizedMessage == "STATUS")
        {
            await SendLoanStatusAsync(phoneNumber, user.Id);
            return;
        }

        if (normalizedMessage == "PROFILE")
        {
            await LaunchFlowAsync(phoneNumber, FLOW_PROFILE_COMPLETION, user.Id);
            return;
        }

        if (normalizedMessage == "DOCUMENTS" || normalizedMessage == "DOCS")
        {
            await LaunchFlowAsync(phoneNumber, FLOW_DOCUMENT_UPLOAD, user.Id);
            return;
        }

        // Default: check readiness and guide user through the checklist
        if (normalizedMessage == "APPLY" || normalizedMessage == "LOAN" || normalizedMessage == "YES" || normalizedMessage == "NEW" || normalizedMessage == "START")
        {
            await CheckReadinessAndGuideAsync(phoneNumber, user);
            return;
        }

        if (normalizedMessage == "CONTINUE" || normalizedMessage == "RESUME")
        {
            await ResumeApplicationAsync(phoneNumber, user);
            return;
        }

        // Default welcome / routing
        await SendWelcomeAndOptionsAsync(phoneNumber, user);
    }

    // ==================== READINESS CHECK & ROUTING ====================

    /// <summary>
    /// Checks user readiness across all pre-application gates and guides them to the next step.
    /// </summary>
    private async Task CheckReadinessAndGuideAsync(string phoneNumber, ApplicationUser user)
    {
        var readiness = await AssessReadinessAsync(user);

        _logger.LogInformation("User {UserId} readiness: {Readiness}", user.Id, readiness);

        switch (readiness)
        {
            case ApplicationReadiness.ProfileIncomplete:
                await _whatsAppService.SendMessageAsync(phoneNumber,
                    "📋 Before you can apply, we need to complete your profile.\n\n" +
                    "This includes your personal details, address, employment, and banking information.\n\n" +
                    "Let's get started! 👇");
                await LaunchFlowAsync(phoneNumber, FLOW_PROFILE_COMPLETION, user.Id);
                break;

            case ApplicationReadiness.DocumentsMissing:
                var docStatus = await GetDocumentStatusAsync(user.Id);
                await _whatsAppService.SendMessageAsync(phoneNumber,
                    $"📄 We need to verify your identity before you can apply.\n\n" +
                    $"{docStatus}\n\n" +
                    "Please upload the required documents. 👇");
                await LaunchFlowAsync(phoneNumber, FLOW_DOCUMENT_UPLOAD, user.Id);
                break;

            case ApplicationReadiness.AffordabilityNeeded:
                await _whatsAppService.SendMessageAsync(phoneNumber,
                    "📊 We need to assess your affordability (NCR requirement).\n\n" +
                    "This helps us determine the right loan amount for you.\n\n" +
                    "Let's review your income and expenses. 👇");
                await LaunchFlowAsync(phoneNumber, FLOW_AFFORDABILITY, user.Id);
                break;

            case ApplicationReadiness.ReadyToApply:
                await _whatsAppService.SendMessageAsync(phoneNumber,
                    "✅ Great news! Everything looks good.\n\n" +
                    "You're ready to apply for a loan! 💰\n\n" +
                    "Starting your application now... 👇");
                await LaunchFlowAsync(phoneNumber, FLOW_LOAN_APPLICATION, user.Id);
                break;

            case ApplicationReadiness.ApplicationInProgress:
                await ResumeApplicationAsync(phoneNumber, user);
                break;
        }
    }

    /// <summary>
    /// Evaluates what the user still needs to complete before applying.
    /// </summary>
    private async Task<ApplicationReadiness> AssessReadinessAsync(ApplicationUser user)
    {
        // 1. Check profile completeness
        if (!IsProfileComplete(user))
            return ApplicationReadiness.ProfileIncomplete;

        // 2. Check documents (ID + proof of address)
        if (!await HasRequiredDocumentsAsync(user.Id))
            return ApplicationReadiness.DocumentsMissing;

        // 3. Check affordability
        if (!await HasValidAffordabilityAsync(user.Id))
            return ApplicationReadiness.AffordabilityNeeded;

        // 4. Check if they have an in-progress application
        var draftApp = await _context.LoanApplications
            .FirstOrDefaultAsync(la => la.UserId == user.Id && la.Status == LoanStatus.Draft);

        if (draftApp != null)
            return ApplicationReadiness.ApplicationInProgress;

        return ApplicationReadiness.ReadyToApply;
    }

    // ==================== PROFILE CHECKS ====================

    private bool IsProfileComplete(ApplicationUser user)
    {
        return !string.IsNullOrEmpty(user.FirstName) &&
               !string.IsNullOrEmpty(user.LastName) &&
               !string.IsNullOrEmpty(user.IdNumber) &&
               !string.IsNullOrEmpty(user.Address) &&
               !string.IsNullOrEmpty(user.City) &&
               !string.IsNullOrEmpty(user.Province) &&
               !string.IsNullOrEmpty(user.PostalCode) &&
               user.BusinessId.HasValue; // Must be linked to employer
    }

    // ==================== DOCUMENT CHECKS ====================

    private async Task<bool> HasRequiredDocumentsAsync(string userId)
    {
        var documents = await _context.UserDocuments
            .Where(d => d.UserId == userId && !d.IsDeleted)
            .ToListAsync();

        var hasId = documents.Any(d =>
            d.DocumentType == DocumentType.IdDocument);

        var hasProofOfAddress = documents.Any(d =>
            d.DocumentType == DocumentType.ProofOfAddress ||
            d.DocumentType == DocumentType.BankStatement);

        return hasId && hasProofOfAddress;
    }

    private async Task<string> GetDocumentStatusAsync(string userId)
    {
        var documents = await _context.UserDocuments
            .Where(d => d.UserId == userId && !d.IsDeleted)
            .ToListAsync();

        var hasId = documents.Any(d =>
            d.DocumentType == DocumentType.IdDocument);

        var hasPoA = documents.Any(d =>
            d.DocumentType == DocumentType.ProofOfAddress ||
            d.DocumentType == DocumentType.BankStatement);

        return $"1️⃣ ID / Passport: {(hasId ? "✅ Uploaded" : "❌ Not uploaded")}\n" +
               $"2️⃣ Proof of Address: {(hasPoA ? "✅ Uploaded" : "❌ Not uploaded")}";
    }

    // ==================== AFFORDABILITY CHECKS ====================

    private async Task<bool> HasValidAffordabilityAsync(string userId)
    {
        var assessment = await _context.AffordabilityAssessments
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AssessmentDate)
            .FirstOrDefaultAsync();

        if (assessment == null)
            return false;

        // Assessment must be less than 30 days old
        if (assessment.AssessmentDate < DateTime.UtcNow.AddDays(-30))
            return false;

        return true;
    }

    // ==================== FLOW LAUNCHING ====================

    /// <summary>
    /// Launches a WhatsApp Flow by sending a flow message to the user.
    /// Uses the Meta WhatsApp Flows API to send interactive flow messages.
    /// </summary>
    private async Task LaunchFlowAsync(string phoneNumber, string flowId, string userId)
    {
        _logger.LogInformation("Launching flow {FlowId} for user {UserId}", flowId, userId);

        try
        {
            // Get initial data for the flow
            var flowData = await GetFlowInitialDataAsync(flowId, userId);

            await _whatsAppService.SendFlowMessageAsync(
                phoneNumber,
                flowId,
                flowData.Screen,
                flowData.Data ?? new Dictionary<string, object>()
            );

            _logger.LogInformation("Flow {FlowId} launched successfully for {PhoneNumber}", flowId, phoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch flow {FlowId} for {PhoneNumber}", flowId, phoneNumber);
            await _whatsAppService.SendMessageAsync(phoneNumber,
                "Sorry, something went wrong. Please try again or visit our website.\n\n" +
                "🌐 https://hohemaweb-development.up.railway.app");
        }
    }

    /// <summary>
    /// Provides initial data for each flow screen based on user's current state.
    /// </summary>
    private async Task<FlowResponse> GetFlowInitialDataAsync(string flowId, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        return flowId switch
        {
            FLOW_PROFILE_COMPLETION => await GetProfileFlowInitialDataAsync(user!),
            FLOW_DOCUMENT_UPLOAD => await GetDocumentFlowInitialDataAsync(userId),
            FLOW_AFFORDABILITY => await GetAffordabilityFlowInitialDataAsync(userId),
            FLOW_LOAN_APPLICATION => await GetLoanApplicationFlowInitialDataAsync(userId),
            _ => new FlowResponse { Screen = "ERROR", Data = new Dictionary<string, object>() }
        };
    }

    // ==================== FLOW DATA PROVIDERS ====================

    private async Task<FlowResponse> GetProfileFlowInitialDataAsync(ApplicationUser user)
    {
        var businesses = await _context.Businesses.Where(b => b.IsActive).ToListAsync();

        return new FlowResponse
        {
            Screen = "PERSONAL_INFO",
            Data = new Dictionary<string, object>
            {
                ["first_name"] = user.FirstName ?? "",
                ["last_name"] = user.LastName ?? "",
                ["id_number"] = user.IdNumber ?? "",
                ["date_of_birth"] = user.DateOfBirth.ToString("yyyy-MM-dd")
            }
        };
    }

    private async Task<FlowResponse> GetDocumentFlowInitialDataAsync(string userId)
    {
        var hasId = await _context.UserDocuments
            .AnyAsync(d => d.UserId == userId && !d.IsDeleted &&
                d.DocumentType == DocumentType.IdDocument);

        var hasPoA = await _context.UserDocuments
            .AnyAsync(d => d.UserId == userId && !d.IsDeleted &&
                (d.DocumentType == DocumentType.ProofOfAddress ||
                 d.DocumentType == DocumentType.BankStatement));

        return new FlowResponse
        {
            Screen = "DOCUMENT_CHECKLIST",
            Data = new Dictionary<string, object>
            {
                ["id_status"] = hasId ? "✅ Uploaded" : "❌ Not uploaded",
                ["poa_status"] = hasPoA ? "✅ Uploaded" : "❌ Not uploaded",
                ["id_uploaded"] = hasId,
                ["poa_uploaded"] = hasPoA
            }
        };
    }

    private async Task<FlowResponse> GetAffordabilityFlowInitialDataAsync(string userId)
    {
        var incomes = await _context.Incomes
            .Where(i => i.UserId == userId)
            .ToListAsync();

        var totalIncome = incomes.Sum(i => i.MonthlyAmount);

        var incomeSummary = incomes.Any()
            ? "💰 Current income on file:\n" + string.Join("\n", incomes.Select(i => $"• {i.SourceType}: R{i.MonthlyAmount:N0}")) + $"\nTotal: R{totalIncome:N0}"
            : "💰 No income information on file yet.";

        var incomeActions = new List<Dictionary<string, string>>
        {
            new() { ["id"] = "keep", ["title"] = "✅ Information is correct" },
            new() { ["id"] = "update", ["title"] = "✏️ I want to update it" },
            new() { ["id"] = "add_new", ["title"] = "➕ Add a new income source" }
        };

        return new FlowResponse
        {
            Screen = "INCOME_OVERVIEW",
            Data = new Dictionary<string, object>
            {
                ["has_existing_income"] = incomes.Any(),
                ["existing_income_summary"] = incomeSummary,
                ["total_income"] = $"R{totalIncome:N0}",
                ["income_action_options"] = incomeActions
            }
        };
    }

    private async Task<FlowResponse> GetLoanApplicationFlowInitialDataAsync(string userId)
    {
        // Check for existing draft to support resume
        var existingDraft = await _context.LoanApplications
            .FirstOrDefaultAsync(la => la.UserId == userId && la.Status == LoanStatus.Draft);

        var user = await _userManager.FindByIdAsync(userId);

        return new FlowResponse
        {
            Screen = "LOAN_AMOUNT",
            Data = new Dictionary<string, object>
            {
                ["min_amount"] = "R500",
                ["max_amount"] = "R50,000",
                ["draft_id"] = existingDraft?.Id.ToString() ?? "",
                ["existing_amount"] = existingDraft?.Amount.ToString("0") ?? "",
                ["is_resume"] = existingDraft != null,
                ["resume_message"] = existingDraft != null
                    ? $"📝 You have an existing draft application (R{existingDraft.Amount:N0}). You can update or continue."
                    : ""
            }
        };
    }

    // ==================== FLOW DATA EXCHANGE HANDLERS ====================

    public async Task<FlowResponse> HandleFlowDataExchangeAsync(string flowId, string screenId, Dictionary<string, object> payload, string userId)
    {
        _logger.LogInformation("Flow data exchange: Flow={FlowId}, Screen={Screen}, User={UserId}", flowId, screenId, userId);

        return flowId switch
        {
            FLOW_PROFILE_COMPLETION => await HandleProfileFlowExchangeAsync(screenId, payload, userId),
            FLOW_DOCUMENT_UPLOAD => await HandleDocumentFlowExchangeAsync(screenId, payload, userId),
            FLOW_AFFORDABILITY => await HandleAffordabilityFlowExchangeAsync(screenId, payload, userId),
            FLOW_LOAN_APPLICATION => await HandleLoanApplicationFlowExchangeAsync(screenId, payload, userId),
            _ => new FlowResponse { Screen = "ERROR", Data = new Dictionary<string, object> { ["error"] = "Unknown flow" } }
        };
    }

    // -------- Profile Flow --------

    private async Task<FlowResponse> HandleProfileFlowExchangeAsync(string screenId, Dictionary<string, object> payload, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) throw new InvalidOperationException("User not found");

        switch (screenId)
        {
            case "PERSONAL_INFO":
                user.FirstName = payload.GetValueOrDefault("first_name")?.ToString() ?? user.FirstName;
                user.LastName = payload.GetValueOrDefault("last_name")?.ToString() ?? user.LastName;
                user.IdNumber = payload.GetValueOrDefault("id_number")?.ToString() ?? user.IdNumber;
                if (payload.ContainsKey("date_of_birth") && DateTime.TryParse(payload["date_of_birth"]?.ToString(), out var dob))
                    user.DateOfBirth = dob;
                await _userManager.UpdateAsync(user);

                var provinces = new List<Dictionary<string, string>>
                {
                    new() { ["id"] = "gauteng", ["title"] = "Gauteng" },
                    new() { ["id"] = "western_cape", ["title"] = "Western Cape" },
                    new() { ["id"] = "kwazulu_natal", ["title"] = "KwaZulu-Natal" },
                    new() { ["id"] = "eastern_cape", ["title"] = "Eastern Cape" },
                    new() { ["id"] = "free_state", ["title"] = "Free State" },
                    new() { ["id"] = "limpopo", ["title"] = "Limpopo" },
                    new() { ["id"] = "mpumalanga", ["title"] = "Mpumalanga" },
                    new() { ["id"] = "north_west", ["title"] = "North West" },
                    new() { ["id"] = "northern_cape", ["title"] = "Northern Cape" }
                };

                return new FlowResponse
                {
                    Screen = "ADDRESS_INFO",
                    Data = new Dictionary<string, object>
                    {
                        ["street_address"] = user.Address ?? "",
                        ["suburb"] = user.Suburb ?? "",
                        ["city"] = user.City ?? "",
                        ["province_options"] = provinces,
                        ["postal_code"] = user.PostalCode ?? ""
                    }
                };

            case "ADDRESS_INFO":
                user.Address = payload.GetValueOrDefault("street_address")?.ToString() ?? user.Address;
                user.Suburb = payload.GetValueOrDefault("suburb")?.ToString() ?? user.Suburb;
                user.City = payload.GetValueOrDefault("city")?.ToString() ?? user.City;
                user.Province = payload.GetValueOrDefault("province")?.ToString() ?? user.Province;
                user.PostalCode = payload.GetValueOrDefault("postal_code")?.ToString() ?? user.PostalCode;
                await _userManager.UpdateAsync(user);

                var businesses = await _context.Businesses.Where(b => b.IsActive).ToListAsync();
                var employerOptions = businesses.Select(b => new Dictionary<string, string>
                {
                    ["id"] = b.Id.ToString(),
                    ["title"] = b.Name
                }).ToList();

                return new FlowResponse
                {
                    Screen = "EMPLOYMENT_INFO",
                    Data = new Dictionary<string, object>
                    {
                        ["employer_options"] = employerOptions,
                        ["employee_number"] = user.EmployeeNumber ?? "",
                        ["job_title"] = user.EmploymentType ?? ""
                    }
                };

            case "EMPLOYMENT_INFO":
                if (payload.ContainsKey("employer") && Guid.TryParse(payload["employer"]?.ToString(), out var businessId))
                    user.BusinessId = businessId;
                user.EmployeeNumber = payload.GetValueOrDefault("employee_number")?.ToString() ?? user.EmployeeNumber;
                user.EmploymentType = payload.GetValueOrDefault("job_title")?.ToString() ?? user.EmploymentType;
                await _userManager.UpdateAsync(user);

                var bankOptions = new List<Dictionary<string, string>>
                {
                    new() { ["id"] = "fnb", ["title"] = "FNB" },
                    new() { ["id"] = "standard_bank", ["title"] = "Standard Bank" },
                    new() { ["id"] = "absa", ["title"] = "ABSA" },
                    new() { ["id"] = "nedbank", ["title"] = "Nedbank" },
                    new() { ["id"] = "capitec", ["title"] = "Capitec" },
                    new() { ["id"] = "african_bank", ["title"] = "African Bank" }
                };

                return new FlowResponse
                {
                    Screen = "BANKING_INFO",
                    Data = new Dictionary<string, object>
                    {
                        ["bank_options"] = bankOptions,
                        ["account_holder"] = $"{user.FirstName} {user.LastName}",
                        ["account_number"] = user.AccountNumber ?? ""
                    }
                };

            case "BANKING_INFO":
                user.BankName = payload.GetValueOrDefault("bank_name")?.ToString() ?? user.BankName;
                user.AccountNumber = payload.GetValueOrDefault("account_number")?.ToString() ?? user.AccountNumber;
                await _userManager.UpdateAsync(user);

                return new FlowResponse
                {
                    Screen = "NEXT_OF_KIN",
                    Data = new Dictionary<string, object>()
                };

            case "NEXT_OF_KIN":
                user.NextOfKinName = payload.GetValueOrDefault("nok_name")?.ToString() ?? "";
                user.NextOfKinRelationship = payload.GetValueOrDefault("nok_relationship")?.ToString() ?? "";
                user.NextOfKinPhone = payload.GetValueOrDefault("nok_phone")?.ToString() ?? "";
                user.IsVerified = true;
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                var maskedId = user.IdNumber != null && user.IdNumber.Length >= 13
                    ? $"****{user.IdNumber.Substring(4, 6)}***"
                    : "****";
                var maskedBank = user.AccountNumber != null && user.AccountNumber.Length >= 4
                    ? $"****{user.AccountNumber[^4..]}"
                    : "****";

                return new FlowResponse
                {
                    Screen = "PROFILE_SUMMARY",
                    Data = new Dictionary<string, object>
                    {
                        ["full_name"] = $"{user.FirstName} {user.LastName}",
                        ["id_number_masked"] = maskedId,
                        ["address_summary"] = $"{user.Address}, {user.City}, {user.Province}",
                        ["employer_name"] = (await _context.Businesses.FindAsync(user.BusinessId))?.Name ?? "Not set",
                        ["bank_summary"] = $"{user.BankName} - {maskedBank}",
                        ["profile_url"] = "https://hohemaweb-development.up.railway.app/profile"
                    }
                };

            default:
                return new FlowResponse { Screen = "PERSONAL_INFO", Data = new Dictionary<string, object>() };
        }
    }

    // -------- Document Flow --------

    private async Task<FlowResponse> HandleDocumentFlowExchangeAsync(string screenId, Dictionary<string, object> payload, string userId)
    {
        switch (screenId)
        {
            case "DOCUMENT_CHECKLIST":
                var idUploaded = payload.ContainsKey("id_uploaded") && payload["id_uploaded"]?.ToString() == "True";
                // Route to whichever document is still needed
                if (!idUploaded)
                {
                    return new FlowResponse
                    {
                        Screen = "UPLOAD_ID",
                        Data = new Dictionary<string, object>
                        {
                            ["id_types"] = new List<Dictionary<string, string>>
                            {
                                new() { ["id"] = "sa_id", ["title"] = "🇿🇦 South African ID Book" },
                                new() { ["id"] = "sa_id_card", ["title"] = "🪪 SA Smart ID Card" },
                                new() { ["id"] = "passport", ["title"] = "📘 Passport" },
                                new() { ["id"] = "asylum_permit", ["title"] = "📄 Asylum Seeker Permit" }
                            }
                        }
                    };
                }
                else
                {
                    return new FlowResponse
                    {
                        Screen = "UPLOAD_PROOF_OF_ADDRESS",
                        Data = new Dictionary<string, object>
                        {
                            ["poa_types"] = new List<Dictionary<string, string>>
                            {
                                new() { ["id"] = "utility_bill", ["title"] = "⚡ Utility Bill (water/electricity)" },
                                new() { ["id"] = "bank_statement", ["title"] = "🏦 Bank Statement" },
                                new() { ["id"] = "lease_agreement", ["title"] = "📝 Lease / Rental Agreement" },
                                new() { ["id"] = "municipal_account", ["title"] = "🏛️ Municipal Account" },
                                new() { ["id"] = "phone_bill", ["title"] = "📱 Phone / Internet Bill" }
                            }
                        }
                    };
                }

            case "UPLOAD_ID":
                // Save ID document record
                var idDocType = payload.GetValueOrDefault("id_document_type")?.ToString() ?? "ID Document";
                await SaveDocumentRecordAsync(userId, idDocType, "ID Document");

                // Check if proof of address is already uploaded
                var hasPoA = await _context.UserDocuments
                    .AnyAsync(d => d.UserId == userId && !d.IsDeleted &&
                        (d.DocumentType == DocumentType.ProofOfAddress ||
                         d.DocumentType == DocumentType.BankStatement));

                if (!hasPoA)
                {
                    return new FlowResponse
                    {
                        Screen = "UPLOAD_PROOF_OF_ADDRESS",
                        Data = new Dictionary<string, object>
                        {
                            ["poa_types"] = new List<Dictionary<string, string>>
                            {
                                new() { ["id"] = "utility_bill", ["title"] = "⚡ Utility Bill" },
                                new() { ["id"] = "bank_statement", ["title"] = "🏦 Bank Statement" },
                                new() { ["id"] = "lease_agreement", ["title"] = "📝 Lease Agreement" },
                                new() { ["id"] = "municipal_account", ["title"] = "🏛️ Municipal Account" },
                                new() { ["id"] = "phone_bill", ["title"] = "📱 Phone / Internet Bill" }
                            }
                        }
                    };
                }

                return new FlowResponse
                {
                    Screen = "DOCUMENT_STATUS",
                    Data = new Dictionary<string, object>
                    {
                        ["id_doc_status"] = $"✅ {idDocType} – Uploaded",
                        ["poa_doc_status"] = "✅ Already on file",
                        ["verification_note"] = "All documents received! You can proceed with your application.",
                        ["portal_url"] = "https://hohemaweb-development.up.railway.app/profile"
                    }
                };

            case "UPLOAD_PROOF_OF_ADDRESS":
                var poaDocType = payload.GetValueOrDefault("poa_document_type")?.ToString() ?? "Proof of Address";
                await SaveDocumentRecordAsync(userId, poaDocType, "Proof of Address");

                var hasIdDoc = await _context.UserDocuments
                    .AnyAsync(d => d.UserId == userId && !d.IsDeleted &&
                        d.DocumentType == DocumentType.IdDocument);

                return new FlowResponse
                {
                    Screen = "DOCUMENT_STATUS",
                    Data = new Dictionary<string, object>
                    {
                        ["id_doc_status"] = hasIdDoc ? "✅ ID Document – On file" : "❌ ID Document – Still needed",
                        ["poa_doc_status"] = $"✅ {poaDocType} – Uploaded",
                        ["verification_note"] = hasIdDoc
                            ? "All documents received! You can proceed with your application."
                            : "Please also upload your ID document to complete verification.",
                        ["portal_url"] = "https://hohemaweb-development.up.railway.app/profile"
                    }
                };

            default:
                return new FlowResponse { Screen = "DOCUMENT_CHECKLIST", Data = new Dictionary<string, object>() };
        }
    }

    // -------- Affordability Flow --------

    private async Task<FlowResponse> HandleAffordabilityFlowExchangeAsync(string screenId, Dictionary<string, object> payload, string userId)
    {
        switch (screenId)
        {
            case "INCOME_OVERVIEW":
                var incomeAction = payload.GetValueOrDefault("income_action")?.ToString();

                if (incomeAction == "add_new" || incomeAction == "update")
                {
                    return new FlowResponse
                    {
                        Screen = "ADD_INCOME",
                        Data = new Dictionary<string, object>
                        {
                            ["income_type_options"] = new List<Dictionary<string, string>>
                            {
                                new() { ["id"] = "salary", ["title"] = "💼 Salary / Wages" },
                                new() { ["id"] = "part_time", ["title"] = "🕐 Part-time / Casual Work" },
                                new() { ["id"] = "self_employed", ["title"] = "🏪 Self-employed / Business" },
                                new() { ["id"] = "grant", ["title"] = "🏛️ Government Grant (SASSA)" },
                                new() { ["id"] = "pension", ["title"] = "👴 Pension / Retirement" },
                                new() { ["id"] = "other", ["title"] = "📋 Other Income" }
                            },
                            ["frequency_options"] = new List<Dictionary<string, string>>
                            {
                                new() { ["id"] = "monthly", ["title"] = "Monthly" },
                                new() { ["id"] = "weekly", ["title"] = "Weekly" },
                                new() { ["id"] = "fortnightly", ["title"] = "Fortnightly" },
                                new() { ["id"] = "annually", ["title"] = "Annually" }
                            }
                        }
                    };
                }

                // "keep" — move to expenses
                return await GetExpensesScreenDataAsync(userId);

            case "ADD_INCOME":
                // Save new income
                var incomeAmount = decimal.TryParse(payload.GetValueOrDefault("income_amount")?.ToString(), out var inAmt) ? inAmt : 0;
                var frequency = payload.GetValueOrDefault("income_frequency")?.ToString() ?? "monthly";
                var monthlyAmount = frequency switch
                {
                    "weekly" => incomeAmount * 4.33m,
                    "fortnightly" => incomeAmount * 2.17m,
                    "annually" => incomeAmount / 12m,
                    _ => incomeAmount
                };

                var income = new Income
                {
                    UserId = userId,
                    SourceType = payload.GetValueOrDefault("income_source")?.ToString() ?? "Other",
                    Description = payload.GetValueOrDefault("income_type")?.ToString() ?? "other",
                    MonthlyAmount = monthlyAmount,
                    Frequency = frequency,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Incomes.Add(income);
                await _context.SaveChangesAsync();

                return await GetExpensesScreenDataAsync(userId);

            case "EXPENSES_OVERVIEW":
                var expenseAction = payload.GetValueOrDefault("expense_action")?.ToString();

                if (expenseAction == "add_new" || expenseAction == "update")
                {
                    return new FlowResponse
                    {
                        Screen = "ADD_EXPENSE",
                        Data = new Dictionary<string, object>
                        {
                            ["expense_type_options"] = new List<Dictionary<string, string>>
                            {
                                new() { ["id"] = "rent", ["title"] = "🏠 Rent / Bond" },
                                new() { ["id"] = "transport", ["title"] = "🚌 Transport / Fuel" },
                                new() { ["id"] = "groceries", ["title"] = "🛒 Groceries / Food" },
                                new() { ["id"] = "utilities", ["title"] = "⚡ Electricity / Water" },
                                new() { ["id"] = "insurance", ["title"] = "🛡️ Insurance" },
                                new() { ["id"] = "debt_repayment", ["title"] = "💳 Existing Debt Repayments" }
                            },
                            ["frequency_options"] = new List<Dictionary<string, string>>
                            {
                                new() { ["id"] = "monthly", ["title"] = "Monthly" },
                                new() { ["id"] = "weekly", ["title"] = "Weekly" },
                                new() { ["id"] = "fortnightly", ["title"] = "Fortnightly" },
                                new() { ["id"] = "annually", ["title"] = "Annually" }
                            }
                        }
                    };
                }

                // "keep" — calculate affordability
                return await CalculateAndReturnAffordabilityAsync(userId);

            case "ADD_EXPENSE":
                var expenseAmount = decimal.TryParse(payload.GetValueOrDefault("expense_amount")?.ToString(), out var exAmt) ? exAmt : 0;
                var expFrequency = payload.GetValueOrDefault("expense_frequency")?.ToString() ?? "monthly";
                var monthlyExpense = expFrequency switch
                {
                    "weekly" => expenseAmount * 4.33m,
                    "fortnightly" => expenseAmount * 2.17m,
                    "annually" => expenseAmount / 12m,
                    _ => expenseAmount
                };

                var expense = new Expense
                {
                    UserId = userId,
                    Description = payload.GetValueOrDefault("expense_description")?.ToString() ?? "Other",
                    Category = payload.GetValueOrDefault("expense_type")?.ToString() ?? "other",
                    MonthlyAmount = monthlyExpense,
                    Frequency = expFrequency,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Expenses.Add(expense);
                await _context.SaveChangesAsync();

                // Return to expense overview or calculate
                return await CalculateAndReturnAffordabilityAsync(userId);

            default:
                return new FlowResponse { Screen = "INCOME_OVERVIEW", Data = new Dictionary<string, object>() };
        }
    }

    private async Task<FlowResponse> GetExpensesScreenDataAsync(string userId)
    {
        var expenses = await _context.Expenses
            .Where(e => e.UserId == userId)
            .ToListAsync();

        var totalExpenses = expenses.Sum(e => e.MonthlyAmount);
        var totalIncome = await _context.Incomes
            .Where(i => i.UserId == userId)
            .SumAsync(i => i.MonthlyAmount);

        var expenseSummary = expenses.Any()
            ? "💳 Current expenses on file:\n" + string.Join("\n", expenses.Select(e => $"• {e.Description}: R{e.MonthlyAmount:N0}")) + $"\nTotal: R{totalExpenses:N0}"
            : "💳 No expense information on file yet.";

        return new FlowResponse
        {
            Screen = "EXPENSES_OVERVIEW",
            Data = new Dictionary<string, object>
            {
                ["has_existing_expenses"] = expenses.Any(),
                ["existing_expenses_summary"] = expenseSummary,
                ["total_expenses"] = $"R{totalExpenses:N0}",
                ["total_income"] = $"R{totalIncome:N0}",
                ["expense_action_options"] = new List<Dictionary<string, string>>
                {
                    new() { ["id"] = "keep", ["title"] = "✅ Expenses are correct" },
                    new() { ["id"] = "update", ["title"] = "✏️ I want to update them" },
                    new() { ["id"] = "add_new", ["title"] = "➕ Add a new expense" }
                }
            }
        };
    }

    private async Task<FlowResponse> CalculateAndReturnAffordabilityAsync(string userId)
    {
        var assessment = await _affordabilityService.CalculateAffordabilityAsync(userId);

        var totalIncome = assessment?.GrossMonthlyIncome ?? 0;
        var totalExpenses = assessment?.TotalMonthlyExpenses ?? 0;
        var disposable = totalIncome - totalExpenses;
        var maxRepayment = disposable * 0.5m; // 50% of disposable income
        var passed = disposable > 0;

        return new FlowResponse
        {
            Screen = "AFFORDABILITY_RESULT",
            Data = new Dictionary<string, object>
            {
                ["total_income"] = $"R{totalIncome:N0}",
                ["total_expenses"] = $"R{totalExpenses:N0}",
                ["disposable_income"] = $"R{disposable:N0}",
                ["max_repayment"] = $"R{maxRepayment:N0}",
                ["affordability_status"] = passed ? "✅ You qualify for a loan" : "⚠️ Affordability concerns",
                ["status_detail"] = passed
                    ? $"Based on your disposable income of R{disposable:N0}, you can afford monthly repayments up to R{maxRepayment:N0}"
                    : "Your expenses exceed your income. Please review your financial information.",
                ["affordability_passed"] = passed,
                ["web_url"] = "https://hohemaweb-development.up.railway.app/affordability"
            }
        };
    }

    // -------- Loan Application Flow --------

    private async Task<FlowResponse> HandleLoanApplicationFlowExchangeAsync(string screenId, Dictionary<string, object> payload, string userId)
    {
        var draftId = payload.GetValueOrDefault("draft_id")?.ToString();
        LoanApplication? draft = null;

        if (!string.IsNullOrEmpty(draftId) && Guid.TryParse(draftId, out var parsedId))
        {
            draft = await _context.LoanApplications.FindAsync(parsedId);
        }

        switch (screenId)
        {
            case "LOAN_AMOUNT":
                var amount = decimal.TryParse(payload.GetValueOrDefault("loan_amount")?.ToString(), out var amt) ? amt : 0;

                if (amount < 500 || amount > 50000)
                {
                    return new FlowResponse
                    {
                        Screen = "LOAN_AMOUNT",
                        Data = new Dictionary<string, object>
                        {
                            ["min_amount"] = "R500",
                            ["max_amount"] = "R50,000",
                            ["draft_id"] = "",
                            ["existing_amount"] = "",
                            ["is_resume"] = false,
                            ["resume_message"] = "⚠️ Please enter an amount between R500 and R50,000"
                        }
                    };
                }

                // Create or update draft
                if (draft == null)
                {
                    draft = new LoanApplication
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Status = LoanStatus.Draft,
                        ChannelOrigin = LoanApplicationChannel.WhatsApp,
                        WhatsAppInitiatedDate = DateTime.UtcNow,
                        ApplicationDate = DateTime.UtcNow,
                        CurrentStep = 0,
                        StepData = new Dictionary<string, object>()
                    };
                    _context.LoanApplications.Add(draft);
                }

                draft.Amount = amount;
                draft.CurrentStep = 1;
                draft.StepData!["amount"] = amount;
                await _context.SaveChangesAsync();

                // Calculate estimated monthly payments for each term
                var termOptions = new List<Dictionary<string, string>>();
                foreach (var term in new[] { 6, 12, 24, 36 })
                {
                    var monthlyRate = 0.20m / 12; // 20% per annum
                    var monthlyPayment = amount * monthlyRate *
                        (decimal)Math.Pow((double)(1 + monthlyRate), term) /
                        ((decimal)Math.Pow((double)(1 + monthlyRate), term) - 1);
                    termOptions.Add(new Dictionary<string, string>
                    {
                        ["id"] = term.ToString(),
                        ["title"] = $"{term} months – ~R{monthlyPayment:N0}/mo"
                    });
                }

                return new FlowResponse
                {
                    Screen = "TERM_SELECTION",
                    Data = new Dictionary<string, object>
                    {
                        ["loan_amount"] = $"R{amount:N0}",
                        ["term_options"] = termOptions,
                        ["draft_id"] = draft.Id.ToString()
                    }
                };

            case "TERM_SELECTION":
                var termMonths = int.TryParse(payload.GetValueOrDefault("term_months")?.ToString(), out var tm) ? tm : 12;

                if (draft == null) throw new InvalidOperationException("Draft not found");

                draft.TermMonths = termMonths;
                var mRate = 0.20m / 12;
                var mp = draft.Amount * mRate *
                    (decimal)Math.Pow((double)(1 + mRate), termMonths) /
                    ((decimal)Math.Pow((double)(1 + mRate), termMonths) - 1);

                draft.MonthlyPayment = Math.Round(mp, 2);
                draft.InterestRate = 0.20m;
                draft.TotalAmount = draft.MonthlyPayment * termMonths;
                draft.CurrentStep = 2;
                draft.StepData!["termMonths"] = termMonths;
                await _context.SaveChangesAsync();

                var purposes = new List<Dictionary<string, string>>
                {
                    new() { ["id"] = "transport", ["title"] = "🚌 Transport" },
                    new() { ["id"] = "groceries", ["title"] = "🛒 Groceries" },
                    new() { ["id"] = "expenses", ["title"] = "💳 Day-to-day expenses" },
                    new() { ["id"] = "airtime", ["title"] = "📱 Airtime & data" },
                    new() { ["id"] = "utilities", ["title"] = "⚡ Utilities" },
                    new() { ["id"] = "medical", ["title"] = "🏥 Medical expenses" }
                };

                return new FlowResponse
                {
                    Screen = "LOAN_PURPOSE",
                    Data = new Dictionary<string, object>
                    {
                        ["loan_amount"] = $"R{draft.Amount:N0}",
                        ["term_months"] = $"{termMonths} months",
                        ["purposes"] = purposes,
                        ["draft_id"] = draft.Id.ToString()
                    }
                };

            case "LOAN_PURPOSE":
                if (draft == null) throw new InvalidOperationException("Draft not found");

                draft.Purpose = payload.GetValueOrDefault("purpose")?.ToString() ?? "Other";
                draft.Notes = payload.GetValueOrDefault("purpose_description")?.ToString();
                draft.CurrentStep = 3;
                draft.StepData!["purpose"] = draft.Purpose;
                await _context.SaveChangesAsync();

                // Fetch affordability and build review screen
                var afford = await _affordabilityService.CalculateAffordabilityAsync(userId);
                var canAfford = afford != null && (afford.GrossMonthlyIncome - afford.TotalMonthlyExpenses) >= draft.MonthlyPayment;

                draft.PassedAffordabilityCheck = canAfford;
                draft.AffordabilityStatus = canAfford ? "Passed" : "Warning";
                draft.IsAffordabilityIncluded = true;
                await _context.SaveChangesAsync();

                return new FlowResponse
                {
                    Screen = "AFFORDABILITY_REVIEW",
                    Data = new Dictionary<string, object>
                    {
                        ["loan_amount"] = $"R{draft.Amount:N0}",
                        ["monthly_payment"] = $"R{draft.MonthlyPayment:N0}",
                        ["total_income"] = $"R{afford?.GrossMonthlyIncome ?? 0:N0}",
                        ["total_expenses"] = $"R{afford?.TotalMonthlyExpenses ?? 0:N0}",
                        ["disposable_income"] = $"R{(afford?.GrossMonthlyIncome ?? 0) - (afford?.TotalMonthlyExpenses ?? 0):N0}",
                        ["affordability_status"] = canAfford
                            ? "✅ Affordability check passed"
                            : "⚠️ This loan may strain your budget",
                        ["can_afford"] = canAfford,
                        ["draft_id"] = draft.Id.ToString()
                    }
                };

            case "AFFORDABILITY_REVIEW":
                if (draft == null) throw new InvalidOperationException("Draft not found");

                draft.CurrentStep = 4;
                draft.StepData!["affordabilityConfirmed"] = true;
                await _context.SaveChangesAsync();

                var totalInterest = draft.TotalAmount - draft.Amount;

                return new FlowResponse
                {
                    Screen = "TERMS_PREVIEW",
                    Data = new Dictionary<string, object>
                    {
                        ["loan_amount"] = $"R{draft.Amount:N0}",
                        ["interest_rate"] = $"{draft.InterestRate * 100:N0}% per annum",
                        ["term_months"] = $"{draft.TermMonths} months",
                        ["monthly_payment"] = $"R{draft.MonthlyPayment:N2}",
                        ["total_interest"] = $"R{totalInterest:N2}",
                        ["total_repayment"] = $"R{draft.TotalAmount:N2}",
                        ["initiation_fee"] = "R0",
                        ["service_fee"] = "R0",
                        ["draft_id"] = draft.Id.ToString()
                    }
                };

            case "TERMS_PREVIEW":
                if (draft == null) throw new InvalidOperationException("Draft not found");

                draft.CurrentStep = 5;
                draft.StepData!["termsAccepted"] = true;
                await _context.SaveChangesAsync();

                var user = await _userManager.FindByIdAsync(userId);
                var bankOpts = new List<Dictionary<string, string>>
                {
                    new() { ["id"] = "fnb", ["title"] = "FNB" },
                    new() { ["id"] = "standard_bank", ["title"] = "Standard Bank" },
                    new() { ["id"] = "absa", ["title"] = "ABSA" },
                    new() { ["id"] = "nedbank", ["title"] = "Nedbank" },
                    new() { ["id"] = "capitec", ["title"] = "Capitec" },
                    new() { ["id"] = "african_bank", ["title"] = "African Bank" }
                };

                return new FlowResponse
                {
                    Screen = "BANK_DETAILS",
                    Data = new Dictionary<string, object>
                    {
                        ["bank_options"] = bankOpts,
                        ["existing_bank"] = user?.BankName ?? "",
                        ["existing_account"] = user?.AccountNumber ?? "",
                        ["existing_holder"] = $"{user?.FirstName} {user?.LastName}",
                        ["draft_id"] = draft.Id.ToString()
                    }
                };

            case "BANK_DETAILS":
                if (draft == null) throw new InvalidOperationException("Draft not found");

                draft.BankName = payload.GetValueOrDefault("bank_name")?.ToString();
                draft.AccountNumber = payload.GetValueOrDefault("account_number")?.ToString();
                draft.AccountHolderName = payload.GetValueOrDefault("account_holder")?.ToString();
                draft.CurrentStep = 6;
                draft.StepData!["bankName"] = draft.BankName ?? "";
                draft.StepData["accountNumber"] = draft.AccountNumber ?? "";
                draft.StepData["accountHolderName"] = draft.AccountHolderName ?? "";
                await _context.SaveChangesAsync();

                var purposeDisplay = draft.Purpose?.Replace("_", " ") ?? "Other";
                purposeDisplay = char.ToUpper(purposeDisplay[0]) + purposeDisplay[1..];
                var maskedAcct = draft.AccountNumber != null && draft.AccountNumber.Length > 4
                    ? $"****{draft.AccountNumber[^4..]}"
                    : "****";

                return new FlowResponse
                {
                    Screen = "REVIEW_SIGN",
                    Data = new Dictionary<string, object>
                    {
                        ["loan_amount"] = $"R{draft.Amount:N0}",
                        ["term_months"] = $"{draft.TermMonths} months",
                        ["purpose"] = purposeDisplay,
                        ["monthly_payment"] = $"R{draft.MonthlyPayment:N2}",
                        ["total_repayment"] = $"R{draft.TotalAmount:N2}",
                        ["bank_name"] = draft.BankName ?? "Not set",
                        ["account_masked"] = maskedAcct,
                        ["affordability_status"] = draft.PassedAffordabilityCheck == true ? "✅ Passed" : "⚠️ Warning",
                        ["draft_id"] = draft.Id.ToString()
                    }
                };

            case "REVIEW_SIGN":
                if (draft == null) throw new InvalidOperationException("Draft not found");

                // Submit the application
                draft.Status = LoanStatus.Pending;
                draft.ApplicationDate = DateTime.UtcNow;
                draft.CurrentStep = 99; // Completed
                draft.StepData!["termsAccepted"] = true;
                draft.StepData["debitConsent"] = true;
                draft.StepData["popiaConsent"] = true;
                draft.StepData["submittedVia"] = "WhatsApp Flow";
                draft.StepData["submittedAt"] = DateTime.UtcNow.ToString("o");
                await _context.SaveChangesAsync();

                _logger.LogInformation("Loan application {AppId} submitted via WhatsApp Flow for user {UserId}", draft.Id, userId);

                return new FlowResponse
                {
                    Screen = "APPLICATION_SUCCESS",
                    Data = new Dictionary<string, object>
                    {
                        ["application_id"] = $"LA-{draft.Id.ToString()[..8].ToUpper()}",
                        ["loan_amount"] = $"R{draft.Amount:N0}",
                        ["monthly_payment"] = $"R{draft.MonthlyPayment:N2}",
                        ["term_months"] = $"{draft.TermMonths} months",
                        ["tracking_url"] = "https://hohemaweb-development.up.railway.app/loans"
                    }
                };

            default:
                return new FlowResponse { Screen = "LOAN_AMOUNT", Data = new Dictionary<string, object>() };
        }
    }

    // ==================== FLOW COMPLETION ====================

    public async Task HandleFlowCompletionAsync(string flowId, Dictionary<string, object> payload, string userId)
    {
        _logger.LogInformation("Flow completed: {FlowId} for user {UserId}", flowId, userId);

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        var phoneNumber = user.PhoneNumber ?? "";

        switch (flowId)
        {
            case FLOW_PROFILE_COMPLETION:
                // Profile done → check what's next
                await _whatsAppService.SendMessageAsync(phoneNumber,
                    "✅ Profile completed successfully!\n\n" +
                    "Let me check what else we need...");
                await CheckReadinessAndGuideAsync(phoneNumber, user);
                break;

            case FLOW_DOCUMENT_UPLOAD:
                await _whatsAppService.SendMessageAsync(phoneNumber,
                    "📄 Documents received!\n\n" +
                    "Checking your application readiness...");
                await CheckReadinessAndGuideAsync(phoneNumber, user);
                break;

            case FLOW_AFFORDABILITY:
                await _whatsAppService.SendMessageAsync(phoneNumber,
                    "📊 Affordability assessment complete!\n\n" +
                    "Let me check if you're ready to apply...");
                await CheckReadinessAndGuideAsync(phoneNumber, user);
                break;

            case FLOW_LOAN_APPLICATION:
                await _whatsAppService.SendMessageAsync(phoneNumber,
                    "🎉 Your application has been submitted!\n\n" +
                    "Our team will review it and get back to you within 24 hours.\n\n" +
                    "Reply STATUS anytime to check your application status.");
                break;
        }
    }

    // ==================== HELPER METHODS ====================

    private async Task<ApplicationUser?> FindUserByPhoneAsync(string phoneNumber)
    {
        var cleanPhone = phoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "");

        return await _userManager.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber != null &&
                u.PhoneNumber.Replace("+", "").Replace(" ", "").Replace("-", "") == cleanPhone);
    }

    private async Task HandleUnregisteredUserAsync(string phoneNumber, string normalizedMessage)
    {
        await _whatsAppService.SendMessageAsync(phoneNumber,
            "Welcome to Ho Hema Loans! 👋\n\n" +
            "To apply for a loan via WhatsApp, you first need to register.\n\n" +
            "🌐 Register at: https://hohemaweb-development.up.railway.app/register\n\n" +
            "Use the same phone number you're messaging from so we can link your account.\n\n" +
            "Already registered? Make sure your phone number matches your account.");
    }

    private async Task SendWelcomeAndOptionsAsync(string phoneNumber, ApplicationUser user)
    {
        var readiness = await AssessReadinessAsync(user);

        var statusLine = readiness switch
        {
            ApplicationReadiness.ProfileIncomplete => "⚠️ Profile incomplete",
            ApplicationReadiness.DocumentsMissing => "⚠️ Documents needed",
            ApplicationReadiness.AffordabilityNeeded => "⚠️ Affordability assessment needed",
            ApplicationReadiness.ReadyToApply => "✅ Ready to apply!",
            ApplicationReadiness.ApplicationInProgress => "📝 Application in progress",
            _ => ""
        };

        var buttons = new List<WhatsAppButton>
        {
            new() { Id = "cmd_apply", Title = readiness == ApplicationReadiness.ApplicationInProgress ? "Continue" : "Apply" },
            new() { Id = "cmd_status", Title = "Check Status" },
            new() { Id = "cmd_help", Title = "Help" }
        };

        await _whatsAppService.SendInteractiveButtonsAsync(phoneNumber,
            $"Hi {user.FirstName}! 👋\n\n" +
            $"Welcome to Ho Hema Loans.\n\n" +
            $"Account Status: {statusLine}\n\n" +
            "What would you like to do?",
            buttons);
    }

    private async Task SendMainMenuAsync(string phoneNumber, ApplicationUser user)
    {
        var sections = new List<WhatsAppListSection>
        {
            new()
            {
                Title = "Loan Options",
                Rows = new List<WhatsAppListRow>
                {
                    new() { Id = "cmd_apply", Title = "Apply for a Loan", Description = "Start or continue an application" },
                    new() { Id = "cmd_status", Title = "Check Loan Status", Description = "View your applications" },
                    new() { Id = "cmd_balance", Title = "Loan Balance", Description = "Check active loan balances" }
                }
            },
            new()
            {
                Title = "Account",
                Rows = new List<WhatsAppListRow>
                {
                    new() { Id = "cmd_profile", Title = "Update Profile", Description = "Edit your personal details" },
                    new() { Id = "cmd_docs", Title = "Upload Documents", Description = "ID, proof of address" },
                    new() { Id = "cmd_web", Title = "Visit Website", Description = "Full features on the web" }
                }
            }
        };

        await _whatsAppService.SendInteractiveListAsync(phoneNumber,
            $"🏠 Ho Hema Loans Menu\n\nHi {user.FirstName}, how can we help?",
            "View Options",
            sections);
    }

    private async Task SendLoanStatusAsync(string phoneNumber, string userId)
    {
        var applications = await _context.LoanApplications
            .Where(la => la.UserId == userId)
            .OrderByDescending(la => la.ApplicationDate)
            .Take(5)
            .ToListAsync();

        if (!applications.Any())
        {
            await _whatsAppService.SendMessageAsync(phoneNumber,
                "📊 No loan applications found.\n\n" +
                "Reply APPLY to start a new application!");
            return;
        }

        var message = "📊 Your Loan Applications:\n\n";
        foreach (var app in applications)
        {
            var statusEmoji = app.Status switch
            {
                LoanStatus.Draft => "📝",
                LoanStatus.Pending => "⏳",
                LoanStatus.UnderReview => "🔍",
                LoanStatus.Approved => "✅",
                LoanStatus.Rejected => "❌",
                LoanStatus.Disbursed => "💰",
                _ => "📋"
            };

            message += $"{statusEmoji} R{app.Amount:N0} - {app.Status}\n";
            message += $"   Applied: {app.ApplicationDate:dd MMM yyyy}\n\n";
        }

        message += "Reply APPLY to start a new application\n";
        message += "🌐 View details: https://hohemaweb-development.up.railway.app/loans";

        await _whatsAppService.SendMessageAsync(phoneNumber, message);
    }

    private async Task ResumeApplicationAsync(string phoneNumber, ApplicationUser user)
    {
        var draft = await _context.LoanApplications
            .FirstOrDefaultAsync(la => la.UserId == user.Id && la.Status == LoanStatus.Draft);

        if (draft != null)
        {
            await _whatsAppService.SendMessageAsync(phoneNumber,
                $"📝 Resuming your application...\n\n" +
                $"Loan: R{draft.Amount:N0}\n" +
                $"Step: {draft.CurrentStep + 1} of 7\n\n" +
                "Continuing where you left off... 👇");
            await LaunchFlowAsync(phoneNumber, FLOW_LOAN_APPLICATION, user.Id);
        }
        else
        {
            await _whatsAppService.SendMessageAsync(phoneNumber,
                "No draft application found.\n\n" +
                "Reply APPLY to start a new application!");
        }
    }

    private async Task SaveDocumentRecordAsync(string userId, string documentType, string category)
    {
        var docTypeEnum = category.Contains("ID") ? DocumentType.IdDocument
            : category.Contains("Address") ? DocumentType.ProofOfAddress
            : DocumentType.Other;

        var doc = new UserDocument
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DocumentType = docTypeEnum,
            FileName = $"{documentType.Replace(" ", "_")}_whatsapp.pdf",
            FilePath = $"whatsapp-uploads/{userId}/{documentType.Replace(" ", "_")}",
            FileSize = 0,
            ContentType = "application/octet-stream",
            Status = DocumentStatus.Pending,
            UploadedAt = DateTime.UtcNow,
            Notes = $"Uploaded via WhatsApp Flow - {category} - {documentType}"
        };

        _context.UserDocuments.Add(doc);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Saved document record: {DocType} for user {UserId}", documentType, userId);
    }
}
