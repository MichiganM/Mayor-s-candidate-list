using MayorElection.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MayorElection.Data.Configurations;

public sealed class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasDiscriminator<string>("ParticipantType")
            .HasValue<SelfNominee>("SelfNominee")
            .HasValue<PartyCandidate>("PartyCandidate")
            .HasValue<ElectionCommissionMember>("CommissionMember");
        builder.Property("ParticipantType").HasMaxLength(32);
        builder.ToTable("Participants", table =>
        {
            table.HasCheckConstraint("CK_Participant_Name",
                "length(trim(FirstName)) > 0 AND length(trim(LastName)) > 0");
            table.HasCheckConstraint("CK_Candidate_Position",
                "ParticipantType = 'CommissionMember' OR (Position IS NOT NULL AND Position > 0)");
            table.HasCheckConstraint("CK_Candidate_Status",
                "ParticipantType = 'CommissionMember' OR (Status IS NOT NULL AND RegistrationDate IS NOT NULL)");
            table.HasCheckConstraint("CK_PartyCandidate_Party",
                "ParticipantType <> 'PartyCandidate' OR (PartyName IS NOT NULL AND length(trim(PartyName)) > 0)");
            table.HasCheckConstraint("CK_CommissionMember_Details",
                "ParticipantType <> 'CommissionMember' OR (CommissionName IS NOT NULL AND length(trim(CommissionName)) > 0 AND Role IS NOT NULL AND length(trim(Role)) > 0)");
        });
    }
}
