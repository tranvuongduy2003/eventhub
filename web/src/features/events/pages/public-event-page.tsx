import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'

import { paths } from '@/app/paths'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import * as checkoutApi from '@/features/checkout/api'
import {
  decodeTicketSelection,
  encodeTicketSelection,
  readStoredSelection,
  selectionToLines,
  writeCheckoutSnapshot,
  writeStoredSelection,
  type TicketSelection,
} from '@/features/checkout/selection-storage'
import { ApiError } from '@/types/api-problem'

import * as eventsApi from '../api'
import { CoverImageDisplay } from '../cover-image-display'
import { CollapsibleDescription } from '../components/collapsible-description'
import { EventMetaTags } from '../components/event-meta-tags'
import { StickyCtaBar } from '../components/sticky-cta-bar'
import { TicketTypeList } from '../components/ticket-type-list'

export function PublicEventPage() {
  const { slug } = useParams<{ slug: string }>()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const ctaSentinelRef = useRef<HTMLDivElement>(null)
  const [isCtaVisible, setIsCtaVisible] = useState(true)
  const [quantities, setQuantities] = useState<TicketSelection>(() => {
    const fromUrl = decodeTicketSelection(searchParams.get('tickets'))
    return Object.keys(fromUrl).length > 0 || !slug ? fromUrl : readStoredSelection(slug)
  })
  const [lineErrors, setLineErrors] = useState<Record<number, string>>({})
  const [checkoutError, setCheckoutError] = useState<string | null>(null)

  const eventQuery = useQuery({
    queryKey: ['public-event', slug],
    queryFn: ({ signal }) => eventsApi.getPublicEventBySlug(slug!, signal),
    enabled: !!slug,
  })

  const checkoutMutation = useMutation({
    mutationFn: (selection: TicketSelection) =>
      checkoutApi.startCheckout(slug!, { lines: selectionToLines(selection) }),
    onSuccess: (checkout, selection) => {
      writeStoredSelection(checkout.eventSlug, selection)
      writeCheckoutSnapshot(checkout.eventSlug, checkout)

      const query = new URLSearchParams({
        event: checkout.eventSlug,
        tickets: encodeTicketSelection(selection),
      })

      navigate(`${paths.checkout}?${query.toString()}`)
    },
    onError: (error) => {
      if (error instanceof ApiError) {
        setCheckoutError(error.problem.detail ?? error.problem.title ?? error.message)
        return
      }

      setCheckoutError('Tickets could not be validated. Please try again.')
    },
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

  function updateQuantity(ticketTypeId: number, quantity: number) {
    if (!slug) {
      return
    }

    const nextSelection = {
      ...quantities,
      [ticketTypeId]: Math.max(0, quantity),
    }

    if (nextSelection[ticketTypeId] === 0) {
      delete nextSelection[ticketTypeId]
    }

    setQuantities(nextSelection)
    setCheckoutError(null)
    setLineErrors((current) => {
      const nextErrors = { ...current }
      delete nextErrors[ticketTypeId]
      return nextErrors
    })
    writeStoredSelection(slug, nextSelection)

    const nextSearchParams = new URLSearchParams(searchParams)
    const encodedSelection = encodeTicketSelection(nextSelection)
    if (encodedSelection) {
      nextSearchParams.set('tickets', encodedSelection)
    } else {
      nextSearchParams.delete('tickets')
    }

    setSearchParams(nextSearchParams, { replace: true })
  }

  function startCheckout() {
    const selectedLines = selectionToLines(quantities)
    if (selectedLines.length === 0) {
      setCheckoutError('Select at least one ticket.')
      return
    }

    const nextLineErrors: Record<number, string> = {}
    for (const line of selectedLines) {
      const ticketType = eventQuery.data?.ticketTypes.find(
        (item) => item.ticketTypeId === line.ticketTypeId,
      )

      if (!ticketType?.isPurchasable) {
        nextLineErrors[line.ticketTypeId] =
          ticketType?.availabilityReason ?? 'This ticket type is not available.'
      } else if (ticketType.maxPerOrder != null && line.quantity > ticketType.maxPerOrder) {
        nextLineErrors[line.ticketTypeId] = `Maximum ${ticketType.maxPerOrder} per order.`
      }
    }

    if (Object.keys(nextLineErrors).length > 0) {
      setLineErrors(nextLineErrors)
      setCheckoutError('Please adjust your ticket selection.')
      return
    }

    setLineErrors({})
    setCheckoutError(null)
    checkoutMutation.mutate(quantities)
  }

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
  const selectedCount = Object.values(quantities).reduce((total, quantity) => total + quantity, 0)
  const checkoutDisabled = selectedCount === 0

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
              <h1 className="text-xl leading-snug font-medium md:text-2xl">{event.title}</h1>
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
                {checkoutError && (
                  <Alert variant="destructive">
                    <AlertDescription>{checkoutError}</AlertDescription>
                  </Alert>
                )}
                <div ref={ctaSentinelRef}>
                  <TicketTypeList
                    ticketTypes={event.ticketTypes}
                    purchasable={event.purchasable}
                    quantities={quantities}
                    lineErrors={lineErrors}
                    onQuantityChange={updateQuantity}
                    onStartCheckout={startCheckout}
                    checkoutDisabled={checkoutDisabled}
                    checkoutPending={checkoutMutation.isPending}
                  />
                </div>
              </>
            )}
          </CardContent>
        </Card>
      </div>

      {event.purchasable && (
        <StickyCtaBar
          eventTitle={event.title}
          visible={!isCtaVisible}
          selectedCount={selectedCount}
          disabled={checkoutDisabled}
          pending={checkoutMutation.isPending}
          onStartCheckout={startCheckout}
        />
      )}
    </>
  )
}
