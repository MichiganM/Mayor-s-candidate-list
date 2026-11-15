using System.ComponentModel.DataAnnotations;
using MayorElection.Domain.Enums;

namespace MayorElection.Domain.Entities;

public abstract class Candidate : Participant
{
    [Range(1, int.MaxValue)] public int Position { get; set; }
    public DateOnly RegistrationDate { get; set; }
    public CandidateStatus Status { get; set; } = CandidateStatus.Registered;
    public abstract CandidateTemplate Template { get; }
}
