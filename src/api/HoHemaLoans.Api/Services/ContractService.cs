using HoHemaLoans.Api.Data;
using HoHemaLoans.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace HoHemaLoans.Api.Services;

/// <summary>
/// Service for managing contracts and digital signatures
/// Includes WhatsApp PIN-based signature functionality
/// </summary>
public class ContractService
{
    private readonly ApplicationDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<ContractService> _logger;

    public ContractService(
        ApplicationDbContext context,
        IWhatsAppService whatsAppService,
        ILogger<ContractService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    /// <summary>
    /// Generate a credit agreement contract for an approved loan application
    /// </summary>
    public async Task<Contract> GenerateCreditAgreementAsync(Guid loanApplicationId, string userId)
    {
        var loanApplication = await _context.LoanApplications
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == loanApplicationId && l.UserId == userId);

        if (loanApplication == null)
        {
            throw new InvalidOperationException("Loan application not found");
        }

        if (loanApplication.Status != LoanStatus.Approved)
        {
            throw new InvalidOperationException("Loan application must be approved before generating contract");
        }

        // Check if contract already exists
        var existingContract = await _context.Contracts
            .FirstOrDefaultAsync(c => c.LoanApplicationId == loanApplicationId 
                && c.ContractType == ContractTypes.CreditAgreement
                && c.Status != ContractStatus.Cancelled);

        if (existingContract != null)
        {
            return existingContract;
        }

        // Generate contract content (Form 39)
        var contractContent = GenerateForm39Content(loanApplication);

        var contract = new Contract
        {
            LoanApplicationId = loanApplicationId,
            UserId = userId,
            ContractType = ContractTypes.CreditAgreement,
            ContractContent = contractContent,
            Status = ContractStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30), // Contract valid for 30 days
            Version = 1,
            Metadata = $"{{\"ncrRegistrationNumber\":\"NCRCP000000\",\"generatedBy\":\"System\"}}"
        };

        _context.Contracts.Add(contract);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Contract {ContractId} generated for loan application {LoanApplicationId}", 
            contract.Id, loanApplicationId);

        return contract;
    }

    /// <summary>
    /// Send contract for signing via WhatsApp PIN
    /// Generates a 6-digit PIN and sends it via WhatsApp
    /// </summary>
    public async Task<(bool Success, string Message, string? Pin)> SendContractForSigningAsync(
        int contractId, 
        string userId, 
        string phoneNumber,
        bool isDevelopment = false)
    {
        var contract = await _context.Contracts
            .Include(c => c.LoanApplication)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);

        if (contract == null)
        {
            return (false, "Contract not found", null);
        }

        if (contract.Status == ContractStatus.Signed)
        {
            return (false, "Contract is already signed", null);
        }

        if (contract.Status == ContractStatus.Expired)
        {
            return (false, "Contract has expired", null);
        }

        if (contract.ExpiresAt < DateTime.UtcNow)
        {
            contract.Status = ContractStatus.Expired;
            await _context.SaveChangesAsync();
            return (false, "Contract has expired", null);
        }

        // Generate 6-digit PIN
        var pin = GeneratePin();
        var (hash, salt) = HashPin(pin);

        // Create or update digital signature record
        var existingSignature = await _context.DigitalSignatures
            .FirstOrDefaultAsync(ds => ds.ContractId == contractId);

        if (existingSignature != null)
        {
            // Update existing signature with new PIN
            existingSignature.SignatureHash = hash;
            existingSignature.Salt = salt;
            existingSignature.PhoneNumber = phoneNumber;
            existingSignature.PinSentAt = DateTime.UtcNow;
            existingSignature.PinExpiresAt = DateTime.UtcNow.AddMinutes(10);
            existingSignature.FailedAttempts = 0;
            existingSignature.IsValid = false;
        }
        else
        {
            // Create new signature record
            var signature = new DigitalSignature
            {
                ContractId = contractId,
                UserId = userId,
                SignatureMethod = SignatureMethods.WhatsAppPIN,
                SignatureHash = hash,
                Salt = salt,
                PhoneNumber = phoneNumber,
                PinSentAt = DateTime.UtcNow,
                PinExpiresAt = DateTime.UtcNow.AddMinutes(10),
                FailedAttempts = 0,
                IsValid = false,
                SignerName = contract.User?.FirstName + " " + contract.User?.LastName,
                SignerIdNumber = contract.User?.IdNumber
            };

            _context.DigitalSignatures.Add(signature);
        }

        contract.Status = ContractStatus.Sent;
        contract.SentAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Send PIN via WhatsApp template
        var loanAmount = contract.LoanApplication?.Amount ?? 0;
        var templateParameters = new List<string>
        {
            pin                        // PIN parameter only
        };

        try
        {
            await _whatsAppService.SendTemplateMessageAsync(phoneNumber, "loanconfirmationotp", templateParameters);
            _logger.LogInformation("Signing PIN sent via WhatsApp to {PhoneNumber} for contract {ContractId}", 
                phoneNumber, contractId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send PIN via WhatsApp to {PhoneNumber}", phoneNumber);
            // Continue anyway - PIN is still valid if user enters it manually
        }

        // Return PIN only in development mode for testing
        var returnPin = isDevelopment ? pin : null;

        return (true, "Signing PIN sent via WhatsApp", returnPin);
    }

    /// <summary>
    /// Verify PIN and complete contract signing
    /// </summary>
    public async Task<(bool Success, string Message)> VerifyPinAndSignContractAsync(
        int contractId,
        string userId,
        string pin,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var signature = await _context.DigitalSignatures
            .Include(ds => ds.Contract)
            .FirstOrDefaultAsync(ds => ds.ContractId == contractId && ds.UserId == userId);

        if (signature == null)
        {
            return (false, "Signature record not found. Please request a new signing PIN.");
        }

        if (signature.IsValid)
        {
            return (false, "Contract is already signed.");
        }

        // Check if PIN has expired
        if (signature.PinExpiresAt < DateTime.UtcNow)
        {
            return (false, "PIN has expired. Please request a new signing PIN.");
        }

        // Check failed attempts (max 3)
        if (signature.FailedAttempts >= 3)
        {
            return (false, "Too many failed attempts. Please request a new signing PIN.");
        }

        // Verify PIN
        if (!VerifyPin(pin, signature.SignatureHash, signature.Salt))
        {
            signature.FailedAttempts++;
            await _context.SaveChangesAsync();

            var attemptsLeft = 3 - signature.FailedAttempts;
            return (false, $"Invalid PIN. {attemptsLeft} attempt(s) remaining.");
        }

        // PIN is valid - complete signature
        signature.IsValid = true;
        signature.SignedAt = DateTime.UtcNow;
        signature.IpAddress = ipAddress;
        signature.UserAgent = userAgent;
        signature.AuditMetadata = $"{{\"signedAt\":\"{DateTime.UtcNow:O}\",\"method\":\"WhatsAppPIN\"}}";

        // Update contract status
        if (signature.Contract != null)
        {
            signature.Contract.Status = ContractStatus.Signed;
            signature.Contract.SignedAt = DateTime.UtcNow;

            // Update loan application status to Disbursed (ready for payment)
            var loanApplication = await _context.LoanApplications
                .FirstOrDefaultAsync(l => l.Id == signature.Contract.LoanApplicationId);

            if (loanApplication != null)
            {
                loanApplication.Status = LoanStatus.Disbursed;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Contract {ContractId} signed successfully by user {UserId}", 
            contractId, userId);

        // Send confirmation via WhatsApp
        try
        {
            var confirmationMessage = $"✅ *Contract Signed Successfully*\n\n" +
                                    $"Your loan agreement has been digitally signed.\n\n" +
                                    $"Your loan is now being processed for disbursement.\n\n" +
                                    $"You will receive a confirmation once the funds are transferred.";

            await _whatsAppService.SendMessageAsync(signature.PhoneNumber, confirmationMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send confirmation message");
            // Don't fail the signing process if WhatsApp message fails
        }

        return (true, "Contract signed successfully!");
    }

    /// <summary>
    /// Get contract by ID
    /// </summary>
    public async Task<Contract?> GetContractAsync(int contractId, string userId)
    {
        return await _context.Contracts
            .Include(c => c.LoanApplication)
            .Include(c => c.User)
            .Include(c => c.DigitalSignature)
            .FirstOrDefaultAsync(c => c.Id == contractId && c.UserId == userId);
    }

    /// <summary>
    /// Get all contracts for a user
    /// </summary>
    public async Task<List<Contract>> GetUserContractsAsync(string userId)
    {
        return await _context.Contracts
            .Include(c => c.LoanApplication)
            .Include(c => c.DigitalSignature)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get contract for a specific loan application
    /// </summary>
    public async Task<Contract?> GetContractByLoanApplicationAsync(Guid loanApplicationId, string userId)
    {
        return await _context.Contracts
            .Include(c => c.LoanApplication)
            .Include(c => c.DigitalSignature)
            .FirstOrDefaultAsync(c => c.LoanApplicationId == loanApplicationId 
                && c.UserId == userId 
                && c.ContractType == ContractTypes.CreditAgreement);
    }

    // Helper methods

    private string GeneratePin()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private (string hash, string salt) HashPin(string pin)
    {
        // Generate salt
        var saltBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        var salt = Convert.ToBase64String(saltBytes);

        // Hash PIN with salt
        var hash = HashPinWithSalt(pin, salt);

        return (hash, salt);
    }

    private string HashPinWithSalt(string pin, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var pinBytes = Encoding.UTF8.GetBytes(pin);

        var combined = new byte[saltBytes.Length + pinBytes.Length];
        Buffer.BlockCopy(saltBytes, 0, combined, 0, saltBytes.Length);
        Buffer.BlockCopy(pinBytes, 0, combined, saltBytes.Length, pinBytes.Length);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(combined);
        return Convert.ToBase64String(hashBytes);
    }

    private bool VerifyPin(string pin, string hash, string salt)
    {
        var computedHash = HashPinWithSalt(pin, salt);
        return computedHash == hash;
    }

    private string GenerateForm39Content(LoanApplication loanApplication)
    {
        var user = loanApplication.User;
        var today = DateTime.UtcNow.ToString("dd MMMM yyyy");

        // Generate NCR Form 39 - Credit Agreement content
        var content = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; padding: 20px; }}
        .header {{ text-align: center; font-weight: bold; font-size: 18px; margin-bottom: 20px; }}
        .section {{ margin-bottom: 20px; }}
        .section-title {{ font-weight: bold; text-decoration: underline; margin-bottom: 10px; }}
        .field {{ margin: 5px 0; }}
        .field-label {{ font-weight: bold; }}
        table {{ width: 100%; border-collapse: collapse; margin: 10px 0; }}
        th, td {{ border: 1px solid #000; padding: 8px; text-align: left; }}
        th {{ background-color: #f0f0f0; }}
        .footer {{ margin-top: 30px; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='header'>
        CREDIT AGREEMENT (FORM 39)<br>
        National Credit Act, 2005
    </div>

    <div class='section'>
        <div class='section-title'>CREDIT PROVIDER DETAILS</div>
        <div class='field'><span class='field-label'>Name:</span> Ho Hema Loans (Pty) Ltd</div>
        <div class='field'><span class='field-label'>NCR Registration Number:</span> NCRCP000000</div>
        <div class='field'><span class='field-label'>Contact Number:</span> +27 XX XXX XXXX</div>
        <div class='field'><span class='field-label'>Email:</span> info@hohemaloans.co.za</div>
        <div class='field'><span class='field-label'>Physical Address:</span> [To be completed]</div>
    </div>

    <div class='section'>
        <div class='section-title'>CONSUMER DETAILS</div>
        <div class='field'><span class='field-label'>Full Name:</span> {user?.FirstName} {user?.LastName}</div>
        <div class='field'><span class='field-label'>ID Number:</span> {user?.IdNumber}</div>
        <div class='field'><span class='field-label'>Phone Number:</span> {user?.PhoneNumber}</div>
        <div class='field'><span class='field-label'>Email:</span> {user?.Email}</div>
        <div class='field'><span class='field-label'>Physical Address:</span> {user?.Address}</div>
    </div>

    <div class='section'>
        <div class='section-title'>LOAN DETAILS</div>
        <div class='field'><span class='field-label'>Agreement Number:</span> LA-{loanApplication.Id:D6}</div>
        <div class='field'><span class='field-label'>Agreement Date:</span> {today}</div>
        <table>
            <tr>
                <th>Description</th>
                <th>Amount (ZAR)</th>
            </tr>
            <tr>
                <td>Principal Loan Amount</td>
                <td>R {loanApplication.Amount:N2}</td>
            </tr>
            <tr>
                <td>Interest Rate (per annum)</td>
                <td>{(loanApplication.InterestRate * 100):N2}%</td>
            </tr>
            <tr>
                <td>Loan Term</td>
                <td>{loanApplication.TermMonths} months</td>
            </tr>
            <tr>
                <td>Monthly Installment</td>
                <td>R {loanApplication.MonthlyPayment:N2}</td>
            </tr>
            <tr>
                <td><strong>Total Amount Repayable</strong></td>
                <td><strong>R {loanApplication.TotalAmount:N2}</strong></td>
            </tr>
        </table>
    </div>

    <div class='section'>
        <div class='section-title'>REPAYMENT SCHEDULE</div>
        <div class='field'><span class='field-label'>First Payment Date:</span> {(loanApplication.ApprovalDate?.AddMonths(1).ToString("dd MMMM yyyy") ?? "To be determined")}</div>
        <div class='field'><span class='field-label'>Payment Frequency:</span> Monthly</div>
        <div class='field'><span class='field-label'>Payment Method:</span> Bank Account - {loanApplication.BankName ?? "N/A"} - Acc: {loanApplication.AccountNumber ?? "N/A"}</div>
    </div>

    <div class='section'>
        <div class='section-title'>AFFORDABILITY ASSESSMENT</div>
        <div class='field'>The credit provider has conducted an affordability assessment as required by the National Credit Act.</div>
        <div class='field'><span class='field-label'>Affordability Status:</span> {loanApplication.AffordabilityStatus ?? "Pending Assessment"}</div>
        <div class='field'><span class='field-label'>Passed Affordability Check:</span> {(loanApplication.PassedAffordabilityCheck ? "Yes" : "No")}</div>
        <div class='field'><span class='field-label'>Assessment Notes:</span> {loanApplication.AffordabilityNotes ?? "N/A"}</div>
    </div>

    <div class='section'>
        <div class='section-title'>TERMS AND CONDITIONS</div>
        <ol>
            <li><strong>Cooling-Off Period:</strong> You have the right to cancel this agreement within 5 business days of signing without penalty.</li>
            <li><strong>Early Settlement:</strong> You may settle the loan early. Early settlement fees may apply as per the National Credit Act.</li>
            <li><strong>Default:</strong> Failure to make timely payments may result in additional fees and negative credit bureau reporting.</li>
            <li><strong>Interest Charges:</strong> Interest is calculated on the outstanding balance at the rate specified above.</li>
            <li><strong>Fees:</strong> All fees are disclosed above and comply with National Credit Act regulations.</li>
            <li><strong>Credit Bureau Reporting:</strong> Your payment behavior will be reported to registered credit bureaus.</li>
            <li><strong>Statements:</strong> You are entitled to receive a statement of account upon request.</li>
            <li><strong>Complaints:</strong> Complaints may be directed to the National Credit Regulator (NCR).</li>
        </ol>
    </div>

    <div class='section'>
        <div class='section-title'>CONSUMER RIGHTS</div>
        <ul>
            <li>Right to information in plain language</li>
            <li>Right to fair credit assessment</li>
            <li>Right to protection against discrimination</li>
            <li>Right to privacy and confidentiality</li>
            <li>Right to approach the credit regulator or ombud</li>
        </ul>
    </div>

    <div class='section'>
        <div class='section-title'>DECLARATIONS</div>
        <p><strong>Credit Provider Declaration:</strong></p>
        <p>Ho Hema Loans (Pty) Ltd confirms that:</p>
        <ul>
            <li>An affordability assessment has been conducted</li>
            <li>The consumer meets the credit criteria</li>
            <li>All terms comply with the National Credit Act</li>
        </ul>
        
        <p><strong>Consumer Declaration:</strong></p>
        <p>I, {user?.FirstName} {user?.LastName}, confirm that:</p>
        <ul>
            <li>I have read and understood this agreement</li>
            <li>All information provided is true and accurate</li>
            <li>I agree to the terms and conditions stated above</li>
            <li>I understand my rights and obligations under this agreement</li>
        </ul>
    </div>

    <div class='footer'>
        <p><strong>Document Reference:</strong> CONTRACT-{loanApplication.Id:D6}-{DateTime.UtcNow:yyyyMMdd}</p>
        <p><strong>Digitally Signed via WhatsApp PIN Verification</strong></p>
        <p>This is a legally binding electronic agreement in terms of the Electronic Communications and Transactions Act, 2002</p>
    </div>
</body>
</html>";

        return content;
    }
}
