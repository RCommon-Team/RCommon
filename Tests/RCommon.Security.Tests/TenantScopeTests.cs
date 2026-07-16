using System.Threading.Tasks;
using FluentAssertions;
using RCommon.Security.Claims;
using Xunit;

namespace RCommon.Security.Tests;

public class TenantScopeTests
{
    [Fact]
    public void IsBypassed_WithNoActiveScope_IsFalse()
    {
        TenantScope.IsBypassed.Should().BeFalse();
    }

    [Fact]
    public void Bypass_WhileScopeIsActive_IsBypassedIsTrue()
    {
        using (TenantScope.Bypass())
        {
            TenantScope.IsBypassed.Should().BeTrue();
        }
    }

    [Fact]
    public void Bypass_AfterDisposal_RestoresPreviousState()
    {
        var scope = TenantScope.Bypass();
        scope.Dispose();

        TenantScope.IsBypassed.Should().BeFalse();
    }

    [Fact]
    public void Bypass_NestedScopes_DisposingInnerScopeLeavesOuterScopeBypassed()
    {
        using (TenantScope.Bypass())
        {
            using (TenantScope.Bypass())
            {
                TenantScope.IsBypassed.Should().BeTrue();
            }

            // Disposing the inner scope must restore "bypassed" (the outer scope's state),
            // not unconditionally reset to "not bypassed".
            TenantScope.IsBypassed.Should().BeTrue();
        }

        TenantScope.IsBypassed.Should().BeFalse();
    }

    [Fact]
    public void Bypass_DisposingTwice_IsIdempotent()
    {
        var scope = TenantScope.Bypass();
        scope.Dispose();
        var action = () => scope.Dispose();

        action.Should().NotThrow();
        TenantScope.IsBypassed.Should().BeFalse();
    }

    [Fact]
    public async Task Bypass_FlowsAcrossAwaitContinuation()
    {
        using (TenantScope.Bypass())
        {
            await Task.Yield();
            await Task.Delay(1);

            TenantScope.IsBypassed.Should().BeTrue();
        }
    }
}
