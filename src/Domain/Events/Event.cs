using EventHub.Domain.Abstractions;
using EventHub.Domain.Exceptions;
using EventHub.Domain.Orders;
using EventHub.Domain.Users;

namespace EventHub.Domain.Events;

public sealed class Event : AggregateRoot<EventId>
{
    private readonly List<Occurrence> _occurrences = [];
    private readonly List<TicketType> _ticketTypes = [];
    private readonly List<Reservation> _reservations = [];

    private Event()
    {
    }

    public UserId OrganizerId { get; private set; }

    public IReadOnlyCollection<Occurrence> Occurrences => _occurrences.AsReadOnly();

    public IReadOnlyCollection<TicketType> TicketTypes => _ticketTypes.AsReadOnly();

    public IReadOnlyCollection<Reservation> Reservations => _reservations.AsReadOnly();

    public EventTitle Title { get; private set; } = null!;

    public EventSchedule? Schedule { get; private set; }

    public EventLocation Location { get; private set; } = null!;

    public string? Description { get; private set; }

    public EventStatus Status { get; private set; }

    public Slug? Slug { get; private set; }

    public CoverImageRef? CoverImageRef { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public long RowVersion { get; private set; }

    public static Event CreateDraft(
        UserId organizerId,
        EventTitle title,
        EventSchedule schedule,
        EventLocation location,
        DateTimeOffset createdAt)
    {
        var draftEvent = new Event
        {
            OrganizerId = organizerId,
            Title = title,
            Schedule = schedule,
            Location = location,
            Description = null,
            Status = EventStatus.Draft,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            RowVersion = 1,
        };

        return draftEvent;
    }

    public void MarkAsPersisted()
    {
        Raise(new EventCreatedEvent(Id, OrganizerId));
    }

    public Event Duplicate(UserId organizerId, DateTimeOffset createdAt)
    {
        return new Event
        {
            OrganizerId = organizerId,
            Title = EventTitle.Create($"Copy of {Title.Value}"),
            Schedule = null,
            Location = Location,
            Description = Description,
            Status = EventStatus.Draft,
            Slug = null,
            CoverImageRef = CoverImageRef,
            CancelledAt = null,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            RowVersion = 1,
        };
    }

    public void SetCoverImage(CoverImageRef coverImageRef)
    {
        CoverImageRef = coverImageRef;
    }

    public void Publish(Slug slug, DateTimeOffset publishedAt)
    {
        if (Status is not EventStatus.Draft)
        {
            throw new BusinessRuleValidationException(
                "EVENT_NOT_PUBLISHABLE",
                Status switch
                {
                    EventStatus.Published => "The event is already published.",
                    EventStatus.Closed => "Cannot publish a closed event.",
                    EventStatus.Cancelled => "Cannot publish a cancelled event.",
                    _ => "The event cannot be published in its current status.",
                });
        }

        if (_ticketTypes.Count == 0)
        {
            throw new BusinessRuleValidationException(
                "EVENT_REQUIRES_TICKET_TYPE",
                "At least one ticket type is required before publishing.");
        }

        Status = EventStatus.Published;
        Slug = slug;
        UpdatedAt = publishedAt;

        Raise(new EventPublishedEvent(Id, slug));
    }

    public void Close(DateTimeOffset closedAt)
    {
        if (Status is not EventStatus.Published)
        {
            throw new BusinessRuleValidationException(
                "EVENT_NOT_CLOSABLE",
                Status switch
                {
                    EventStatus.Draft => "Cannot close a draft event.",
                    EventStatus.Closed => "The event is already closed.",
                    EventStatus.Cancelled => "Cannot close a cancelled event.",
                    _ => "The event cannot be closed in its current status.",
                });
        }

        Status = EventStatus.Closed;
        UpdatedAt = closedAt;

        Raise(new EventClosedEvent(Id, closedAt));
    }

    public void Cancel(DateTimeOffset cancelledAt)
    {
        if (Status is EventStatus.Draft or EventStatus.Cancelled)
        {
            throw new BusinessRuleValidationException(
                "EVENT_NOT_CANCELLABLE",
                Status switch
                {
                    EventStatus.Draft => "Cannot cancel a draft event.",
                    EventStatus.Cancelled => "The event is already cancelled.",
                    _ => "The event cannot be cancelled in its current status.",
                });
        }

        Status = EventStatus.Cancelled;
        CancelledAt = cancelledAt;
        UpdatedAt = cancelledAt;

        Raise(new EventCancelledEvent(Id, cancelledAt));
    }

    public void UpdateDetails(
        EventTitle title,
        EventSchedule schedule,
        EventLocation location,
        string? description,
        DateTimeOffset updatedAt)
    {
        if (Status is EventStatus.Closed or EventStatus.Cancelled)
        {
            throw new BusinessRuleValidationException(
                "EVENT_CLOSED_OR_CANCELLED",
                "Cannot edit a closed or cancelled event.");
        }

        Title = title;
        Schedule = schedule;
        Location = location;
        Description = description;
        UpdatedAt = updatedAt;
    }

    public Occurrence ScheduleOccurrence(
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? venueName,
        string? address,
        DateTimeOffset createdAt)
    {
        if (Status is EventStatus.Closed or EventStatus.Cancelled)
        {
            throw new BusinessRuleValidationException(
                "EVENT_CLOSED_OR_CANCELLED",
                "Cannot add occurrences to a closed or cancelled event.");
        }

        var overlapping = _occurrences.Any(o =>
            startsAt < o.EndsAt && endsAt > o.StartsAt);

        if (overlapping)
        {
            throw new BusinessRuleValidationException(
                "OCCURRENCE_OVERLAPS",
                "The occurrence overlaps with an existing occurrence.");
        }

        var occurrence = Occurrence.Schedule(startsAt, endsAt, venueName, address, createdAt);

        _occurrences.Add(occurrence);

        UpdatedAt = createdAt;

        Raise(new OccurrenceScheduledEvent(Id, occurrence.Id, startsAt, endsAt));

        return occurrence;
    }

    public void RescheduleOccurrence(
        OccurrenceId occurrenceId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        string? venueName,
        string? address,
        DateTimeOffset updatedAt)
    {
        if (Status is EventStatus.Closed or EventStatus.Cancelled)
        {
            throw new BusinessRuleValidationException(
                "EVENT_CLOSED_OR_CANCELLED",
                "Cannot edit occurrences on a closed or cancelled event.");
        }

        var occurrence = _occurrences.FirstOrDefault(o => o.Id == occurrenceId)
            ?? throw new BusinessRuleValidationException(
                "OCCURRENCE_NOT_FOUND",
                "The occurrence was not found.");

        var overlapping = _occurrences.Any(o =>
            o.Id != occurrenceId &&
            startsAt < o.EndsAt && endsAt > o.StartsAt);

        if (overlapping)
        {
            throw new BusinessRuleValidationException(
                "OCCURRENCE_OVERLAPS",
                "The updated occurrence would overlap with an existing occurrence.");
        }

        occurrence.Reschedule(startsAt, endsAt, venueName, address, updatedAt);

        UpdatedAt = updatedAt;

        Raise(new OccurrenceUpdatedEvent(Id, occurrenceId, startsAt, endsAt));
    }

    public void RemoveOccurrence(OccurrenceId occurrenceId, DateTimeOffset updatedAt)
    {
        if (Status is EventStatus.Closed or EventStatus.Cancelled)
        {
            throw new BusinessRuleValidationException(
                "EVENT_CLOSED_OR_CANCELLED",
                "Cannot remove occurrences from a closed or cancelled event.");
        }

        var occurrence = _occurrences.FirstOrDefault(o => o.Id == occurrenceId)
            ?? throw new BusinessRuleValidationException(
                "OCCURRENCE_NOT_FOUND",
                "The occurrence was not found.");

        _occurrences.Remove(occurrence);

        UpdatedAt = updatedAt;

        Raise(new OccurrenceRemovedEvent(Id, occurrenceId));
    }

    public void LoadOccurrences(List<Occurrence> occurrences)
    {
        _occurrences.Clear();
        _occurrences.AddRange(occurrences);
    }

    public TicketType AddTicketType(
        TicketName name,
        Money price,
        Capacity capacity,
        DateTimeOffset createdAt)
    {
        if (Status is not EventStatus.Draft)
        {
            throw new BusinessRuleValidationException(
                "INVALID_EVENT_STATUS",
                Status switch
                {
                    EventStatus.Published => "Cannot add ticket types to a published event.",
                    EventStatus.Closed => "Cannot add ticket types to a closed event.",
                    EventStatus.Cancelled => "Cannot add ticket types to a cancelled event.",
                    _ => "Cannot add ticket types to an event in its current status.",
                });
        }

        // AC-11: maximum 10 ticket types per event
        if (_ticketTypes.Count >= 10)
        {
            throw new BusinessRuleValidationException(
                "TICKET_TYPE_MAX_REACHED",
                "An event cannot have more than 10 ticket types.");
        }

        // EC-01: duplicate name check
        if (_ticketTypes.Any(t => t.Name.Value == name.Value))
        {
            throw new BusinessRuleValidationException(
                "TICKET_TYPE_NAME_DUPLICATE",
                $"A ticket type with the name '{name.Value}' already exists on this event.");
        }

        // Assign negative IDs for new types — the database generates positive IDs on persist.
        // Negative IDs ensure uniqueness in-memory without conflicting with DB-generated IDs.
        var minId = _ticketTypes.Count > 0
            ? _ticketTypes.Min(t => t.Id.Value)
            : 0;
        var nextId = minId < 0 ? TicketTypeId.From(minId - 1) : TicketTypeId.From(-1);

        var ticketType = TicketType.Create(nextId, name, price, capacity, createdAt);

        _ticketTypes.Add(ticketType);

        UpdatedAt = createdAt;

        Raise(new TicketTypeAddedEvent(Id, ticketType.Id));

        return ticketType;
    }

    public void EditTicketType(
        TicketTypeId ticketTypeId,
        TicketName name,
        Money price,
        Capacity capacity,
        DateTimeOffset updatedAt)
    {
        if (Status is not EventStatus.Draft)
        {
            throw new BusinessRuleValidationException(
                "INVALID_EVENT_STATUS",
                Status switch
                {
                    EventStatus.Published => "Cannot edit ticket types on a published event.",
                    EventStatus.Closed => "Cannot edit ticket types on a closed event.",
                    EventStatus.Cancelled => "Cannot edit ticket types on a cancelled event.",
                    _ => "Cannot edit ticket types on an event in its current status.",
                });
        }

        var ticketType = _ticketTypes.FirstOrDefault(t => t.Id == ticketTypeId)
            ?? throw new BusinessRuleValidationException(
                "TICKET_TYPE_NOT_FOUND",
                "The ticket type was not found on this event.");

        // EC-01: duplicate name check (exclude current type)
        if (_ticketTypes.Any(t => t.Id != ticketTypeId && t.Name.Value == name.Value))
        {
            throw new BusinessRuleValidationException(
                "TICKET_TYPE_NAME_DUPLICATE",
                $"A ticket type with the name '{name.Value}' already exists on this event.");
        }

        // INV-12: capacity cannot drop below Reserved + Sold
        if (capacity.Value < ticketType.Reserved + ticketType.Sold)
        {
            throw new BusinessRuleValidationException(
                "CAPACITY_REDUCTION_INVALID",
                $"Cannot reduce capacity below {ticketType.Reserved + ticketType.Sold} (reserved + sold).");
        }

        ticketType.Update(name, price, capacity, updatedAt);

        UpdatedAt = updatedAt;

        Raise(new TicketTypeUpdatedEvent(Id, ticketTypeId));
    }

    public void RemoveTicketType(TicketTypeId ticketTypeId, DateTimeOffset updatedAt)
    {
        if (Status is EventStatus.Closed or EventStatus.Cancelled)
        {
            throw new BusinessRuleValidationException(
                "EVENT_CLOSED_OR_CANCELLED",
                "Cannot remove ticket types from a closed or cancelled event.");
        }

        var ticketType = _ticketTypes.FirstOrDefault(t => t.Id == ticketTypeId)
            ?? throw new BusinessRuleValidationException(
                "TICKET_TYPE_NOT_FOUND",
                "The ticket type was not found on this event.");

        // EC-02: cannot remove if Reserved + Sold > 0
        if (ticketType.Reserved + ticketType.Sold > 0)
        {
            throw new BusinessRuleValidationException(
                "TICKET_TYPE_HAS_SALES",
                "Cannot remove a ticket type that has reserved or sold tickets.");
        }

        // EC-03 / INV-11: cannot remove last ticket type from a Published event
        if (Status is EventStatus.Published && _ticketTypes.Count <= 1)
        {
            throw new BusinessRuleValidationException(
                "TICKET_TYPE_LAST_ON_PUBLISHED_EVENT",
                "Cannot remove the last ticket type from a published event. Unpublish or cancel the event first.");
        }

        _ticketTypes.Remove(ticketType);

        UpdatedAt = updatedAt;

        Raise(new TicketTypeRemovedEvent(Id, ticketTypeId));
    }

    public void LoadTicketTypes(List<TicketType> ticketTypes)
    {
        _ticketTypes.Clear();
        _ticketTypes.AddRange(ticketTypes);
    }

    public Reservation Reserve(
        TicketTypeId ticketTypeId,
        int quantity,
        OrderId orderId,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        // INV-14: event must be published
        if (Status is not EventStatus.Published)
        {
            throw new BusinessRuleValidationException(
                "EVENT_NOT_PUBLISHED",
                "Cannot reserve tickets for an event that is not published.");
        }

        var ticketType = _ticketTypes.FirstOrDefault(t => t.Id == ticketTypeId)
            ?? throw new BusinessRuleValidationException(
                "TICKET_TYPE_NOT_FOUND",
                "The ticket type was not found on this event.");

        ticketType.Reserve(quantity);

        var nextId = _reservations.Count > 0
            ? ReservationId.From(_reservations.Max(r => r.Id.Value) + 1)
            : ReservationId.From(1);

        var reservation = Reservation.Create(
            nextId,
            ticketTypeId,
            quantity,
            orderId,
            expiresAt,
            now);

        _reservations.Add(reservation);

        Raise(new InventoryReservedEvent(Id, ticketTypeId, nextId, quantity, now));

        if (ticketType.Available == 0)
        {
            Raise(new EventSoldOutEvent(Id, ticketTypeId, now));
        }

        return reservation;
    }

    public void CommitReservation(ReservationId reservationId, DateTimeOffset now)
    {
        var reservation = _reservations.FirstOrDefault(r => r.Id == reservationId)
            ?? throw new BusinessRuleValidationException(
                "RESERVATION_NOT_FOUND",
                "The reservation was not found.");

        var ticketType = _ticketTypes.FirstOrDefault(t => t.Id == reservation.TicketTypeId)
            ?? throw new BusinessRuleValidationException(
                "TICKET_TYPE_NOT_FOUND",
                "The ticket type was not found on this event.");

        ticketType.CommitReservation(reservation.Quantity);

        _reservations.Remove(reservation);

        Raise(new ReservationCommittedEvent(Id, reservation.TicketTypeId, reservationId, reservation.Quantity, now));

        if (ticketType.Available == 0)
        {
            Raise(new EventSoldOutEvent(Id, reservation.TicketTypeId, now));
        }
    }

    public void ReleaseReservation(ReservationId reservationId, DateTimeOffset now)
    {
        var reservation = _reservations.FirstOrDefault(r => r.Id == reservationId)
            ?? throw new BusinessRuleValidationException(
                "RESERVATION_NOT_FOUND",
                "The reservation was not found.");

        var ticketType = _ticketTypes.FirstOrDefault(t => t.Id == reservation.TicketTypeId)
            ?? throw new BusinessRuleValidationException(
                "TICKET_TYPE_NOT_FOUND",
                "The ticket type was not found on this event.");

        ticketType.ReleaseReservation(reservation.Quantity);

        _reservations.Remove(reservation);

        Raise(new ReservationReleasedEvent(Id, reservation.TicketTypeId, reservationId, reservation.Quantity, now));
    }

    public void ReturnToPool(TicketTypeId ticketTypeId, int quantity, DateTimeOffset now)
    {
        var ticketType = _ticketTypes.FirstOrDefault(t => t.Id == ticketTypeId)
            ?? throw new BusinessRuleValidationException(
                "TICKET_TYPE_NOT_FOUND",
                "The ticket type was not found on this event.");

        ticketType.ReturnToPool(quantity);

        Raise(new InventoryReturnedToPoolEvent(Id, ticketTypeId, quantity, now));
    }

    public void LoadReservations(List<Reservation> reservations)
    {
        _reservations.Clear();
        _reservations.AddRange(reservations);
    }

    public static Event FromPersistence(
        EventId id,
        UserId organizerId,
        EventTitle title,
        EventSchedule? schedule,
        EventLocation location,
        string? description,
        EventStatus status,
        Slug? slug,
        CoverImageRef? coverImageRef,
        DateTimeOffset? cancelledAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        long rowVersion) =>
        new()
        {
            Id = id,
            OrganizerId = organizerId,
            Title = title,
            Schedule = schedule,
            Location = location,
            Description = description,
            Status = status,
            Slug = slug,
            CoverImageRef = coverImageRef,
            CancelledAt = cancelledAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            RowVersion = rowVersion,
        };
}
