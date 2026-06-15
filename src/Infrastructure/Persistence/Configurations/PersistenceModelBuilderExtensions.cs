using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

internal static class PersistenceModelBuilderExtensions
{
    public static PropertyBuilder<decimal> AsMoney(this PropertyBuilder<decimal> propertyBuilder) =>
        propertyBuilder.HasColumnType("numeric(18,4)");

    public static PropertyBuilder<decimal?> AsMoney(this PropertyBuilder<decimal?> propertyBuilder) =>
        propertyBuilder.HasColumnType("numeric(18,4)");

    public static PropertyBuilder<long> AsRowVersion(this PropertyBuilder<long> propertyBuilder) =>
        propertyBuilder
            .HasColumnName("row_version")
            .HasDefaultValue(1L)
            .IsConcurrencyToken();
}
