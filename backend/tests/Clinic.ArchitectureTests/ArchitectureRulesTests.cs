using System.Reflection;
using Clinic.Domain;
using NetArchTest.Rules;

namespace Clinic.ArchitectureTests;

public class ArchitectureRulesTests
{
    private static readonly Assembly _domain = typeof(ITenantEntity).Assembly;
    private static readonly Assembly _api = typeof(Program).Assembly;

    [Fact]
    public void BE003_Domain_does_not_depend_on_efcore_aspnetcore_or_infrastructure()
    {
        var result = Types.InAssembly(_domain)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql", "Clinic.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful, "BE-003 violated by: " + Names(result));
    }

    [Fact]
    public void BE004_Api_does_not_reference_the_DbContext()
    {
        var result = Types.InAssembly(_api)
            .ShouldNot()
            .HaveDependencyOnAny("Clinic.Infrastructure.Persistence", "Microsoft.EntityFrameworkCore.DbContext")
            .GetResult();

        Assert.True(result.IsSuccessful, "BE-004 violated by: " + Names(result));
    }

    [Fact]
    public void BE005_Every_domain_entity_implements_ITenantEntity()
    {
        // Technical, non-business types may be allowlisted here with a justification in the PR.
        var allowlist = new HashSet<string>
        {
            // An enum value type, not a business entity (the test's AreClasses filter also matches enums).
            "Clinic.Domain.Appointments.AppointmentStatus",
        };

        var result = Types.InAssembly(_domain)
            .That()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveNameMatching(@"^<>|^<.*>")
            .Should()
            .ImplementInterface(typeof(ITenantEntity))
            .GetResult();

        var offenders = (result.FailingTypeNames ?? []).Where(n => !allowlist.Contains(n)).ToList();
        Assert.True(offenders.Count == 0, "BE-005 violated by: " + string.Join(", ", offenders));
    }

    private static string Names(TestResult result) => string.Join(", ", result.FailingTypeNames ?? []);
}
