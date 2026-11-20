using Clinic.Domain.Appointments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clinic.Infrastructure.Persistence.Configurations;

internal sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(a => new { a.TenantId, a.DentalChairId, a.StartsAt });
        builder.HasIndex(a => a.PatientId);
    }
}
