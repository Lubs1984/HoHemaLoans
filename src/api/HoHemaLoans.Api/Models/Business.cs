using System.ComponentModel.DataAnnotations;

namespace HoHemaLoans.Api.Models;

public class Business
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string ContactPerson { get; set; } = string.Empty;

    [StringLength(100)]
    public string ContactEmail { get; set; } = string.Empty;

    [StringLength(20)]
    public string ContactPhone { get; set; } = string.Empty;

    [StringLength(200)]
    public string Address { get; set; } = string.Empty;

    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(50)]
    public string Province { get; set; } = string.Empty;

    [StringLength(10)]
    public string PostalCode { get; set; } = string.Empty;

    // Payroll Contact
    [StringLength(100)]
    public string PayrollContactName { get; set; } = string.Empty;

    [StringLength(100)]
    public string PayrollContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Day of the month when payroll runs (1-31)
    /// </summary>
    public int PayrollDay { get; set; } = 25;

    /// <summary>
    /// Maximum loan amount as percentage of salary (e.g. 30 = 30%)
    /// </summary>
    public decimal MaxLoanPercentage { get; set; } = 30;

    /// <summary>
    /// Per-employer interest rate override (monthly %). Null = use system default.
    /// </summary>
    public decimal? InterestRate { get; set; }

    /// <summary>
    /// Per-employer admin fee override. Null = use system default.
    /// </summary>
    public decimal? AdminFee { get; set; }

    public bool IsActive { get; set; } = true;

    public string Notes { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<ApplicationUser> Employees { get; set; } = new List<ApplicationUser>();
}
