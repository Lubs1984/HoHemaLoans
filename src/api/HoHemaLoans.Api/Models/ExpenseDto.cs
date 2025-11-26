namespace HoHemaLoans.Api.Models;

public class ExpenseDto
{
    public Guid Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyAmount { get; set; }
    public string? Frequency { get; set; }
    public string? Notes { get; set; }
    public bool IsEssential { get; set; }
    public bool IsFixed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateExpenseDto
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyAmount { get; set; }
    public string? Frequency { get; set; } = "Monthly";
    public string? Notes { get; set; }
    public bool IsEssential { get; set; } = false;
    public bool IsFixed { get; set; } = false;
}

public class UpdateExpenseDto
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyAmount { get; set; }
    public string? Frequency { get; set; }
    public string? Notes { get; set; }
    public bool IsEssential { get; set; }
    public bool IsFixed { get; set; }
}
