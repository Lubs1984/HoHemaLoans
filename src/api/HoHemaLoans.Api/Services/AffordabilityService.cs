using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HoHemaLoans.Api.Services;

public interface IAffordabilityService
{
    Task<AffordabilityAssessment> CalculateAffordabilityAsync(string userId);
    Task<decimal> GetMaxRecommendedLoanAmountAsync(string userId);
    Task<bool> CanAffordLoanAsync(string userId, decimal loanAmount, decimal monthlyPayment);
    Task<decimal> GetMaxAffordableLoanAsync(string userId, decimal interestRate, int termMonths);
}

public class AffordabilityService : IAffordabilityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AffordabilityService> _logger;

    // Business rules and thresholds
    private const decimal MaxDebtToIncomeRatio = 0.35m; // 35% max per NCA guidelines
    private const decimal ConservativeDebtRatio = 0.25m; // 25% conservative threshold
    private const decimal MinSafetyBuffer = 500m; // Minimum R500 safety buffer
    private const decimal SafetyBufferPercentage = 0.10m; // 10% of income as safety buffer
    private const int AssessmentValidityDays = 30;

    public AffordabilityService(ApplicationDbContext context, ILogger<AffordabilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AffordabilityAssessment> CalculateAffordabilityAsync(string userId)
    {
        // Get all incomes for the user
        var incomes = await _context.Incomes
            .Where(i => i.UserId == userId)
            .ToListAsync();

        // Get all expenses for the user
        var expenses = await _context.Expenses
            .Where(e => e.UserId == userId)
            .ToListAsync();

        // Calculate income totals
        var grossMonthlyIncome = incomes.Sum(i => ConvertToMonthly(i.MonthlyAmount, i.Frequency));
        
        // Calculate expense totals
        var totalMonthlyExpenses = expenses.Sum(e => ConvertToMonthly(e.MonthlyAmount, e.Frequency));
        var essentialExpenses = expenses
            .Where(e => e.IsEssential)
            .Sum(e => ConvertToMonthly(e.MonthlyAmount, e.Frequency));
        var nonEssentialExpenses = totalMonthlyExpenses - essentialExpenses;

        // Calculate safety buffer - minimum R500 or 10% of income, whichever is higher
        var safetyBuffer = Math.Max(MinSafetyBuffer, grossMonthlyIncome * SafetyBufferPercentage);
        _logger.LogDebug("Safety buffer calculated: {SafetyBuffer:C} for user {UserId}", safetyBuffer, userId);

        // Calculate net income (with safety buffer deducted)
        var netMonthlyIncome = grossMonthlyIncome - totalMonthlyExpenses - safetyBuffer;

        // Get existing debt obligations from expenses
        var debtExpenses = expenses
            .Where(e => e.Category == "Debt")
            .Sum(e => ConvertToMonthly(e.MonthlyAmount, e.Frequency));

        // Calculate existing loan payments from current loans (approved or disbursed)
        var existingLoanPayments = await _context.LoanApplications
            .Where(l => l.UserId == userId && 
                       (l.Status == LoanStatus.Approved || 
                        l.Status == LoanStatus.Disbursed))
            .SumAsync(l => l.MonthlyPayment); // Use the calculated monthly payment from the application

        var totalDebtPayments = debtExpenses + existingLoanPayments;

        // Calculate debt-to-income ratio (NCA compliance: max 35%)
        var debtToIncomeRatio = grossMonthlyIncome > 0 ? totalDebtPayments / grossMonthlyIncome : 0;
        var expenseToIncomeRatio = grossMonthlyIncome > 0 ? totalMonthlyExpenses / grossMonthlyIncome : 0;
        var availableFunds = netMonthlyIncome;

        _logger.LogInformation(
            "User {UserId} affordability: Income={Income:C}, Expenses={Expenses:C}, SafetyBuffer={Buffer:C}, NetIncome={Net:C}, DTI={DTI:P1}",
            userId, grossMonthlyIncome, totalMonthlyExpenses, safetyBuffer, netMonthlyIncome, debtToIncomeRatio);

        // Determine affordability status based on NCA guidelines and safety buffers
        var affordabilityStatus = DetermineAffordabilityStatus(debtToIncomeRatio, netMonthlyIncome, grossMonthlyIncome);
        
        // Calculate max recommended loan amount with safety considerations
        var maxRecommendedLoanAmount = CalculateMaxLoanAmount(netMonthlyIncome, debtToIncomeRatio, grossMonthlyIncome, safetyBuffer);

        // Create assessment notes
        var assessmentNotes = GenerateAssessmentNotes(
            affordabilityStatus,
            debtToIncomeRatio,
            netMonthlyIncome,
            essentialExpenses,
            totalMonthlyExpenses,
            grossMonthlyIncome,
            safetyBuffer
        );

        // Check if assessment already exists
        var existingAssessment = await _context.AffordabilityAssessments
            .FirstOrDefaultAsync(a => a.UserId == userId);

        var assessment = existingAssessment ?? new AffordabilityAssessment { UserId = userId };

        assessment.GrossMonthlyIncome = grossMonthlyIncome;
        assessment.NetMonthlyIncome = netMonthlyIncome;
        assessment.TotalMonthlyExpenses = totalMonthlyExpenses;
        assessment.EssentialExpenses = essentialExpenses;
        assessment.NonEssentialExpenses = nonEssentialExpenses;
        assessment.ExistingDebtPayments = totalDebtPayments;
        assessment.DebtToIncomeRatio = debtToIncomeRatio;
        assessment.AvailableFunds = availableFunds;
        assessment.ExpenseToIncomeRatio = expenseToIncomeRatio;
        assessment.AffordabilityStatus = affordabilityStatus;
        assessment.AssessmentNotes = assessmentNotes;
        assessment.MaxRecommendedLoanAmount = maxRecommendedLoanAmount;
        assessment.AssessmentDate = DateTime.UtcNow;
        assessment.ExpiryDate = DateTime.UtcNow.AddDays(AssessmentValidityDays);

        if (existingAssessment == null)
        {
            _context.AffordabilityAssessments.Add(assessment);
        }

        await _context.SaveChangesAsync();
        return assessment;
    }

    public async Task<decimal> GetMaxRecommendedLoanAmountAsync(string userId)
    {
        var assessment = await _context.AffordabilityAssessments
            .FirstOrDefaultAsync(a => a.UserId == userId);

        return assessment?.MaxRecommendedLoanAmount ?? 0;
    }

    /// <summary>
    /// Convert any frequency to monthly amount
    /// </summary>
    private decimal ConvertToMonthly(decimal amount, string? frequency)
    {
        return (frequency?.ToLower()) switch
        {
            "weekly" => amount * 4.33m, // Average weeks per month
            "bi-weekly" => amount * 2.17m,
            "annual" => amount / 12m,
            _ => amount // Monthly or default
        };
    }

    /// <summary>
    /// Determine affordability status per NCA guidelines with conservative thresholds
    /// </summary>
    private string DetermineAffordabilityStatus(decimal debtToIncomeRatio, decimal netIncome, decimal grossIncome)
    {
        // NCA Guideline: Debt to Income ratio must not exceed 35% (MaxDebtToIncomeRatio)
        if (debtToIncomeRatio > MaxDebtToIncomeRatio)
        {
            _logger.LogWarning("User exceeds NCA debt limit: DTI={DTI:P1}", debtToIncomeRatio);
            return "NotAffordable"; // Over NCA limit
        }

        // No disposable income after expenses and safety buffer
        if (netIncome <= 0)
        {
            _logger.LogWarning("User has no disposable income: NetIncome={NetIncome:C}", netIncome);
            return "NotAffordable";
        }

        // Conservative threshold: 25% debt-to-income triggers LimitedAffordability
        // This provides early warning before reaching NCA limit
        if (debtToIncomeRatio > ConservativeDebtRatio)
        {
            _logger.LogInformation("User approaching debt limits: DTI={DTI:P1}", debtToIncomeRatio);
            return "LimitedAffordability";
        }

        // Limited disposable income (less than 10% of gross income after expenses and buffer)
        if (netIncome < (grossIncome * 0.10m))
        {
            _logger.LogInformation("User has limited disposable income: NetIncome={NetIncome:C}", netIncome);
            return "LimitedAffordability";
        }

        return "Affordable";
    }

    /// <summary>
    /// Calculate maximum recommended loan amount with safety considerations
    /// Formula: Available monthly funds (after buffer) can support loan repayment over the loan term
    /// Assuming 36-month term and prime rate lending
    /// </summary>
    private decimal CalculateMaxLoanAmount(decimal availableFunds, decimal debtToIncomeRatio, decimal grossIncome, decimal safetyBuffer)
    {
        // Ensure we don't recommend loans if already at debt limit
        if (debtToIncomeRatio >= MaxDebtToIncomeRatio)
        {
            _logger.LogWarning("Cannot recommend loan: DTI at or above limit");
            return 0;
        }

        // Reserve at least 20% of available funds for unexpected expenses
        var availableForLoan = availableFunds * 0.80m;

        // If no funds available after safety buffer, cannot afford a loan
        if (availableForLoan <= 0)
        {
            return 0;
        }

        // Typical monthly repayment capacity
        var monthlyRepaymentCapacity = availableForLoan;

        // For a 36-month loan at approximately 11% annual interest (South African prime rate approximate)
        // Monthly repayment formula: P = [r(PV)] / [1 - (1 + r)^-n]
        // Where P = monthly payment, r = monthly rate, PV = principal, n = number of periods

        const decimal annualInterestRate = 0.11m; // 11% approximate
        const decimal monthlyInterestRate = annualInterestRate / 12m;
        const int loanTermMonths = 36;

        var factor = ((1 + monthlyInterestRate) * (decimal)Math.Pow((double)(1 + monthlyInterestRate), loanTermMonths - 1)) / 
                     (((decimal)Math.Pow((double)(1 + monthlyInterestRate), loanTermMonths)) - 1);
        var maxLoan = monthlyRepaymentCapacity / factor;

        _logger.LogDebug("Max loan calculated: {MaxLoan:C} (monthly capacity: {Capacity:C}, safety buffer: {Buffer:C})", 
            maxLoan, monthlyRepaymentCapacity, safetyBuffer);

        return Math.Max(0, maxLoan);
    }

    /// <summary>
    /// Generate detailed assessment notes with safety buffer considerations
    /// </summary>
    private string GenerateAssessmentNotes(
        string status,
        decimal debtToIncomeRatio,
        decimal netIncome,
        decimal essentialExpenses,
        decimal totalExpenses,
        decimal grossIncome,
        decimal safetyBuffer)
    {
        var notes = status switch
        {
            "NotAffordable" when debtToIncomeRatio > MaxDebtToIncomeRatio =>
                $"Your debt-to-income ratio of {(debtToIncomeRatio * 100):F1}% exceeds the NCR limit of {(MaxDebtToIncomeRatio * 100):F0}%. " +
                "You may not be approved for additional loans until your debt levels decrease.",

            "NotAffordable" when netIncome <= 0 =>
                "Your monthly expenses exceed your income (including required safety buffer). " +
                "Consider reducing non-essential expenses before applying for a loan.",

            "LimitedAffordability" when debtToIncomeRatio > ConservativeDebtRatio =>
                $"Your debt-to-income ratio of {(debtToIncomeRatio * 100):F1}% is approaching our conservative limit of {(ConservativeDebtRatio * 100):F0}%. " +
                "While you may still qualify, consider reducing existing debts to improve your affordability.",

            "LimitedAffordability" =>
                $"Your available funds after expenses and safety buffer are limited ({netIncome:C}). " +
                "A small loan may be considered, but focus on building emergency savings first.",

            "Affordable" =>
                $"Your affordability profile is healthy. After expenses and a {safetyBuffer:C} safety buffer, " +
                $"you have {netIncome:C} available monthly. Your debt-to-income ratio is {(debtToIncomeRatio * 100):F1}%, " +
                $"well within NCR guidelines.",

            _ => "Affordability assessment complete."
        };

        // Add essential vs non-essential breakdown
        var essentialPercentage = totalExpenses > 0 ? (essentialExpenses / totalExpenses) * 100 : 0;
        notes += $"\n\nExpense Analysis: {essentialPercentage:F1}% of your expenses are essential. ";
        notes += $"Safety buffer of {safetyBuffer:C} ({(safetyBuffer / grossIncome * 100):F1}% of income) is reserved for emergencies.";

        return notes;
    }

    /// <summary>
    /// Check if user can afford a specific loan amount and monthly payment
    /// </summary>
    public async Task<bool> CanAffordLoanAsync(string userId, decimal loanAmount, decimal monthlyPayment)
    {
        _logger.LogInformation("Checking affordability for user {UserId}: LoanAmount={Loan:C}, MonthlyPayment={Payment:C}", 
            userId, loanAmount, monthlyPayment);

        // Get current affordability assessment
        var assessment = await CalculateAffordabilityAsync(userId);

        // Cannot afford if already not affordable
        if (assessment.AffordabilityStatus == "NotAffordable")
        {
            _logger.LogWarning("User {UserId} marked as NotAffordable", userId);
            return false;
        }

        // Calculate new debt-to-income ratio with this loan
        var newTotalDebt = assessment.ExistingDebtPayments + monthlyPayment;
        var newDebtToIncomeRatio = assessment.GrossMonthlyIncome > 0 
            ? newTotalDebt / assessment.GrossMonthlyIncome 
            : 1m;

        // Check against NCA limit
        if (newDebtToIncomeRatio > MaxDebtToIncomeRatio)
        {
            _logger.LogWarning("User {UserId} would exceed NCA limit: NewDTI={DTI:P1}", userId, newDebtToIncomeRatio);
            return false;
        }

        // Check if monthly payment exceeds available funds
        if (monthlyPayment > assessment.NetMonthlyIncome)
        {
            _logger.LogWarning("User {UserId} cannot afford monthly payment: Payment={Payment:C}, Available={Available:C}", 
                userId, monthlyPayment, assessment.NetMonthlyIncome);
            return false;
        }

        // Check if this would leave less than minimum safety buffer
        var remainingFunds = assessment.NetMonthlyIncome - monthlyPayment;
        if (remainingFunds < MinSafetyBuffer * 0.5m) // At least 50% of safety buffer should remain
        {
            _logger.LogWarning("User {UserId} would have insufficient funds after loan: Remaining={Remaining:C}", 
                userId, remainingFunds);
            return false;
        }

        _logger.LogInformation("User {UserId} can afford loan: NewDTI={DTI:P1}, RemainingFunds={Remaining:C}", 
            userId, newDebtToIncomeRatio, remainingFunds);

        return true;
    }

    /// <summary>
    /// Calculate maximum affordable loan amount for a given interest rate and term
    /// </summary>
    public async Task<decimal> GetMaxAffordableLoanAsync(string userId, decimal interestRate, int termMonths)
    {
        _logger.LogInformation("Calculating max affordable loan for user {UserId}: Rate={Rate:P2}, Term={Term} months", 
            userId, interestRate, termMonths);

        // Get current affordability assessment
        var assessment = await CalculateAffordabilityAsync(userId);

        // If not affordable, cannot take any loan
        if (assessment.AffordabilityStatus == "NotAffordable" || assessment.NetMonthlyIncome <= 0)
        {
            _logger.LogWarning("User {UserId} is not affordable for any loan", userId);
            return 0;
        }

        // Calculate maximum monthly payment capacity
        // Reserve at least 20% of available funds for buffer
        var maxMonthlyPayment = assessment.NetMonthlyIncome * 0.80m;

        // Also check against NCA debt-to-income limit
        var maxDebtPaymentByNCA = (assessment.GrossMonthlyIncome * MaxDebtToIncomeRatio) - assessment.ExistingDebtPayments;
        maxMonthlyPayment = Math.Min(maxMonthlyPayment, maxDebtPaymentByNCA);

        if (maxMonthlyPayment <= 0)
        {
            _logger.LogWarning("User {UserId} has no payment capacity", userId);
            return 0;
        }

        // Calculate loan principal from monthly payment
        // Formula: PV = PMT × [(1 - (1 + r)^-n) / r]
        // Where PV = present value (loan amount), PMT = payment, r = monthly rate, n = periods

        var monthlyRate = interestRate / 12m;
        
        if (monthlyRate == 0)
        {
            // No interest case (e.g., worker single-payment loans)
            return maxMonthlyPayment * termMonths;
        }

        var discountFactor = (1 - (decimal)Math.Pow((double)(1 + monthlyRate), -termMonths)) / monthlyRate;
        var maxLoanAmount = maxMonthlyPayment * discountFactor;

        _logger.LogInformation("User {UserId} max affordable loan: {MaxLoan:C} (monthly payment capacity: {Payment:C})", 
            userId, maxLoanAmount, maxMonthlyPayment);

        return Math.Max(0, maxLoanAmount);
    }
}
