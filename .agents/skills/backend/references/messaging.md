# Channel Messaging Reference

Use this reference for integration events, `IIntegrationEventPublisher`, Channel queues, typed consumers, and notification/projection side effects.

## Source Discovery

Start with the current implementation:

```powershell
rg -n "IIntegrationEventPublisher|ChannelIntegrationEvent|IIntegrationEventConsumer|QueuedIntegrationEvent" src tests
rg -n "IntegrationEvent|CreatedIntegrationEvent|ConfirmedIntegrationEvent|CheckedInIntegrationEvent" src/Domain src/Application src/Infrastructure tests
```

Read these files before editing messaging:

- `src/Application/Abstractions/Messaging/IIntegrationEventPublisher.cs`
- `src/Infrastructure/Messaging/ChannelIntegrationEventQueue.cs`
- `src/Infrastructure/Messaging/ChannelIntegrationEventPublisher.cs`
- `src/Infrastructure/Messaging/ChannelIntegrationEventConsumerService.cs`
- `src/Infrastructure/Messaging/IIntegrationEventConsumer.cs`
- the concrete `*IntegrationEventConsumer.cs` and event record involved
- `src/Infrastructure/DependencyInjection.cs`

## Ownership

- Integration event records are facts. Keep them small, explicit, and free of transport details.
- Application code publishes through `IIntegrationEventPublisher`; it must not depend on `System.Threading.Channels` or Infrastructure queue types.
- Infrastructure owns the Channel queue, publisher adapter, hosted dispatcher, typed consumers, email/payment/storage adapters, and DI registrations.
- Do not reintroduce RabbitMQ, AMQP, broker packages, broker connection strings, or Docker Compose.
- Do not use `NoOp` classes, names, provider references, docs, tests, or silent fallback behavior. Prefer a real local adapter or fail-fast configuration error.

## Publisher Pattern

- Inject `IIntegrationEventPublisher` into the application handler that owns the state change.
- Publish a concrete integration event record, not an anonymous object or domain entity.
- Do not pass aggregate instances, EF records, raw uploaded content, credentials, session tokens, payment secrets, or full ticket codes.
- Keep cancellation flowing from the command handler.

Example shape:

```csharp
await integrationEventPublisher.PublishAsync(
    new SomeIntegrationEvent(entityId, occurredAt),
    cancellationToken);
```

## Channel Queue Pattern

- Use `Channel<T>` explicitly for the in-memory queue.
- Queue envelopes should carry the payload object and its runtime event type.
- Keep the queue generic and infrastructure-only.
- Expose read APIs needed by the dispatcher, not business-specific helper methods.

The queue must not know about any concrete event such as `InvitationCreatedIntegrationEvent`.

## Consumer Pattern

Keep the generic hosted service as a dispatcher only:

- Read `QueuedIntegrationEvent` from the Channel queue.
- Resolve `IIntegrationEventConsumer<TIntegrationEvent>` by runtime event type.
- Log missing consumers by event type only.
- Log failures by event type only; do not log payloads because they may contain tokens or sensitive data.
- Do not put `switch` statements for concrete business events in the dispatcher.

Put each event-specific behavior in its own typed consumer:

```csharp
public sealed class SomeIntegrationEventConsumer(IDependency dependency)
    : IntegrationEventConsumer<SomeIntegrationEvent>
{
    public override Task ConsumeAsync(
        SomeIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        // event-specific side effect
    }
}
```

Register each typed consumer explicitly in `src/Infrastructure/DependencyInjection.cs`:

```csharp
services.AddScoped<
    IIntegrationEventConsumer<SomeIntegrationEvent>,
    SomeIntegrationEventConsumer>();
```

## Design Guardrails

- Do not combine the generic Channel dispatcher and a concrete event consumer in one class.
- Do not inject event-specific dependencies, such as `IEmailSender`, into the generic dispatcher.
- Do not add fallback consumers that swallow events.
- Do not make consumers depend on HTTP, API endpoint types, or EF entities directly.
- If a side effect needs persistence, go through an Application port or Infrastructure repository pattern already present in the codebase.
- Consumers must be idempotent when repeated delivery can happen or when the side effect can be retried.

## Verification

For messaging changes, run at least:

```powershell
rg -n "RabbitMQ|rabbitmq|AMQP|amqp|NoOp|no-op|noop" .
dotnet build EventHub.slnx -c Release
dotnet test tests/Api.IntegrationTests/EventHub.Api.IntegrationTests.csproj -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File scripts/agent/Verify-ChangedCode.ps1
```

Add or update focused tests when behavior changes:

- Publisher enqueues into `Channel<T>`.
- Dispatcher remains generic and has no concrete event switch.
- Concrete typed consumer performs the intended side effect.
- DI registers the queue, publisher, dispatcher, and typed consumer.
