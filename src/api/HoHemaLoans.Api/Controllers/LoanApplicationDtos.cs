using System.Text.Json.Serialization;

namespace HoHemaLoans.Api.Controllers;

/// <summary>
/// DTO for creating a new loan application (draft)
/// </summary>
public class CreateLoanApplicationDto
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("termMonths")]
    public int TermMonths { get; set; }
    
    [JsonPropertyName("purpose")]
    public string Purpose { get; set; } = string.Empty;
    
    [JsonPropertyName("channelOrigin")]
    public string ChannelOrigin { get; set; } = "Web";
    
    [JsonPropertyName("currentStep")]
    public int CurrentStep { get; set; } = 0;
}

/// <summary>
/// DTO for updating a specific step in the loan application
/// </summary>
public class UpdateApplicationStepDto
{
    [JsonPropertyName("stepNumber")]
    public int StepNumber { get; set; }
    
    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// DTO for submitting a completed loan application
/// </summary>
public class SubmitLoanApplicationDto
{
    [JsonPropertyName("otp")]
    public string Otp { get; set; } = string.Empty;
}

/// <summary>
/// DTO for resuming an application from another channel
/// </summary>
public class ResumeApplicationDto
{
    [JsonPropertyName("applicationId")]
    public Guid ApplicationId { get; set; }
    
    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "Web";
}

/// <summary>
/// DTO for displaying loan application summary
/// </summary>
public class LoanApplicationDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("termMonths")]
    public int TermMonths { get; set; }
    
    [JsonPropertyName("purpose")]
    public string Purpose { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("channelOrigin")]
    public string ChannelOrigin { get; set; } = string.Empty;
    
    [JsonPropertyName("currentStep")]
    public int CurrentStep { get; set; }
    
    [JsonPropertyName("monthlyPayment")]
    public decimal MonthlyPayment { get; set; }
    
    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }
    
    [JsonPropertyName("interestRate")]
    public decimal InterestRate { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updatedAt")]
    public DateTime? UpdatedAt { get; set; }
    
    [JsonPropertyName("stepData")]
    public Dictionary<string, object>? StepData { get; set; }
    
    [JsonPropertyName("isAffordabilityIncluded")]
    public bool IsAffordabilityIncluded { get; set; }
    
    [JsonPropertyName("bankName")]
    public string? BankName { get; set; }
    
    [JsonPropertyName("accountNumber")]
    public string? AccountNumber { get; set; }
    
    [JsonPropertyName("accountHolderName")]
    public string? AccountHolderName { get; set; }
}

/// <summary>
/// DTO for listing user's loan applications
/// </summary>
public class LoanApplicationListItemDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("channelOrigin")]
    public string ChannelOrigin { get; set; } = string.Empty;
    
    [JsonPropertyName("currentStep")]
    public int CurrentStep { get; set; }
    
    [JsonPropertyName("purpose")]
    public string Purpose { get; set; } = string.Empty;
    
    [JsonPropertyName("monthlyPayment")]
    public decimal MonthlyPayment { get; set; }
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for WhatsApp session management
/// </summary>
public class WhatsAppSessionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("draftApplicationId")]
    public Guid? DraftApplicationId { get; set; }
    
    [JsonPropertyName("currentStep")]
    public int CurrentStep { get; set; }
    
    [JsonPropertyName("sessionStatus")]
    public string SessionStatus { get; set; } = string.Empty;
    
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for resuming from WhatsApp
/// </summary>
public class ResumeFromWhatsAppDto
{
    [JsonPropertyName("phoneNumber")]
    public string PhoneNumber { get; set; } = string.Empty;
}

/// <summary>
/// DTO for successful loan submission response
/// </summary>
public class LoanSubmissionResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("applicationId")]
    public Guid ApplicationId { get; set; }
    
    [JsonPropertyName("referenceNumber")]
    public string ReferenceNumber { get; set; } = string.Empty;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("submittedAt")]
    public DateTime SubmittedAt { get; set; }
    
    [JsonPropertyName("nextSteps")]
    public List<string> NextSteps { get; set; } = new();
}

/// <summary>
/// DTO for application step response
/// </summary>
public class ApplicationStepResponseDto
{
    [JsonPropertyName("currentStep")]
    public int CurrentStep { get; set; }
    
    [JsonPropertyName("totalSteps")]
    public int TotalSteps { get; set; } = 7;
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; set; } = new();
    
    [JsonPropertyName("nextStepPrompt")]
    public string NextStepPrompt { get; set; } = string.Empty;
}
