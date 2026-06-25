import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

import * as eventsApi from '../api'
import { TicketTypeList } from '../components/ticket-type-list'

export function PublicEventPage() {
  const { eventId } = useParams<{ eventId: string }>()

  const eventQuery = useQuery({
    queryKey: ['public-event', eventId],
    queryFn: ({ signal }) => eventsApi.getPublicEventDetails(Number(eventId), signal),
    enabled: !!eventId,
  })

  if (eventQuery.isPending) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-8">
        <Skeleton className="mb-4 h-8 w-3/4" />
        <Skeleton className="mb-2 h-4 w-full" />
        <Skeleton className="mb-2 h-4 w-2/3" />
        <Skeleton className="h-10 w-32" />
      </div>
    )
  }

  if (eventQuery.isError) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-8">
        <Alert variant="destructive">
          <AlertDescription>Event not found or an error occurred.</AlertDescription>
        </Alert>
      </div>
    )
  }

  const event = eventQuery.data

  return (
    <div className="mx-auto max-w-2xl px-4 py-8">
      <Card>
        <CardHeader>
          <CardTitle className="text-2xl">{event.title}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {event.description && <p className="text-muted-foreground">{event.description}</p>}

          <div className="text-muted-foreground text-sm">
            {event.startsAt && (
              <p>
                {new Date(event.startsAt).toLocaleDateString('en-US', {
                  weekday: 'long',
                  year: 'numeric',
                  month: 'long',
                  day: 'numeric',
                })}
              </p>
            )}
            {event.startsAt && event.endsAt && (
              <p>
                {new Date(event.startsAt).toLocaleTimeString('en-US', {
                  hour: '2-digit',
                  minute: '2-digit',
                })}{' '}
                –{' '}
                {new Date(event.endsAt).toLocaleTimeString('en-US', {
                  hour: '2-digit',
                  minute: '2-digit',
                })}
              </p>
            )}
            {event.physicalAddress && <p>{event.physicalAddress}</p>}
            {event.isOnline && <p>Online event</p>}
          </div>

          <TicketTypeList ticketTypes={event.ticketTypes} />
        </CardContent>
      </Card>
    </div>
  )
}
