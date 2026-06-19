using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class EventLocationTests
{
    [Fact]
    public void EventLocation_Create_BothNull_Throws()
    {
        var act = () => EventLocation.Create(null, false);

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_LOCATION_REQUIRED");
    }

    [Fact]
    public void EventLocation_Create_EmptyAddressAndNotOnline_Throws()
    {
        var act = () => EventLocation.Create("   ", false);

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_LOCATION_REQUIRED");
    }

    [Fact]
    public void EventLocation_Create_BothProvided_Throws()
    {
        var act = () => EventLocation.Create("123 Main St", true);

        act.Should()
            .Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("EVENT_LOCATION_REQUIRED");
    }

    [Fact]
    public void EventLocation_Create_PhysicalOnly_SetsIsOnlineFalse()
    {
        var location = EventLocation.Create("123 Main St", false);

        location.PhysicalAddress.Should().Be("123 Main St");
        location.IsOnline.Should().BeFalse();
    }

    [Fact]
    public void EventLocation_Create_OnlineOnly_SetsPhysicalAddressNull()
    {
        var location = EventLocation.Create(null, true);

        location.PhysicalAddress.Should().BeNull();
        location.IsOnline.Should().BeTrue();
    }

    [Fact]
    public void EventLocation_Create_AddressTrimmed()
    {
        var location = EventLocation.Create("  123 Main St  ", false);

        location.PhysicalAddress.Should().Be("123 Main St");
    }
}
