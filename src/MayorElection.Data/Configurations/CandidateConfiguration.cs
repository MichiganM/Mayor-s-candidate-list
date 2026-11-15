using MayorElection.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MayorElection.Data.Configurations;

public sealed class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.Ignore(x => x.Template);
        builder.HasIndex(x => x.Position).IsUnique().HasFilter("Position IS NOT NULL");
        builder.HasOne<CandidateStatusDefinition>().WithMany().HasForeignKey(x => x.Status)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
