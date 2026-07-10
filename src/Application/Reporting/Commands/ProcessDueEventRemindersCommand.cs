using EventHub.Application.Abstractions.Messaging;

namespace EventHub.Application.Reporting.Commands;

public sealed record ProcessDueEventRemindersCommand : ICommand<ProcessDueEventRemindersResult>, IUnitOfWorkRequest;

public sealed record ProcessDueEventRemindersResult(int EventCount, int RecipientCount);
