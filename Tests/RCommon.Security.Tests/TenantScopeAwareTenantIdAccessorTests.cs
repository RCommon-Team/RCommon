using System;
using FluentAssertions;
using Moq;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Security.Tests;

public class TenantScopeAwareTenantIdAccessorTests
{
    private readonly Mock<ITenantIdAccessor> _mockInner;

    public TenantScopeAwareTenantIdAccessorTests()
    {
        _mockInner = new Mock<ITenantIdAccessor>();
    }

    [Fact]
    public void Constructor_WithNullInner_ThrowsArgumentNullException()
    {
        var action = () => new TenantScopeAwareTenantIdAccessor(null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetTenantId_WithNoActiveBypassScope_DelegatesToInner()
    {
        _mockInner.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var accessor = new TenantScopeAwareTenantIdAccessor(_mockInner.Object);

        var result = accessor.GetTenantId();

        result.Should().Be("tenant-1");
    }

    [Fact]
    public void GetTenantId_WithActiveBypassScope_ReturnsNullWithoutCallingInner()
    {
        _mockInner.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var accessor = new TenantScopeAwareTenantIdAccessor(_mockInner.Object);

        using (TenantScope.Bypass())
        {
            var result = accessor.GetTenantId();

            result.Should().BeNull();
            _mockInner.Verify(x => x.GetTenantId(), Times.Never);
        }
    }

    [Fact]
    public void GetTenantId_AfterBypassScopeDisposed_DelegatesToInnerAgain()
    {
        _mockInner.Setup(x => x.GetTenantId()).Returns("tenant-1");
        var accessor = new TenantScopeAwareTenantIdAccessor(_mockInner.Object);

        using (TenantScope.Bypass())
        {
            accessor.GetTenantId();
        }

        var result = accessor.GetTenantId();

        result.Should().Be("tenant-1");
    }

    [Fact]
    public void TenantScopeAwareTenantIdAccessor_ImplementsITenantIdAccessor()
    {
        var accessor = new TenantScopeAwareTenantIdAccessor(_mockInner.Object);

        accessor.Should().BeAssignableTo<ITenantIdAccessor>();
    }
}
