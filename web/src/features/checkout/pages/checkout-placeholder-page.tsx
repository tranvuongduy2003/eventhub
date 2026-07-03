import { useMemo } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router-dom'
import { ShoppingCart } from 'lucide-react'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ApiError } from '@/types/api-problem'

import { startCheckout } from '../api'
import { OrderSummary, type OrderLineItem } from '../components/order-summary'
import { decodeTicketSelection, readStoredSelection, selectionToLines } from '../selection-storage'

export function CheckoutPlaceholderPage() {
  const [searchParams] = useSearchParams()
  const eventSlug = searchParams.get('event') ?? ''

  const selection = useMemo(() => {
    const fromUrl = decodeTicketSelection(searchParams.get('tickets'))
    return Object.keys(fromUrl).length > 0 || !eventSlug ? fromUrl : readStoredSelection(eventSlug)
  }, [eventSlug, searchParams])

  const lines = useMemo(() => selectionToLines(selection), [selection])

  const checkoutQuery = useQuery({
    queryKey: ['checkout-start', eventSlug, searchParams.get('tickets')],
    queryFn: ({ signal }) => startCheckout(eventSlug, { lines }, signal),
    enabled: !!eventSlug && lines.length > 0,
    retry: false,
  })

  if (!eventSlug || lines.length === 0) {
    return (
      <div className="flex flex-col gap-4">
        <Alert>
          <AlertDescription>
            Select tickets from an event before continuing checkout.
          </AlertDescription>
        </Alert>
        <Link
          className="text-primary text-sm font-medium underline underline-offset-4"
          to="/events"
        >
          Browse events
        </Link>
      </div>
    )
  }

  if (checkoutQuery.isPending) {
    return (
      <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
        <Skeleton className="h-52 w-full" />
        <Skeleton className="h-72 w-full" />
      </div>
    )
  }

  if (checkoutQuery.isError) {
    const message =
      checkoutQuery.error instanceof ApiError
        ? (checkoutQuery.error.problem.detail ?? checkoutQuery.error.message)
        : 'Ticket selection could not be validated.'

    return (
      <div className="flex flex-col gap-4">
        <Alert variant="destructive">
          <AlertDescription>{message}</AlertDescription>
        </Alert>
        <Link
          className="text-primary text-sm font-medium underline underline-offset-4"
          to={`/events/${eventSlug}?tickets=${searchParams.get('tickets') ?? ''}`}
        >
          Adjust tickets
        </Link>
      </div>
    )
  }

  const checkout = checkoutQuery.data
  const lineItems: OrderLineItem[] = checkout.lines.map((line) => ({
    ticketTypeName: line.ticketTypeName,
    unitPrice: line.unitPriceAmount,
    currency: line.unitPriceCurrency,
    quantity: line.quantity,
  }))

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <p className="text-primary text-sm font-medium">Checkout</p>
        <h1 className="text-2xl font-bold tracking-tight">{checkout.eventTitle}</h1>
        <p className="text-muted-foreground max-w-2xl text-sm">
          Your ticket selection has been checked against current availability.
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
        <Card className="shadow-sm">
          <CardHeader>
            <div className="bg-primary/10 text-primary mb-2 flex size-10 items-center justify-center rounded-lg">
              <ShoppingCart className="size-5" aria-hidden />
            </div>
            <CardTitle>Guest details come next</CardTitle>
            <CardDescription>
              This step keeps your validated ticket selection ready for the next checkout slice.
            </CardDescription>
          </CardHeader>
          <CardContent className="text-muted-foreground flex flex-col gap-3 text-sm">
            <p>No order, reservation, payment, or hold has been created yet.</p>
            <Link
              className="text-primary font-medium underline underline-offset-4"
              to={`/events/${checkout.eventSlug}?tickets=${searchParams.get('tickets') ?? ''}`}
            >
              Edit ticket selection
            </Link>
          </CardContent>
        </Card>

        <OrderSummary lineItems={lineItems} />
      </div>
    </div>
  )
}
