namespace HoHemaLoans.Api.Models;

public class BulkUserImportDto
{
    public int RowNumber { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal MonthlyIncome { get; set; }
}

public class BulkImportValidationResult
{
    public bool IsValid { get; set; }
    public List<BulkImportValidationError> ValidationErrors { get; set; } = new();
    public List<BulkUserImportDto> ValidUsers { get; set; } = new();
    public int TotalRows { get; set; }
}

public class BulkImportValidationError
{
    public int RowNumber { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}

public class BulkImportResult
{
    public int TotalUsers { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
}