using MayorElection.Domain.Enums;

namespace MayorElection.Domain.Entities;

public sealed class CandidateStatusDefinition
{
    public CandidateStatus Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
