using System;
using FluentAssertions;
using RCommon.Persistence.Outbox;
using Xunit;

namespace RCommon.Persistence.Tests;

public class OutboxOptionsTests
{
    [Fact]
    public void OnDataStore_sets_DataStoreName_and_returns_same_instance_for_chaining()
    {
        var options = new OutboxOptions();

        var returned = options.OnDataStore("Billing");

        options.DataStoreName.Should().Be("Billing");
        returned.Should().BeSameAs(options);
    }

    [Fact]
    public void DataStoreName_is_null_by_default()
    {
        new OutboxOptions().DataStoreName.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OnDataStore_rejects_null_empty_or_whitespace(string? bad)
    {
        var options = new OutboxOptions();

        Action act = () => options.OnDataStore(bad!);

        act.Should().Throw<ArgumentException>();
    }
}
