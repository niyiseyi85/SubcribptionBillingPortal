using FluentAssertions;
using SubscriptionBillingPortal.Shared.Pagination;
using SubscriptionBillingPortal.Shared.Responses;

namespace SubscriptionBillingPortal.UnitTests.Application.Shared;

/// <summary>
/// Unit tests for shared types: ApiResponse and PaginatedResult.
/// </summary>
public sealed class SharedTypesTests
{
    // ── ApiResponse ───────────────────────────────────────────────────────────

    [Fact]
    public void ApiResponse_Ok_ShouldHaveSuccessTrueAndStatusCode200()
    {
        var response = ApiResponse<string>.Ok("hello");

        response.Success.Should().BeTrue();
        response.StatusCode.Should().Be(200);
        response.Data.Should().Be("hello");
    }

    [Fact]
    public void ApiResponse_Created_ShouldHaveSuccessTrueAndStatusCode201()
    {
        var response = ApiResponse<string>.Created("created");

        response.Success.Should().BeTrue();
        response.StatusCode.Should().Be(201);
    }

    [Fact]
    public void ApiResponse_Fail_ShouldHaveSuccessFalseAndNullData()
    {
        var response = ApiResponse<string>.Fail("error occurred", 404);

        response.Success.Should().BeFalse();
        response.StatusCode.Should().Be(404);
        response.Data.Should().BeNull();
        response.Message.Should().Be("error occurred");
    }

    // ── PaginatedResult ───────────────────────────────────────────────────────

    [Fact]
    public void PaginatedResult_Create_ShouldCalculateTotalPagesCorrectly()
    {
        var items = Enumerable.Range(1, 5).Select(i => i.ToString());

        var result = PaginatedResult<string>.Create(items, totalCount: 25, pageNumber: 2, pageSize: 5);

        result.TotalPages.Should().Be(5);
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(2);
        result.PageSize.Should().Be(5);
        result.Items.Should().HaveCount(5);
    }

    [Fact]
    public void PaginatedResult_HasPreviousPage_ShouldBeFalseOnFirstPage()
    {
        var result = PaginatedResult<string>.Create(["a"], totalCount: 10, pageNumber: 1, pageSize: 5);

        result.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedResult_HasNextPage_ShouldBeFalseOnLastPage()
    {
        var result = PaginatedResult<string>.Create(["a"], totalCount: 1, pageNumber: 1, pageSize: 5);

        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedResult_HasNextPage_ShouldBeTrueWhenMorePagesExist()
    {
        var result = PaginatedResult<string>.Create(["a"], totalCount: 10, pageNumber: 1, pageSize: 5);

        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedRequest_PageNumber_ShouldDefaultToOneWhenBelowMinimum()
    {
        var request = new PaginatedRequest { PageNumber = -5 };

        request.PageNumber.Should().Be(1);
    }

    [Fact]
    public void PaginatedRequest_PageSize_ShouldCapAtMaximum()
    {
        var request = new PaginatedRequest { PageSize = 999 };

        request.PageSize.Should().Be(100);
    }
}
