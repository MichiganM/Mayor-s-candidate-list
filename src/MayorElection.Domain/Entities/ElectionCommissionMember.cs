using System.ComponentModel.DataAnnotations;

namespace MayorElection.Domain.Entities;

public sealed class ElectionCommissionMember : Participant
{
    [Required, MaxLength(200)] public string CommissionName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Role { get; set; } = string.Empty;
}
