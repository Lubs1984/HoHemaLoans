using Microsoft.EntityFrameworkCore;
using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;

namespace HoHemaLoans.Api.Services;

/// <summary>
/// Service for managing WhatsApp-based loan application wizard
/// Handles the conversational flow and state management
/// </summary>
public interface IWhatsAppLoanWizardService
{
    Task ProcessUserResponseAsync(string phoneNumber, string userId, string messageText, WhatsAppSession session);
    Task<LoanApplication?> GetActiveDraftAsync(string userId);
    Task<LoanApplication> CreateDraftApplicationAsync(string userId, string phoneNumber);
}

public class WhatsAppLoanWizardService : IWhatsAppLoanWizardService
{
    private readonly ApplicationDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IAffordabilityService _affordabilityService;
    private readonly ILogger<WhatsAppLoanWizardService> _logger;

    public WhatsAppLoanWizardService(
        ApplicationDbContext context,
        IWhatsAppService whatsAppService,
        IAffordabilityService affordabilityService,
        ILogger<WhatsAppLoanWizardService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _affordabilityService = affordabilityService;
        _logger = logger;
    }

    /// <summary>
    /// Process user's text response based on current session state
    /// </summary>
    public async Task ProcessUserResponseAsync(string phoneNumber, string userId, string messageText, WhatsAppSession session)
    {
        var normalizedMessage = messageText.Trim().ToUpper();

        // Get current draft application if exists
        var draftApp = session.DraftApplicationId.HasValue
            ? await _context.LoanApplications.FindAsync(session.DraftApplicationId.Value)
            : null;

        // Handle commands
        if (normalizedMessage == "YES")
        {
            await HandleYesCommandAsync(phoneNumber, userId, session, draftApp);
        }
        else if (normalizedMessage == "NO")
        {
            await HandleNoCommandAsync(phoneNumber, session);
        }
        else if (normalizedMessage == "NEW")
        {
            await HandleNewApplicationAsync(phoneNumber, userId, session);
        }
        else if (normalizedMessage == "BALANCE")
        {
            await HandleBalanceInquiryAsync(phoneNumber, userId);
        }
        else if (normalizedMessage == "HELP" || normalizedMessage == "MENU")
        {
            await SendMainMenuAsync(phoneNumber);
        }
        else if (draftApp != null)
        {
            // Process wizard step response
            await ProcessWizardStepAsync(phoneNumber, userId, draftApp, normalizedMessage, messageText);
        }
        else
        {
            // Unknown command - send help
            await _whatsAppService.SendMessageAsync(
                phoneNumber,
                "I didn't understand that. 🤔\n\n" +
                "Reply with:\n" +
                "• YES - to continue\n" +
                "• NEW - to start a new loan application\n" +
                "• BALANCE - to check your loan balance\n" +
                "• HELP - to see all options"
            );
        }
    }

    /// <summary>
    /// Handle YES command - continue with existing application or start wizard
    /// </summary>
    private async Task HandleYesCommandAsync(string phoneNumber, string userId, WhatsAppSession session, LoanApplication? draftApp)
    {
        if (draftApp == null)
        {
            // No draft exists, create new one
            draftApp = await CreateDraftApplicationAsync(userId, phoneNumber);
            session.DraftApplicationId = draftApp.Id;
            await _context.SaveChangesAsync();
        }

        // Start or resume wizard
        await StartWizardStepAsync(phoneNumber, userId, draftApp);
    }

    /// <summary>
    /// Handle NO command - cancel or decline
    /// </summary>
    private async Task HandleNoCommandAsync(string phoneNumber, WhatsAppSession session)
    {
        session.SessionStatus = "Cancelled";
        session.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _whatsAppService.SendMessageAsync(
            phoneNumber,
            "No problem! 👍\n\n" +
            "If you change your mind, just send me a message anytime.\n\n" +
            "Have a great day! ✨"
        );
    }

    /// <summary>
    /// Handle NEW command - start fresh application
    /// </summary>
    private async Task HandleNewApplicationAsync(string phoneNumber, string userId, WhatsAppSession session)
    {
        // Archive old draft if exists
        if (session.DraftApplicationId.HasValue)
        {
            var oldDraft = await _context.LoanApplications.FindAsync(session.DraftApplicationId.Value);
            if (oldDraft != null && oldDraft.Status == LoanStatus.Draft)
            {
                oldDraft.Notes = (oldDraft.Notes ?? "") + " [Abandoned for new application]";
                await _context.SaveChangesAsync();
            }
        }

        // Create new draft
        var newDraft = await CreateDraftApplicationAsync(userId, phoneNumber);
        session.DraftApplicationId = newDraft.Id;
        session.LastUpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _whatsAppService.SendMessageAsync(
            phoneNumber,
            "Great! Let's start a fresh loan application. 🆕\n\n" +
            "This will only take a few minutes!"
        );

        await StartWizardStepAsync(phoneNumber, userId, newDraft);
    }

    /// <summary>
    /// Start or resume the wizard from current step
    /// </summary>
    private async Task StartWizardStepAsync(string phoneNumber, string userId, LoanApplication draftApp)
    {
        switch (draftApp.CurrentStep)
        {
            case 0: // Loan Amount
                await AskLoanAmountAsync(phoneNumber, userId);
                break;
            case 1: // Term Months
                await AskTermMonthsAsync(phoneNumber);
                break;
            case 2: // Purpose
                await AskLoanPurposeAsync(phoneNumber);
                break;
            case 3: // Affordability Review
                await ShowAffordabilityAsync(phoneNumber, userId, draftApp);
                break;
            case 4: // Bank Details
                await AskBankDetailsAsync(phoneNumber);
                break;
            case 5: // Confirmation
                await ShowFinalConfirmationAsync(phoneNumber, draftApp);
                break;
            default:
                await AskLoanAmountAsync(phoneNumber, userId);
                break;
        }
    }

    /// <summary>
    /// Process wizard step responses
    /// </summary>
    private async Task ProcessWizardStepAsync(string phoneNumber, string userId, LoanApplication draftApp, string normalizedMessage, string originalMessage)
    {
        switch (draftApp.CurrentStep)
        {
            case 0: // Loan Amount input
                await ProcessLoanAmountAsync(phoneNumber, userId, draftApp, originalMessage);
                break;
            case 1: // Term selection
                await ProcessTermMonthsAsync(phoneNumber, draftApp, originalMessage);
                break;
            case 2: // Purpose selection
                await ProcessLoanPurposeAsync(phoneNumber, draftApp, originalMessage);
                break;
            case 3: // Affordability confirmation
                if (normalizedMessage == "CONTINUE")
                {
                    draftApp.CurrentStep = 4;
                    await _context.SaveChangesAsync();
                    await AskBankDetailsAsync(phoneNumber);
                }
                break;
            case 4: // Bank details
                await ProcessBankDetailsAsync(phoneNumber, draftApp, originalMessage);
                break;
            case 5: // Final confirmation
                if (normalizedMessage == "CONFIRM")
                {
                    await SubmitApplicationAsync(phoneNumber, draftApp);
                }
                break;
        }
    }

    // ==================== WIZARD STEPS ====================

    /// <summary>
    /// Step 0: Ask for loan amount
    /// </summary>
    private async Task AskLoanAmountAsync(string phoneNumber, string userId)
    {
        // Get affordability to show max amount
        var assessment = await _affordabilityService.CalculateAffordabilityAsync(userId);
        
        var maxAmount = assessment?.MaxRecommendedLoanAmount ?? 10000m;

        var buttons = new List<WhatsAppButton>
        {
            new() { Id = "amt_1000", Title = "R1,000" },
            new() { Id = "amt_5000", Title = "R5,000" },
            new() { Id = "amt_custom", Title = "Custom Amount" }
        };

        await _whatsAppService.SendInteractiveButtonsAsync(
            phoneNumber,
            $"💰 How much would you like to borrow?\n\n" +
            $"Based on your profile, you can borrow up to R{maxAmount:N2}\n\n" +
            $"Choose a quick option or reply with your amount (e.g., \"3500\"):",
            buttons
        );
    }

    /// <summary>
    /// Step 1: Ask for term months
    /// </summary>
    private async Task AskTermMonthsAsync(string phoneNumber)
    {
        var buttons = new List<WhatsAppButton>
        {
            new() { Id = "term_6", Title = "6 months" },
            new() { Id = "term_12", Title = "12 months" },
            new() { Id = "term_24", Title = "24 months" }
        };

        await _whatsAppService.SendInteractiveButtonsAsync(
            phoneNumber,
            "📅 How long do you need to repay the loan?",
            buttons
        );
    }

    /// <summary>
    /// Step 2: Ask for loan purpose
    /// </summary>
    private async Task AskLoanPurposeAsync(string phoneNumber)
    {
        var sections = new List<WhatsAppListSection>
        {
            new()
            {
                Title = "Common Purposes",
                Rows = new List<WhatsAppListRow>
                {
                    new() { Id = "purpose_emergency", Title = "Emergency Expenses", Description = "Unexpected costs" },
                    new() { Id = "purpose_medical", Title = "Medical Expenses", Description = "Healthcare costs" },
                    new() { Id = "purpose_education", Title = "Education", Description = "School fees, supplies" },
                    new() { Id = "purpose_home", Title = "Home Improvement", Description = "Repairs, upgrades" },
                    new() { Id = "purpose_debt", Title = "Debt Consolidation", Description = "Combine debts" },
                    new() { Id = "purpose_business", Title = "Business", Description = "Business needs" },
                    new() { Id = "purpose_transport", Title = "Transport", Description = "Vehicle, travel" },
                    new() { Id = "purpose_other", Title = "Other", Description = "Other purpose" }
                }
            }
        };

        await _whatsAppService.SendInteractiveListAsync(
            phoneNumber,
            "📝 What will you use this loan for?\n\nThis helps us process your application.",
            "Select Purpose",
            sections
        );
    }

    /// <summary>
    /// Step 3: Show affordability assessment
    /// </summary>
    private async Task ShowAffordabilityAsync(string phoneNumber, string userId, LoanApplication draftApp)
    {
        var assessment = await _affordabilityService.CalculateAffordabilityAsync(userId);

        if (assessment == null)
        {
            await _whatsAppService.SendMessageAsync(
                phoneNumber,
                "⚠️ We need to verify your income and expenses first.\n\n" +
                "Please update your profile on our website or contact support."
            );
            return;
        }

        var canAfford = await _affordabilityService.CanAffordLoanAsync(userId, draftApp.Amount, draftApp.TermMonths);

        var message = $"📊 Affordability Assessment\n\n" +
                     $"Loan Amount: R{draftApp.Amount:N2}\n" +
                     $"Term: {draftApp.TermMonths} months\n" +
                     $"Monthly Payment: R{draftApp.MonthlyPayment:N2}\n\n" +
                     $"Your monthly income: R{assessment.GrossMonthlyIncome:N2}\n" +
                     $"Your monthly expenses: R{assessment.TotalMonthlyExpenses:N2}\n" +
                     $"Net available: R{assessment.NetMonthlyIncome:N2}\n\n";

        if (canAfford)
        {
            message += "✅ Good news! This loan is affordable for you.\n\n" +
                      "Reply CONTINUE to proceed with your application.";
        }
        else
        {
            message += "⚠️ This loan amount may strain your budget.\n\n" +
                      $"We recommend a maximum of R{assessment.MaxRecommendedLoanAmount:N2}\n\n" +
                      "Reply CONTINUE to proceed anyway, or NEW to start over with a lower amount.";
        }

        draftApp.PassedAffordabilityCheck = canAfford;
        draftApp.AffordabilityStatus = canAfford ? "Affordable" : "LimitedAffordability";
        await _context.SaveChangesAsync();

        await _whatsAppService.SendMessageAsync(phoneNumber, message);
    }

    /// <summary>
    /// Step 4: Ask for bank details
    /// </summary>
    private async Task AskBankDetailsAsync(string phoneNumber)
    {
        await _whatsAppService.SendMessageAsync(
            phoneNumber,
            "🏦 Bank Details\n\n" +
            "Please provide your bank details in this format:\n\n" +
            "BankName|AccountNumber|AccountHolder\n\n" +
            "Example:\n" +
            "FNB|1234567890|John Doe"
        );
    }

    /// <summary>
    /// Step 5: Show final confirmation
    /// </summary>
    private async Task ShowFinalConfirmationAsync(string phoneNumber, LoanApplication draftApp)
    {
        var message = $"✅ Application Summary\n\n" +
                     $"Loan Amount: R{draftApp.Amount:N2}\n" +
                     $"Term: {draftApp.TermMonths} months\n" +
                     $"Monthly Payment: R{draftApp.MonthlyPayment:N2}\n" +
                     $"Total Repayment: R{draftApp.TotalAmount:N2}\n" +
                     $"Interest Rate: {draftApp.InterestRate * 100:N2}%\n\n" +
                     $"Purpose: {draftApp.Purpose}\n" +
                     $"Bank: {draftApp.BankName}\n" +
                     $"Account: {draftApp.AccountNumber}\n\n" +
                     $"⚠️ Important: By confirming, you agree to the loan terms and conditions.\n\n" +
                     $"Reply CONFIRM to submit your application for review.";

        await _whatsAppService.SendMessageAsync(phoneNumber, message);
    }

    // ==================== PROCESS RESPONSES ====================

    private async Task ProcessLoanAmountAsync(string phoneNumber, string userId, LoanApplication draftApp, string message)
    {
        decimal amount = 0;

        // Check if it's a button response
        if (message.StartsWith("amt_"))
        {
            amount = message switch
            {
                "amt_1000" => 1000m,
                "amt_5000" => 5000m,
                _ => 0
            };

            if (amount == 0)
            {
                await AskLoanAmountAsync(phoneNumber, userId);
                return;
            }
        }
        else if (!decimal.TryParse(message.Replace("R", "").Replace(",", "").Trim(), out amount) || amount <= 0)
        {
            await _whatsAppService.SendMessageAsync(
                phoneNumber,
                "❌ Please enter a valid amount (e.g., 5000 or R5,000)"
            );
            return;
        }

        // Validate amount
        if (amount < 500)
        {
            await _whatsAppService.SendMessageAsync(
                phoneNumber,
                "❌ Minimum loan amount is R500. Please enter a higher amount."
            );
            return;
        }

        if (amount > 50000)
        {
            await _whatsAppService.SendMessageAsync(
                phoneNumber,
                "❌ Maximum loan amount is R50,000. Please enter a lower amount."
            );
            return;
        }

        // Save amount and move to next step
        draftApp.Amount = amount;
        draftApp.CurrentStep = 1;
        await _context.SaveChangesAsync();

        await _whatsAppService.SendMessageAsync(
            phoneNumber,
            $"✅ Loan amount: R{amount:N2}"
        );

        await AskTermMonthsAsync(phoneNumber);
    }

    private async Task ProcessTermMonthsAsync(string phoneNumber, LoanApplication draftApp, string message)
    {
        int termMonths = 0;

        if (message.StartsWith("term_"))
        {
            termMonths = message switch
            {
                "term_6" => 6,
                "term_12" => 12,
                "term_24" => 24,
                _ => 0
            };
        }
        else if (!int.TryParse(message, out termMonths) || termMonths <= 0)
        {
            await _whatsAppService.SendMessageAsync(
                phoneNumber,
                "❌ Please select 6, 12, or 24 months"
            );
            return;
        }

        if (!new[] { 6, 12, 24, 36 }.Contains(termMonths))
        {
            await _whatsAppService.SendMessageAsync(
                phoneNumber,
                "❌ Please choose 6, 12, or 24 months"
            );
            return;
        }

        // Calculate loan details
        draftApp.TermMonths = termMonths;
        draftApp.InterestRate = 0.20m; // 20% per annum
        
        var monthlyInterestRate = draftApp.InterestRate / 12;
        var monthlyPayment = draftApp.Amount * monthlyInterestRate * 
            (decimal)Math.Pow((double)(1 + monthlyInterestRate), termMonths) /
            ((decimal)Math.Pow((double)(1 + monthlyInterestRate), termMonths) - 1);

        draftApp.MonthlyPayment = Math.Round(monthlyPayment, 2);
        draftApp.TotalAmount = draftApp.MonthlyPayment * termMonths;
        draftApp.CurrentStep = 2;
        await _context.SaveChangesAsync();

        await _whatsAppService.SendMessageAsync(
            phoneNumber,
            $"✅ Term: {termMonths} months\n" +
            $"💵 Monthly payment: R{draftApp.MonthlyPayment:N2}"
        );

        await AskLoanPurposeAsync(phoneNumber);
    }

    private async Task ProcessLoanPurposeAsync(string phoneNumber, LoanApplication draftApp, string message)
    {
        string purpose = "Other";

        if (message.StartsWith("purpose_"))
        {
            purpose = message.Replace("purpose_", "").Replace("_", " ");
            purpose = char.ToUpper(purpose[0]) + purpose[1..];
        }
        else
        {
            purpose = message.Length > 50 ? message[..50] : message;
        }

        draftApp.Purpose = purpose;
        draftApp.CurrentStep = 3;
        await _context.SaveChangesAsync();

        await _whatsAppService.SendMessageAsync(
            phoneNumber,
            $"✅ Purpose: {purpose}"
        );

        await ShowAffordabilityAsync(phoneNumber, draftApp.UserId, draftApp);
    }

    private async Task ProcessBankDetailsAsync(string phoneNumber, LoanApplication draftApp, string message)
    {
        var parts = message.Split('|');
        
        if (parts.Length != 3)
        {
            await _whatsAppService.SendMessageAsync(
                phoneNumber,
                "❌ Invalid format. Please use:\n" +
                "BankName|AccountNumber|AccountHolder\n\n" +
                "Example: FNB|1234567890|John Doe"
            );
            return;
        }

        draftApp.BankName = parts[0].Trim();
        draftApp.AccountNumber = parts[1].Trim();
        draftApp.AccountHolderName = parts[2].Trim();
        draftApp.CurrentStep = 5;
        await _context.SaveChangesAsync();

        await _whatsAppService.SendMessageAsync(
            phoneNumber,
            "✅ Bank details saved!"
        );

        await ShowFinalConfirmationAsync(phoneNumber, draftApp);
    }

    private async Task SubmitApplicationAsync(string phoneNumber, LoanApplication draftApp)
    {
        draftApp.Status = LoanStatus.Pending;
        draftApp.ApplicationDate = DateTime.UtcNow;
        draftApp.CurrentStep = 99; // Completed
        await _context.SaveChangesAsync();

        await _whatsAppService.SendMessageAsync(
            phoneNumber,
            "🎉 Application Submitted!\n\n" +
            $"Application ID: {draftApp.Id}\n\n" +
            "Your application has been submitted for review.\n" +
            "Our team will review it within 24-48 hours.\n\n" +
            "We'll notify you once it's approved! 🚀\n\n" +
            "Thank you for choosing Ho Hema Loans! ✨"
        );
    }

    // ==================== BALANCE & MENU ====================

    private async Task HandleBalanceInquiryAsync(string phoneNumber, string userId)
    {
        var approvedLoans = await _context.LoanApplications
            .Where(la => la.UserId == userId && 
                   (la.Status == LoanStatus.Approved || la.Status == LoanStatus.Disbursed))
            .OrderByDescending(la => la.ApprovalDate)
            .ToListAsync();

        if (!approvedLoans.Any())
        {
            await _whatsAppService.SendMessageAsync(
                phoneNumber,
                "📊 Balance Inquiry\n\n" +
                "You don't have any active loans at the moment.\n\n" +
                "Reply NEW to apply for a loan! 💰"
            );
            return;
        }

        var message = "📊 Your Loan Balance\n\n";

        foreach (var loan in approvedLoans.Take(3))
        {
            message += $"Loan: R{loan.Amount:N2}\n" +
                      $"Status: {loan.Status}\n" +
                      $"Monthly: R{loan.MonthlyPayment:N2}\n" +
                      $"Remaining: R{loan.TotalAmount:N2}\n" +
                      $"---\n";
        }

        message += "\nFor detailed statements, visit our website or contact support.";

        await _whatsAppService.SendMessageAsync(phoneNumber, message);
    }

    private async Task SendMainMenuAsync(string phoneNumber)
    {
        var buttons = new List<WhatsAppButton>
        {
            new() { Id = "menu_new", Title = "Apply for Loan" },
            new() { Id = "menu_balance", Title = "Check Balance" },
            new() { Id = "menu_support", Title = "Contact Support" }
        };

        await _whatsAppService.SendInteractiveButtonsAsync(
            phoneNumber,
            "🏠 Ho Hema Loans Main Menu\n\n" +
            "What would you like to do today?",
            buttons
        );
    }

    // ==================== HELPERS ====================

    public async Task<LoanApplication?> GetActiveDraftAsync(string userId)
    {
        return await _context.LoanApplications
            .FirstOrDefaultAsync(la => la.UserId == userId && la.Status == LoanStatus.Draft);
    }

    public async Task<LoanApplication> CreateDraftApplicationAsync(string userId, string phoneNumber)
    {
        var draft = new LoanApplication
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
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created draft application {AppId} for user {UserId} via WhatsApp", 
            draft.Id, userId);

        return draft;
    }
}
