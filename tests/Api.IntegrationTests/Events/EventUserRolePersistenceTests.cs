using EventHub.Api.IntegrationTests.Integration;
using EventHub.Domain.Events;
using EventHub.Domain.Users;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Entities;
using EventHub.Testing.Common.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Api.IntegrationTests.Events;

[Collection(IntegrationTestCollection.Name)]
public sealed class EventUserRolePersistenceTests(IntegrationTestFixture fixture)
{
    [Fact]
    public async Task InsertEventUserRole_PersistsWithCorrectColumns()
    {
        var eventId = 1;
        var userId = await CreateUserAsync("owner@example.com");

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

            databaseContext.EventUserRoles.Add(new EventUserRoleRecord
            {
                EventId = eventId,
                UserId = userId,
                Role = EventRole.Owner,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            await databaseContext.SaveChangesAsync();
        }

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

            var saved = await databaseContext.EventUserRoles
                .AsNoTracking()
                .SingleAsync(eventUserRole => eventUserRole.EventId == eventId && eventUserRole.UserId == userId);

            saved.Role.Should().Be(EventRole.Owner);
            saved.CreatedAt.Should().BeAfter(DateTimeOffset.MinValue);
        }
    }

    [Fact]
    public async Task InsertDuplicateEventUserRole_ThrowsDueToCompositeKey()
    {
        var eventId = 2;
        var userId = await CreateUserAsync("duplicate@example.com");

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

            databaseContext.EventUserRoles.Add(new EventUserRoleRecord
            {
                EventId = eventId,
                UserId = userId,
                Role = EventRole.Owner,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            await databaseContext.SaveChangesAsync();
        }

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

            databaseContext.EventUserRoles.Add(new EventUserRoleRecord
            {
                EventId = eventId,
                UserId = userId,
                Role = EventRole.Staff,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            var act = () => databaseContext.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>();
        }
    }

    [Fact]
    public async Task CascadeDeleteUser_RemovesAssociatedEventUserRoles()
    {
        var eventId = 3;
        var userId = await CreateUserAsync("cascade@example.com");

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

            databaseContext.EventUserRoles.Add(new EventUserRoleRecord
            {
                EventId = eventId,
                UserId = userId,
                Role = EventRole.Staff,
                CreatedAt = DateTimeOffset.UtcNow,
            });

            await databaseContext.SaveChangesAsync();
        }

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

            var user = await databaseContext.Users.SingleAsync(user => user.Id == userId);
            databaseContext.Users.Remove(user);
            await databaseContext.SaveChangesAsync();
        }

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

            var roles = await databaseContext.EventUserRoles
                .AsNoTracking()
                .Where(eventUserRole => eventUserRole.UserId == userId)
                .ToListAsync();

            roles.Should().BeEmpty();
        }
    }

    [Fact]
    public async Task QueryByUserId_ReturnsUserRolesAcrossEvents()
    {
        var eventId1 = 4;
        var eventId2 = 5;
        var userId = await CreateUserAsync("multi@example.com");

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

            databaseContext.EventUserRoles.AddRange(
                new EventUserRoleRecord
                {
                    EventId = eventId1,
                    UserId = userId,
                    Role = EventRole.Owner,
                    CreatedAt = DateTimeOffset.UtcNow,
                },
                new EventUserRoleRecord
                {
                    EventId = eventId2,
                    UserId = userId,
                    Role = EventRole.Staff,
                    CreatedAt = DateTimeOffset.UtcNow,
                });

            await databaseContext.SaveChangesAsync();
        }

        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

            var roles = await databaseContext.EventUserRoles
                .AsNoTracking()
                .Where(eventUserRole => eventUserRole.UserId == userId)
                .ToListAsync();

            roles.Should().HaveCount(2);
            roles.Should().Contain(eventUserRole => eventUserRole.EventId == eventId1 && eventUserRole.Role == EventRole.Owner);
            roles.Should().Contain(eventUserRole => eventUserRole.EventId == eventId2 && eventUserRole.Role == EventRole.Staff);
        }
    }

    private async Task<Guid> CreateUserAsync(string email)
    {
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var databaseContext = scope.ServiceProvider.GetRequiredService<ApplicationDatabaseContext>();

        var userId = Guid.NewGuid();

        databaseContext.Users.Add(new UserRecord
        {
            Id = userId,
            DisplayName = "Test User",
            Email = email,
            PasswordHash = "hashed-password-stub",
            Role = UserRole.Organizer,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });

        await databaseContext.SaveChangesAsync();
        return userId;
    }
}
