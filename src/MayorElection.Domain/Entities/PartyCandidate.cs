using System.ComponentModel.DataAnnotations;
using MayorElection.Domain.Enums;

namespace MayorElection.Domain.Entities;

public sealed class PartyCandidate : Candidate
{
    [Required, MaxLength(200)] public string PartyName { get; set; } = string.Empty;
    public override CandidateTemplate Template => CandidateTemplate.PartyCandidate;
}
