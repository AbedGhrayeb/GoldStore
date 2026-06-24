using System.ComponentModel.DataAnnotations;

namespace WebUI.Models.Debt;

public class UpdateDebtModel
{
    [Required]
    public Guid Id { get; set; }

    [StringLength(200)]
    public string? Name { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}
