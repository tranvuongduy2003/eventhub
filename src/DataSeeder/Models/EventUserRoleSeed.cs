namespace EventHub.DataSeeder.Models;

public sealed record EventUserRoleSeed(int EventId, Guid UserId, string Role);
