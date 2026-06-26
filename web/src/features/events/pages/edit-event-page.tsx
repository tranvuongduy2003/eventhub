import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'

import * as eventsApi from '../api'
import { DiscountCodeManager } from '../components/discount-code-manager'
import { TicketTypeManager } from '../components/ticket-type-manager'
import { EditEventForm } from '../edit-event-form'

export function EditEventPage() {
  const { eventId } = useParams<{ eventId: string }>()

  const eventIdNum = eventId ? Number(eventId) : NaN

  const eventQuery = useQuery({
    queryKey: ['event', eventIdNum],
    queryFn: ({ signal }) => eventsApi.getEventDetails(eventIdNum, signal),
    enabled: !isNaN(eventIdNum),
  })

  if (isNaN(eventIdNum)) {
    return (
      <div className="flex flex-col gap-6">
        <Alert variant="destructive">
          <AlertDescription>Invalid event ID.</AlertDescription>
        </Alert>
      </div>
    )
  }

  if (eventQuery.isPending) {
    return (
      <div className="flex flex-col gap-6">
        <div className="flex flex-col gap-2">
          <Skeleton className="h-4 w-24" />
          <Skeleton className="h-8 w-48" />
          <Skeleton className="h-4 w-96" />
        </div>
        <div className="max-w-xl space-y-4">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
        </div>
      </div>
    )
  }

  if (eventQuery.isError) {
    return (
      <div className="flex flex-col gap-6">
        <Alert variant="destructive">
          <AlertDescription>Failed to load event details. Please try again.</AlertDescription>
        </Alert>
      </div>
    )
  }

  const event = eventQuery.data

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <p className="text-primary text-sm font-medium">Organizer tools</p>
        <h1 className="text-2xl font-bold tracking-tight">Edit event</h1>
        <p className="text-muted-foreground max-w-2xl text-sm">
          Update your event details.{' '}
          {event.status === 'Published'
            ? 'Changes will be visible to attendees.'
            : 'You can add ticket types and publish later.'}
        </p>
      </div>

      <div className="max-w-xl">
        <EditEventForm event={event} />
      </div>

      <Separator className="max-w-xl" />

      <div className="max-w-xl">
        <TicketTypeManager eventId={event.eventId} eventStatus={event.status} />
      </div>

      <Separator className="max-w-xl" />

      <div className="max-w-xl">
        <DiscountCodeManager eventId={event.eventId} />
      </div>
    </div>
  )
}
