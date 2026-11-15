using MayorElection.Domain.Entities;
using MayorElection.Domain.Enums;

namespace MayorElection.Domain.Repositories;

public interface ICandidateRepository
{
    Task<IReadOnlyList<Candidate>> ListAsync(CandidateStatus? status = null,
        CandidateTemplate? template = null, CancellationToken cancellationToken = default);
    Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Candidate candidate, CancellationToken cancellationToken = default);
    void Remove(Candidate candidate);
}
