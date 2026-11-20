using Clinic.Domain.Patients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinic.Infrastructure.Persistence.Configurations;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.FullName).HasMaxLength(200);
        builder.Property(p => p.DocumentNumber).HasMaxLength(20);
        builder.Property(p => p.Phone).HasMaxLength(20);
        builder.HasIndex(p => new { p.TenantId, p.DocumentNumber }).IsUnique();
    }
}
