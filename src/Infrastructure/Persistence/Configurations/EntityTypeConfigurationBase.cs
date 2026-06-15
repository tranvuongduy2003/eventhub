using EventHub.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventHub.Infrastructure.Persistence.Configurations;

public abstract class EntityTypeConfigurationBase<TAggregate, TIdentifier> : IEntityTypeConfiguration<TAggregate>
    where TAggregate : class, IAggregateRoot<TIdentifier>
    where TIdentifier : notnull
{
    public virtual void Configure(EntityTypeBuilder<TAggregate> builder)
    {
        builder.HasKey(aggregate => aggregate.Id);
        ConfigureAggregate(builder);
    }

    protected abstract void ConfigureAggregate(EntityTypeBuilder<TAggregate> builder);
}
