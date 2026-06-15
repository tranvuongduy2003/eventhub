using EventHub.Application.Common;
using EventHub.Testing.Common.Assertions;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Common;

public class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();
        result.ShouldBeSuccess();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_CarriesErrorCode()
    {
        var error = Error.NotFound("ORDER_NOT_FOUND", "Order was not found.");
        var result = Result.Failure(error);

        result.ShouldBeFailure("ORDER_NOT_FOUND");
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void GenericSuccess_ReturnsValue()
    {
        var result = Result<Guid>.Success(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        result.ShouldBeSuccess(v => v.Should().NotBeEmpty());
    }
}
