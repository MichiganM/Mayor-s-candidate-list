using MayorElection.Domain.Entities;
using MayorElection.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MayorElection.Data.Configurations;

public sealed class CandidateStatusConfiguration : IEntityTypeConfiguration<CandidateStatusDefinition>
{
    public void Configure(EntityTypeBuilder<CandidateStatusDefinition> builder)
    {
        builder.ToTable("CandidateStatuses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.HasData(
            new CandidateStatusDefinition { Id = CandidateStatus.Registered, Name = "Зарегистрирован" },
            new CandidateStatusDefinition { Id = CandidateStatus.Withdrawn, Name = "Снят" },
            new CandidateStatusDefinition { Id = CandidateStatus.Rejected, Name = "Отклонён" });
    }
}
