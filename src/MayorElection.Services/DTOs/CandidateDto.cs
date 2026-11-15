using System.ComponentModel.DataAnnotations;
using MayorElection.Domain.Enums;

namespace MayorElection.Services.DTOs;

public sealed record CandidateDto
{
    public Guid Id { get; init; }
    [Required, MaxLength(100)] public string LastName { get; init; } = "";
    [Required, MaxLength(100)] public string FirstName { get; init; } = "";
    [MaxLength(100)] public string? Patronymic { get; init; }
    public DateOnly BirthDate { get; init; }
    public DateOnly RegistrationDate { get; init; }
    public CandidateStatus Status { get; init; } = CandidateStatus.Registered;
    public CandidateTemplate Template { get; init; } = CandidateTemplate.SelfNominee;
    public string? PartyName { get; init; }
    public int Position { get; init; }
}
