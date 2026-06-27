using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Events;

public sealed class SalesWindowTests
{
    private static readonly DateTimeOffset Start = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset End = new(2026, 7, 1, 18, 0, 0, TimeSpan.Zero);

    // --- SalesWindow.Create ---

    [Fact]
    public void Create_ValidStartAndEnd_CreatesSalesWindow()
    {
        var window = SalesWindow.Create(Start, End);

        window.Start.Should().Be(Start);
        window.End.Should().Be(End);
    }

    [Fact]
    public void Create_EndBeforeStart_ThrowsBusinessRuleValidationException()
    {
        var act = () => SalesWindow.Create(End, Start);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("SALES_WINDOW_INVALID");
    }

    [Fact]
    public void Create_EndEqualsStart_ThrowsBusinessRuleValidationException()
    {
        var act = () => SalesWindow.Create(Start, Start);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("SALES_WINDOW_INVALID");
    }

    // --- SalesWindow.IsOpen ---

    [Fact]
    public void IsOpen_DuringWindow_ReturnsTrue()
    {
        var window = SalesWindow.Create(Start, End);
        var now = new DateTimeOffset(2026, 7, 1, 14, 0, 0, TimeSpan.Zero);

        window.IsOpen(now).Should().BeTrue();
    }

    [Fact]
    public void IsOpen_BeforeWindow_ReturnsFalse()
    {
        var window = SalesWindow.Create(Start, End);
        var now = new DateTimeOffset(2026, 6, 30, 23, 59, 59, TimeSpan.Zero);

        window.IsOpen(now).Should().BeFalse();
    }

    [Fact]
    public void IsOpen_AfterWindow_ReturnsFalse()
    {
        var window = SalesWindow.Create(Start, End);
        var now = new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero);

        window.IsOpen(now).Should().BeFalse();
    }

    [Fact]
    public void IsOpen_AtStartBoundary_ReturnsTrue()
    {
        var window = SalesWindow.Create(Start, End);

        window.IsOpen(Start).Should().BeTrue();
    }

    [Fact]
    public void IsOpen_AtEndBoundary_ReturnsTrue()
    {
        var window = SalesWindow.Create(Start, End);

        window.IsOpen(End).Should().BeTrue();
    }

    // --- Equality ---

    [Fact]
    public void Equality_SameStartAndEnd_AreEqual()
    {
        var window1 = SalesWindow.Create(Start, End);
        var window2 = SalesWindow.Create(Start, End);

        window1.Should().Be(window2);
    }

    [Fact]
    public void Equality_DifferentStart_AreNotEqual()
    {
        var window1 = SalesWindow.Create(Start, End);
        var window2 = SalesWindow.Create(Start.AddHours(-2), End);

        window1.Should().NotBe(window2);
    }

    [Fact]
    public void Equality_DifferentEnd_AreNotEqual()
    {
        var window1 = SalesWindow.Create(Start, End);
        var window2 = SalesWindow.Create(Start, End.AddDays(1));

        window1.Should().NotBe(window2);
    }
}
