namespace HoHemaLoans.Api.Models;

public class IncomeDto
{
    public Guid Id { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyAmount { get; set; }
    public string? Frequency { get; set; }
    public string? Notes { get; set; }
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateIncomeDto
{
    public string SourceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyAmount { get; set; }
    public string? Frequency { get; set; } = "Monthly";
    public string? Notes { get; set; }
}

public class UpdateIncomeDto
{
    public string SourceType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyAmount { get; set; }
    public string? Frequency { get; set; }
    public string? Notes { get; set; }
}
