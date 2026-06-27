import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

import * as eventsApi from '../api'
import { TicketTypeList } from '../components/ticket-type-list'

export function PublicEventPage() {
  const { slug } = useParams<{ slug: string }>()

  const eventQuery = useQuery({
    queryKey: ['public-event', slug],
    queryFn: ({ signal }) => eventsApi.getPublicEventBySlug(slug!, signal),
    enabled: !!slug,
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
          <AlertDescription>Event not found.</AlertDescription>
        </Alert>
      </div>
    )
  }

  const event = eventQuery.data

  return (
    <div className="mx-auto max-w-2xl px-4 py-8">
      {event.coverImageUrl && (
        <img
          src={event.coverImageUrl}
          alt={event.title}
          className="mb-6 w-full rounded-lg object-cover"
        />
      )}

      <Card>
        <CardHeader>
          <div className="flex items-start justify-between gap-2">
            <CardTitle className="text-2xl">{event.title}</CardTitle>
            {event.status === 'Cancelled' && <Badge variant="destructive">Cancelled</Badge>}
            {event.status === 'Closed' && <Badge variant="secondary">Sales closed</Badge>}
          </div>
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

          {event.status === 'Cancelled' ? (
            <Alert>
              <AlertDescription>This event has been cancelled.</AlertDescription>
            </Alert>
          ) : event.status === 'Closed' ? (
            <Alert>
              <AlertDescription>Sales for this event are closed.</AlertDescription>
            </Alert>
          ) : (
            <TicketTypeList ticketTypes={event.ticketTypes} purchasable={event.purchasable} />
          )}
        </CardContent>
      </Card>
    </div>
  )
}
