import { useQuery } from '@tanstack/react-query'
import { CalendarDays, Pencil } from 'lucide-react'
import { Link } from 'react-router-dom'

import { paths } from '@/app/paths'
import { Badge } from '@/components/ui/badge'
import { buttonVariants } from '@/components/ui/button-variants'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

import { getOrganizerEvents } from '../api'

export function EventsPlaceholderPage() {
  const eventsQuery = useQuery({
    queryKey: ['organizer-events'],
    queryFn: ({ signal }) => getOrganizerEvents(signal),
  })

  const events = eventsQuery.data?.items ?? []

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
        <div className="flex flex-col gap-2">
          <p className="text-primary text-sm font-medium">Organizer tools</p>
          <h1 className="text-2xl font-bold tracking-tight">Events</h1>
          <p className="text-muted-foreground max-w-2xl text-sm">
            Create event listings, set ticket types, and publish when you are ready to sell.
          </p>
        </div>

        <Link
          to={paths.createEvent}
          className={buttonVariants({ variant: 'default', className: 'w-fit' })}
        >
          Create event
        </Link>
      </div>

      {eventsQuery.isPending && (
        <div className="grid gap-3">
          <Skeleton className="h-24 w-full" />
          <Skeleton className="h-24 w-full" />
        </div>
      )}

      {eventsQuery.isError && (
        <p className="text-destructive text-sm">Events could not be loaded. Please try again.</p>
      )}

      {!eventsQuery.isPending && !eventsQuery.isError && events.length === 0 && (
        <Card className="shadow-sm">
          <CardContent className="flex flex-col gap-3 py-6">
            <div className="bg-primary/10 text-primary flex size-10 items-center justify-center rounded-lg">
              <CalendarDays className="size-5" aria-hidden />
            </div>
            <div>
              <h2 className="text-base font-semibold">No events yet</h2>
              <p className="text-muted-foreground mt-1 text-sm">
                Create your first draft, add ticket types, and publish it when sales can begin.
              </p>
            </div>
          </CardContent>
        </Card>
      )}

      {events.length > 0 && (
        <div className="grid gap-3">
          {events.map((event) => (
            <Card key={event.eventId} className="shadow-sm">
              <CardContent className="grid gap-4 py-4 md:grid-cols-[1fr_auto] md:items-center">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <h2 className="truncate text-base font-semibold">{event.title}</h2>
                    <Badge variant={event.status === 'Published' ? 'default' : 'secondary'}>
                      {event.status}
                    </Badge>
                  </div>
                  <p className="text-muted-foreground mt-1 text-sm">
                    {event.startsAt
                      ? new Date(event.startsAt).toLocaleString(undefined, {
                          dateStyle: 'medium',
                          timeStyle: 'short',
                        })
                      : 'No schedule'}
                    {' · '}
                    {event.isOnline ? 'Online' : (event.physicalAddress ?? 'No location')}
                  </p>
                  <p className="text-muted-foreground mt-1 text-xs">
                    {event.ticketTypeCount} ticket types · {event.soldCount} sold
                  </p>
                </div>

                <Link
                  to={paths.editEvent.replace(':eventId', String(event.eventId))}
                  aria-label={`Edit ${event.title}`}
                  className={buttonVariants({ variant: 'outline', size: 'sm', className: 'w-fit' })}
                >
                  <Pencil className="mr-2 size-4" aria-hidden />
                  Edit
                </Link>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
