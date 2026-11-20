using Clinic.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Clinic.Infrastructure.Persistence;

/// <summary>Used only by `dotnet ef`. The connection string is a throwaway local value, never connected to.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ClinicDbContext>
{
    public ClinicDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseNpgsql("Host=localhost;Database=clinic_design_time")
            .Options;
        return new ClinicDbContext(options, new NoTenant());
    }

    private sealed class NoTenant : ITenantContext
    {
        public bool HasTenant => false;

        public Guid TenantId => Guid.Empty;
    }
}
