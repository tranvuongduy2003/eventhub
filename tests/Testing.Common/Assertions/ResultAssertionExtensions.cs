using EventHub.Application.Common;
using FluentAssertions;

namespace EventHub.Testing.Common.Assertions;

public static class ResultAssertionExtensions
{
    public static void ShouldBeSuccess(this Result result) =>
        result.IsSuccess.Should().BeTrue(result.Error?.Message);

    public static void ShouldBeFailure(this Result result, string? expectedCode = null)
    {
        result.IsFailure.Should().BeTrue();
        if (expectedCode is not null)
        {
            result.Error!.Code.Should().Be(expectedCode);
        }
    }

    public static void ShouldBeSuccess<T>(this Result<T> result, Action<T>? assertValue = null)
    {
        result.IsSuccess.Should().BeTrue(result.Error?.Message);
        if (assertValue is not null)
        {
            assertValue(result.Value!);
        }
    }

    public static void ShouldBeFailure<T>(this Result<T> result, string? expectedCode = null)
    {
        result.IsFailure.Should().BeTrue();
        if (expectedCode is not null)
        {
            result.Error!.Code.Should().Be(expectedCode);
        }
    }
}
