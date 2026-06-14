using Microsoft.EntityFrameworkCore;
using Solution.Application.Abstractions.Persistence;
using Solution.Infrastructure.Persistence.Entities;

namespace Solution.Infrastructure.Persistence;

public sealed class ApplicationDatabaseContext(DbContextOptions<ApplicationDatabaseContext> options)
    : DbContext(options), IApplicationDatabaseContext
{
    public const string SchemaName = "app";

    public DbSet<UserRecord> Users => Set<UserRecord>();

    public DbSet<UserSessionRecord> UserSessions => Set<UserSessionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(global::Solution.Infrastructure.AssemblyReference.Assembly);
    }
}
