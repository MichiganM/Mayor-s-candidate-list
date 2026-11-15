using System.ComponentModel.DataAnnotations;

namespace MayorElection.Domain.Entities;

public abstract class Participant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
    [MaxLength(100)] public string? Patronymic { get; set; }
    public DateOnly BirthDate { get; set; }
}
