using MayorElection.Domain.Entities;
using MayorElection.Domain.Enums;
using MayorElection.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MayorElection.Data.Repositories;

public sealed class CandidateRepository(AppDbContext context) : ICandidateRepository
{
    public async Task<IReadOnlyList<Candidate>> ListAsync(CandidateStatus? status = null,
        CandidateTemplate? template = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Candidate> query = context.Candidates.AsNoTracking();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        query = template switch
        {
            null => query,
            CandidateTemplate.SelfNominee => query.OfType<SelfNominee>(),
            CandidateTemplate.PartyCandidate => query.OfType<PartyCandidate>(),
            _ => throw new ArgumentOutOfRangeException(nameof(template))
        };
        return await query.OrderBy(x => x.Position).ToListAsync(cancellationToken);
    }

    public Task<Candidate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Candidates.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AddAsync(Candidate candidate, CancellationToken cancellationToken = default) =>
        await context.Candidates.AddAsync(candidate, cancellationToken);

    public void Remove(Candidate candidate) => context.Candidates.Remove(candidate);
}
