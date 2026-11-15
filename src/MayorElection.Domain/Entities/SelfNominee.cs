using MayorElection.Domain.Enums;

namespace MayorElection.Domain.Entities;

public sealed class SelfNominee : Candidate
{
    public override CandidateTemplate Template => CandidateTemplate.SelfNominee;
}
