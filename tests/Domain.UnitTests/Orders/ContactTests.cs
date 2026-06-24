using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;
using FluentAssertions;

namespace EventHub.Domain.UnitTests.Orders;

public sealed class ContactTests
{
    // --- Contact.Create ---

    [Fact]
    public void Create_ValidInput_CreatesContact()
    {
        var contact = Contact.Create("John Doe", "john@example.com");

        contact.Name.Should().Be("John Doe");
        contact.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void Create_TrimsName()
    {
        var contact = Contact.Create("  John Doe  ", "john@example.com");

        contact.Name.Should().Be("John Doe");
    }

    [Fact]
    public void Create_NormalizesEmailToLowercase()
    {
        var contact = Contact.Create("John Doe", "John@Example.COM");

        contact.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void Create_TrimsEmail()
    {
        var contact = Contact.Create("John Doe", "  john@example.com  ");

        contact.Email.Should().Be("john@example.com");
    }

    [Fact]
    public void Create_EmptyName_ThrowsBusinessRuleValidationException()
    {
        var act = () => Contact.Create("", "john@example.com");

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("CONTACT_NAME_REQUIRED");
    }

    [Fact]
    public void Create_WhitespaceName_ThrowsBusinessRuleValidationException()
    {
        var act = () => Contact.Create("   ", "john@example.com");

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("CONTACT_NAME_REQUIRED");
    }

    [Fact]
    public void Create_NameExceeds200Characters_ThrowsBusinessRuleValidationException()
    {
        var longName = new string('A', 201);

        var act = () => Contact.Create(longName, "john@example.com");

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("CONTACT_NAME_TOO_LONG");
    }

    [Fact]
    public void Create_NameExactly200Characters_Succeeds()
    {
        var name = new string('A', 200);

        var contact = Contact.Create(name, "john@example.com");

        contact.Name.Should().HaveLength(200);
    }

    [Fact]
    public void Create_EmptyEmail_ThrowsBusinessRuleValidationException()
    {
        var act = () => Contact.Create("John Doe", "");

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("CONTACT_EMAIL_REQUIRED");
    }

    [Fact]
    public void Create_WhitespaceEmail_ThrowsBusinessRuleValidationException()
    {
        var act = () => Contact.Create("John Doe", "   ");

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("CONTACT_EMAIL_REQUIRED");
    }

    [Fact]
    public void Create_InvalidEmail_ThrowsBusinessRuleValidationException()
    {
        var act = () => Contact.Create("John Doe", "not-an-email");

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("CONTACT_EMAIL_INVALID");
    }

    [Fact]
    public void Create_EmailMissingAtSign_ThrowsBusinessRuleValidationException()
    {
        var act = () => Contact.Create("John Doe", "johnexample.com");

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("CONTACT_EMAIL_INVALID");
    }

    [Fact]
    public void Create_EmailMissingDomain_ThrowsBusinessRuleValidationException()
    {
        var act = () => Contact.Create("John Doe", "john@");

        act.Should().Throw<BusinessRuleValidationException>()
            .Which.Code.Should().Be("CONTACT_EMAIL_INVALID");
    }

    // --- Equality ---

    [Fact]
    public void SameValues_AreEqual()
    {
        var contact1 = Contact.Create("John Doe", "john@example.com");
        var contact2 = Contact.Create("John Doe", "john@example.com");

        contact1.Should().Be(contact2);
    }

    [Fact]
    public void DifferentNames_AreNotEqual()
    {
        var contact1 = Contact.Create("John Doe", "john@example.com");
        var contact2 = Contact.Create("Jane Doe", "john@example.com");

        contact1.Should().NotBe(contact2);
    }

    [Fact]
    public void DifferentEmails_AreNotEqual()
    {
        var contact1 = Contact.Create("John Doe", "john@example.com");
        var contact2 = Contact.Create("John Doe", "jane@example.com");

        contact1.Should().NotBe(contact2);
    }
}
