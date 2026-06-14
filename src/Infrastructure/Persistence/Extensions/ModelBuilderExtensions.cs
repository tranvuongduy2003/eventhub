using Microsoft.EntityFrameworkCore;

namespace Solution.Infrastructure.Persistence.Extensions;

public static class ModelBuilderExtensions
{
    public static void UseApplicationSchema(
        this ModelBuilder modelBuilder,
        string schema = ApplicationDatabaseContext.SchemaName) =>
        modelBuilder.HasDefaultSchema(schema);
}
