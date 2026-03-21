using System;
using System.Collections.Generic;
using FluentAssertions;
using RCommon.Models;
using Xunit;

namespace RCommon.Models.Tests;

public class PagedResultTests
{
    [Fact]
    public void Constructor_Sets_Properties()
    {
        var items = new List<string> { "a", "b", "c" };
        var result = new PagedResult<string>(items, 10, 1, 5);

        result.Items.Should().BeEquivalentTo(items);
        result.TotalCount.Should().Be(10);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(5);
    }

    [Fact]
    public void TotalPages_Rounds_Up()
    {
        var result = new PagedResult<string>(new List<string>(), 11, 1, 5);
        result.TotalPages.Should().Be(3); // ceil(11/5) = 3
    }

    [Fact]
    public void TotalPages_Exact_Division()
    {
        var result = new PagedResult<string>(new List<string>(), 10, 1, 5);
        result.TotalPages.Should().Be(2);
    }

    [Fact]
    public void HasNextPage_True_When_Not_Last_Page()
    {
        var result = new PagedResult<string>(new List<string>(), 10, 1, 5);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_False_On_Last_Page()
    {
        var result = new PagedResult<string>(new List<string>(), 10, 2, 5);
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_False_On_First_Page()
    {
        var result = new PagedResult<string>(new List<string>(), 10, 1, 5);
        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_True_On_Page_2()
    {
        var result = new PagedResult<string>(new List<string>(), 10, 2, 5);
        result.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void Constructor_Throws_When_PageSize_Zero()
    {
        var act = () => new PagedResult<string>(new List<string>(), 10, 1, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_Throws_When_PageSize_Negative()
    {
        var act = () => new PagedResult<string>(new List<string>(), 10, 1, -1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Empty_Result_Has_Zero_TotalPages()
    {
        var result = new PagedResult<string>(new List<string>(), 0, 1, 10);
        result.TotalPages.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
    }
}
