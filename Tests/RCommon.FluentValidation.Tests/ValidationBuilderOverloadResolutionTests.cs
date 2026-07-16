using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RCommon.ApplicationServices;
using RCommon.ApplicationServices.Validation;
using RCommon.FluentValidation;
using Xunit;

namespace RCommon.FluentValidation.Tests;

/// <summary>
/// Locks in the fix from docs/specs/validation/validation-builder-api.md: RCommon.FluentValidation's
/// byte-for-byte duplicate of the zero-arg WithValidation&lt;T&gt;() overload has been deleted, so
/// these calls -- previously ambiguous (CS0121) whenever both RCommon.ApplicationServices and
/// RCommon.FluentValidation were referenced with RCommon.ApplicationServices in scope -- now compile
/// and resolve unambiguously to RCommon.ApplicationServices.ValidationBuilderExtensions.
/// </summary>
public class ValidationBuilderOverloadResolutionTests
{
    [Fact]
    public void WithValidation_ZeroArgOverload_CompilesAndRegistersValidationProvider()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act -- previously CS0121 (unconditionally ambiguous) with both packages referenced
        var result = rcommonBuilder.WithValidation<FluentValidationBuilder>();

        // Assert
        result.Should().BeSameAs(rcommonBuilder);
        var provider = services.BuildServiceProvider();
        provider.GetService<IValidationProvider>().Should().NotBeNull();
    }

    [Fact]
    public void WithValidation_LambdaTypeCheckingOnlyAgainstBuilder_ResolvesUnambiguously()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act -- a lambda body that only type-checks against Action<FluentValidationBuilder>
        // (referencing a member -- Services -- that CqrsValidationOptions doesn't have) resolves
        // unambiguously to RCommon.ApplicationServices' Action<T> overload. This is the shape every
        // real call site uses in practice (registering validators, calling UseWithCqrs, etc.).
        //
        // Note: a bare discard lambda -- .WithValidation<FluentValidationBuilder>(_ => {}) -- is
        // NOT fixed by this change and remains genuinely ambiguous (CS0121): it type-checks against
        // both Action<FluentValidationBuilder> and Action<CqrsValidationOptions>, and per the spec's
        // own "Must Not Do" section, the still-documented, still-in-use
        // WithValidation<T>(Action<CqrsValidationOptions>) overload is deprecated, not deleted, this
        // release -- so that overload remains a live, competing candidate for any lambda body
        // convertible to both delegate types.
        var result = rcommonBuilder.WithValidation<FluentValidationBuilder>(v => { _ = v.Services; });

        // Assert -- resolves to RCommon.ApplicationServices' Action<T> overload, which registers
        // IValidationProvider; the (now-Obsolete) Action<CqrsValidationOptions> overload does not.
        var provider = services.BuildServiceProvider();
        provider.GetService<IValidationProvider>().Should().NotBeNull();
    }

    [Fact]
    public void UseWithCqrs_CalledInsideWithValidationLambda_ConfiguresCqrsValidationOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

        // Act -- the recommended replacement for the obsolete WithValidation<T>(Action<CqrsValidationOptions>)
        rcommonBuilder.WithValidation<FluentValidationBuilder>(v =>
        {
            v.UseWithCqrs(opts =>
            {
                opts.ValidateCommands = true;
                opts.ValidateQueries = true;
            });
        });

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CqrsValidationOptions>>().Value;
        options.ValidateCommands.Should().BeTrue();
        options.ValidateQueries.Should().BeTrue();
    }

    [Fact]
    [System.Obsolete]
    public void WithValidation_ObsoleteCqrsOptionsOverload_StillConfiguresOptions()
    {
        // Arrange -- the deprecated overload remains fully functional; only [Obsolete] was added.
        var services = new ServiceCollection();
        services.AddLogging();
        var rcommonBuilder = new RCommonBuilder(services);

#pragma warning disable CS0618 // Type or member is obsolete
        rcommonBuilder.WithValidation<FluentValidationBuilder>(opts =>
        {
            opts.ValidateCommands = true;
        });
#pragma warning restore CS0618

        // Assert
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<CqrsValidationOptions>>().Value;
        options.ValidateCommands.Should().BeTrue();
    }
}
