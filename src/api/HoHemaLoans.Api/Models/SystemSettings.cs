using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HoHemaLoans.Api.Models;

public class SystemSettings
{
    [Key]
    public int Id { get; set; }

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal InterestRatePercentage { get; set; } = 5.0m;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal AdminFee { get; set; } = 50.0m;

    [Required]
    [Column(TypeName = "decimal(5,2)")]
    public decimal MaxLoanPercentage { get; set; } = 20.0m;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal MinLoanAmount { get; set; } = 100.0m;

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal MaxLoanAmount { get; set; } = 10000.0m;

    public DateTime LastModifiedDate { get; set; } = DateTime.UtcNow;

    public string? LastModifiedBy { get; set; }
}
