using MayorElection.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MayorElection.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<ElectionCommissionMember> CommissionMembers => Set<ElectionCommissionMember>();
    public DbSet<CandidateStatusDefinition> CandidateStatuses => Set<CandidateStatusDefinition>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
