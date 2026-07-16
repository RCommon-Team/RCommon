using Finbuckle.MultiTenant.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using RCommon.Finbuckle;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Finbuckle.Tests;

/// <summary>
/// Uses Finbuckle's built-in <see cref="TenantInfo"/> class as the type argument -- the specific case
/// that failed to compile before the fix in docs/specs/multi-tenancy/finbuckle-new-constraint.md.
/// </summary>
public class FinbuckleMultiTenantBuilderTests
{
    [Fact]
    public void Constructor_NullServices_ThrowsArgumentNullException()
    {
        var act = () => new FinbuckleMultiTenantBuilder<TenantInfo>(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_RegistersFinbuckleTenantIdAccessorAsITenantIdAccessor()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMultiTenantContextAccessor<TenantInfo>>(
            new AsyncLocalMultiTenantContextAccessor<TenantInfo>());

        _ = new FinbuckleMultiTenantBuilder<TenantInfo>(services);

        using var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<ITenantIdAccessor>();

        accessor.Should().BeOfType<TenantScopeAwareTenantIdAccessor>();
    }
}
