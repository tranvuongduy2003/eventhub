import { useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

import * as eventsApi from '../api'
import { CoverImageDisplay } from '../cover-image-display'
import { CollapsibleDescription } from '../components/collapsible-description'
import { EventMetaTags } from '../components/event-meta-tags'
import { StickyCtaBar } from '../components/sticky-cta-bar'
import { TicketTypeList } from '../components/ticket-type-list'

export function PublicEventPage() {
  const { slug } = useParams<{ slug: string }>()
  const ctaSentinelRef = useRef<HTMLDivElement>(null)
  const [isCtaVisible, setIsCtaVisible] = useState(true)

  const eventQuery = useQuery({
    queryKey: ['public-event', slug],
    queryFn: ({ signal }) => eventsApi.getPublicEventBySlug(slug!, signal),
    enabled: !!slug,
  })

  useEffect(() => {
    const sentinel = ctaSentinelRef.current
    if (!sentinel) return

    const observer = new IntersectionObserver(([entry]) => setIsCtaVisible(entry.isIntersecting), {
      threshold: 0,
    })
    observer.observe(sentinel)
    return () => observer.disconnect()
  }, [eventQuery.data])

  if (eventQuery.isPending) {
    return (
      <div className="w-full px-4 py-8 md:mx-auto md:max-w-2xl">
        <Skeleton className="mb-6 aspect-video w-full rounded-lg" />
        <Skeleton className="mb-4 h-8 w-3/4" />
        <Skeleton className="mb-2 h-4 w-full" />
        <Skeleton className="mb-2 h-4 w-2/3" />
        <Skeleton className="h-10 w-32" />
      </div>
    )
  }

  if (eventQuery.isError) {
    return (
      <div className="w-full px-4 py-8 md:mx-auto md:max-w-2xl">
        <Alert variant="destructive">
          <AlertDescription>Event not found.</AlertDescription>
        </Alert>
      </div>
    )
  }

  const event = eventQuery.data

  return (
    <>
      <EventMetaTags
        event={{
          title: event.title,
          description: event.description,
          startsAt: event.startsAt,
          endsAt: event.endsAt,
          physicalAddress: event.physicalAddress,
          isOnline: event.isOnline,
          coverImageUrl: event.coverImageUrl,
          slug: event.slug,
        }}
      />
      <div className="w-full px-4 py-8 pb-24 md:mx-auto md:max-w-2xl md:pb-8">
        <CoverImageDisplay imageUrl={event.coverImageUrl} alt={event.title} className="mb-6" />

        <Card>
          <CardHeader>
            <div className="flex items-start justify-between gap-2">
              <CardTitle className="text-xl md:text-2xl">{event.title}</CardTitle>
              {event.status === 'Cancelled' && <Badge variant="destructive">Cancelled</Badge>}
              {event.status === 'Closed' && <Badge variant="secondary">Sales closed</Badge>}
            </div>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            {event.description && <CollapsibleDescription description={event.description} />}

            <div className="text-muted-foreground text-base md:text-sm">
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
              <>
                <div ref={ctaSentinelRef}>
                  <TicketTypeList ticketTypes={event.ticketTypes} purchasable={event.purchasable} />
                </div>
              </>
            )}
          </CardContent>
        </Card>
      </div>

      {event.purchasable && <StickyCtaBar eventTitle={event.title} visible={!isCtaVisible} />}
    </>
  )
}
