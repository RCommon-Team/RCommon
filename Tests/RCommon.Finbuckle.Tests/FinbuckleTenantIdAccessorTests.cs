using Finbuckle.MultiTenant.Abstractions;
using FluentAssertions;
using RCommon.Finbuckle;
using Xunit;

namespace RCommon.Finbuckle.Tests;

/// <summary>
/// Uses Finbuckle's built-in <see cref="TenantInfo"/> class as the type argument throughout --
/// the specific case that failed to compile before the fix in
/// docs/specs/multi-tenancy/finbuckle-new-constraint.md (TenantInfo's required members are
/// incompatible with a `new()` generic constraint).
/// </summary>
public class FinbuckleTenantIdAccessorTests
{
    [Fact]
    public void Constructor_NullContextAccessor_ThrowsArgumentNullException()
    {
        var act = () => new FinbuckleTenantIdAccessor<TenantInfo>(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("contextAccessor");
    }

    [Fact]
    public void GetTenantId_NoResolvedContext_ReturnsNull()
    {
        var contextAccessor = new AsyncLocalMultiTenantContextAccessor<TenantInfo>();
        var accessor = new FinbuckleTenantIdAccessor<TenantInfo>(contextAccessor);

        accessor.GetTenantId().Should().BeNull();
    }

    [Fact]
    public void GetTenantId_ResolvedContext_ReturnsTenantInfoId()
    {
        var contextAccessor = new AsyncLocalMultiTenantContextAccessor<TenantInfo>();
        var tenant = new TenantInfo { Id = "tenant-a", Identifier = "acme" };

        // The setter is an explicit interface implementation (IMultiTenantContextSetter), separate
        // from IMultiTenantContextAccessor<T>'s read-only MultiTenantContext getter.
        ((IMultiTenantContextSetter)contextAccessor).MultiTenantContext = new MultiTenantContext<TenantInfo>(tenant);

        var accessor = new FinbuckleTenantIdAccessor<TenantInfo>(contextAccessor);

        accessor.GetTenantId().Should().Be("tenant-a");
    }
}
