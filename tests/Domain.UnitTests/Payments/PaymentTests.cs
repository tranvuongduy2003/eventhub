using EventHub.Domain.Events;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;
using EventHub.Domain.Payments;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Payments;

public sealed class PaymentTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 7, 5, 10, 0, 0, TimeSpan.Zero);
    private static readonly OrderId TestOrderId = OrderId.From(42);
    private static readonly PaymentId TestPaymentId = PaymentId.From(7);
    private static readonly Money Amount = Money.Create(120m, "VND");
    private static readonly ProviderReference ProviderReference = ProviderReference.Create("provider-123");

    [Fact]
    public void Initiate_NonZeroAmount_CreatesInitiatedPayment()
    {
        var payment = Payment.Initiate(TestOrderId, Amount, ProviderReference, StartedAt, TestPaymentId);

        payment.Id.Should().Be(TestPaymentId);
        payment.OrderId.Should().Be(TestOrderId);
        payment.Amount.Should().Be(Amount);
        payment.ProviderReference.Should().Be(ProviderReference);
        payment.Status.Should().Be(PaymentStatus.Initiated);
        payment.InitiatedAt.Should().Be(StartedAt);
    }

    [Fact]
    public void Initiate_ZeroAmount_ThrowsBusinessRuleValidationException()
    {
        var act = () => Payment.Initiate(
            TestOrderId,
            Money.Create(0, "VND"),
            ProviderReference,
            StartedAt,
            TestPaymentId);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("PAYMENT_AMOUNT_REQUIRED");
    }

    [Fact]
    public void Capture_InitiatedPayment_TransitionsToCapturedAndRaisesEvent()
    {
        var payment = CreatePayment();
        payment.ClearDomainEvents();
        var capturedAt = StartedAt.AddMinutes(1);

        var applied = payment.Capture(capturedAt);

        applied.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.CapturedAt.Should().Be(capturedAt);
        payment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PaymentCapturedEvent>();
    }

    [Fact]
    public void Capture_AlreadyCapturedPayment_IsIdempotent()
    {
        var payment = CreatePayment();
        payment.Capture(StartedAt.AddMinutes(1));
        payment.ClearDomainEvents();

        var applied = payment.Capture(StartedAt.AddMinutes(2));

        applied.Should().BeFalse();
        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Fail_InitiatedPayment_TransitionsToFailedAndRaisesEvent()
    {
        var payment = CreatePayment();
        payment.ClearDomainEvents();
        var failedAt = StartedAt.AddMinutes(1);

        var applied = payment.Fail(failedAt);

        applied.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.FailedAt.Should().Be(failedAt);
        payment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PaymentFailedEvent>();
    }

    [Fact]
    public void Fail_CapturedPayment_ThrowsBusinessRuleValidationException()
    {
        var payment = CreatePayment();
        payment.Capture(StartedAt.AddMinutes(1));

        var act = () => payment.Fail(StartedAt.AddMinutes(2));

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("PAYMENT_NOT_FAILABLE");
    }

    [Fact]
    public void ProviderReference_Blank_ThrowsBusinessRuleValidationException()
    {
        var act = () => ProviderReference.Create(" ");

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("PAYMENT_PROVIDER_REFERENCE_REQUIRED");
    }

    [Fact]
    public void PaymentId_Zero_ThrowsBusinessRuleValidationException()
    {
        var act = () => PaymentId.From(0);

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("PAYMENT_ID_INVALID");
    }

    private static Payment CreatePayment() =>
        Payment.Initiate(TestOrderId, Amount, ProviderReference, StartedAt, TestPaymentId);
}
