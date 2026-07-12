import { useQuery } from '@tanstack/react-query'
import { BarChart3, CalendarDays, Pencil } from 'lucide-react'
import { Link } from 'react-router-dom'

import { paths } from '@/app/paths'
import { Badge } from '@/components/ui/badge'
import { buttonVariants } from '@/components/ui/button-variants'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { getOrganizerAudienceOverview } from '@/features/reporting/api'
import { formatPrice } from '@/lib/utils/format-price'

function numeric(value: string | number) {
  return typeof value === 'number' ? value : Number(value)
}

export function EventsPlaceholderPage() {
  const eventsQuery = useQuery({
    queryKey: ['organizer-audience-overview'],
    queryFn: ({ signal }) => getOrganizerAudienceOverview(signal),
  })

  const ownedEvents = eventsQuery.data?.ownedEvents ?? []
  const staffEvents = eventsQuery.data?.staffEvents ?? []
  const hasEvents = ownedEvents.length > 0 || staffEvents.length > 0

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
        <div className="flex flex-col gap-2">
          <p className="text-primary text-sm font-medium">Organizer tools</p>
          <h1 className="text-2xl font-bold tracking-tight">Events</h1>
          <p className="text-muted-foreground max-w-2xl text-sm">
            Track sales, attendance, and audience data for the events you run.
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

      {!eventsQuery.isPending && !eventsQuery.isError && !hasEvents && (
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

      {ownedEvents.length > 0 && (
        <div className="flex flex-col gap-3">
          <h2 className="text-base font-semibold">Owned events</h2>
          {ownedEvents.map((event) => (
            <Card key={event.eventId} className="shadow-sm">
              <CardContent className="grid gap-4 py-4 md:grid-cols-[1fr_auto] md:items-center">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="truncate text-base font-semibold">{event.title}</h3>
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
                  </p>
                  <p className="text-muted-foreground mt-1 text-xs">
                    {event.soldCount} sold .{' '}
                    {formatPrice(numeric(event.totalRevenueAmount), event.totalRevenueCurrency)} .{' '}
                    {event.checkedInCount}/{event.issuedCount} checked in
                  </p>
                </div>

                <div className="flex flex-wrap gap-2">
                  <Link
                    to={paths.organizerEventResults.replace(':eventId', String(event.eventId))}
                    aria-label={`Open results for ${event.title}`}
                    className={buttonVariants({
                      variant: 'default',
                      size: 'sm',
                      className: 'w-fit',
                    })}
                  >
                    <BarChart3 className="mr-2 size-4" aria-hidden />
                    Results
                  </Link>
                  <Link
                    to={paths.editEvent.replace(':eventId', String(event.eventId))}
                    aria-label={`Edit ${event.title}`}
                    className={buttonVariants({
                      variant: 'outline',
                      size: 'sm',
                      className: 'w-fit',
                    })}
                  >
                    <Pencil className="mr-2 size-4" aria-hidden />
                    Edit
                  </Link>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {staffEvents.length > 0 && (
        <div className="flex flex-col gap-3">
          <h2 className="text-base font-semibold">Staff events</h2>
          {staffEvents.map((event) => (
            <Card key={event.eventId} className="shadow-sm">
              <CardContent className="grid gap-4 py-4 md:grid-cols-[1fr_auto] md:items-center">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <h3 className="truncate text-base font-semibold">{event.title}</h3>
                    <Badge variant="secondary">{event.status}</Badge>
                  </div>
                  <p className="text-muted-foreground mt-1 text-sm">
                    {event.startsAt
                      ? new Date(event.startsAt).toLocaleString(undefined, {
                          dateStyle: 'medium',
                          timeStyle: 'short',
                        })
                      : 'No schedule'}
                  </p>
                  <p className="text-muted-foreground mt-1 text-xs">
                    {event.checkedInCount}/{event.issuedCount} checked in
                  </p>
                </div>

                <Link
                  to={paths.organizerEventResults.replace(':eventId', String(event.eventId))}
                  aria-label={`Open results for ${event.title}`}
                  className={buttonVariants({ variant: 'outline', size: 'sm', className: 'w-fit' })}
                >
                  <BarChart3 className="mr-2 size-4" aria-hidden />
                  Results
                </Link>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
