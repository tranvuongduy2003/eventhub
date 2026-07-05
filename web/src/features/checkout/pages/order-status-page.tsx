import { useQuery } from '@tanstack/react-query'
import { AlertCircle, Clock3, ReceiptText, TicketCheck } from 'lucide-react'
import { Link, useParams } from 'react-router-dom'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ApiError } from '@/types/api-problem'

import { getOrderStatus } from '../api'
import { OrderSummary, type OrderLineItem } from '../components/order-summary'

function formatDate(value: string | null) {
  if (!value) {
    return null
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function statusLabel(status: string) {
  return status.charAt(0).toUpperCase() + status.slice(1)
}

function statusVariant(status: string): 'default' | 'secondary' | 'destructive' {
  if (status === 'confirmed') {
    return 'default'
  }

  if (status === 'expired' || status === 'cancelled') {
    return 'destructive'
  }

  return 'secondary'
}

export function OrderStatusPage() {
  const { orderId } = useParams()
  const numericOrderId = Number(orderId)
  const isValidOrderId = Number.isInteger(numericOrderId) && numericOrderId > 0

  const orderQuery = useQuery({
    queryKey: ['order-status', numericOrderId],
    queryFn: ({ signal }) => getOrderStatus(numericOrderId, signal),
    enabled: isValidOrderId,
    retry: false,
  })

  if (!isValidOrderId) {
    return <OrderNotFound message="That order reference is not valid." />
  }

  if (orderQuery.isPending) {
    return (
      <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
        <Skeleton className="h-64 w-full" />
        <Skeleton className="h-72 w-full" />
      </div>
    )
  }

  if (orderQuery.isError) {
    const message =
      orderQuery.error instanceof ApiError && orderQuery.error.problem.status === 404
        ? 'We could not find an order for that reference.'
        : 'Order status could not be loaded. Please try again.'

    return <OrderNotFound message={message} />
  }

  const order = orderQuery.data
  const lineItems: OrderLineItem[] = order.lines.map((line) => ({
    ticketTypeName: line.ticketTypeName,
    unitPrice: line.unitPriceAmount,
    currency: line.unitPriceCurrency,
    quantity: line.quantity,
    lineTotal: line.lineTotalAmount,
  }))

  const placedAt = formatDate(order.placedAt)
  const confirmedAt = formatDate(order.confirmedAt)

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <p className="text-primary text-sm font-medium">Order status</p>
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-bold tracking-tight">Order #{order.orderId}</h1>
          <Badge variant={statusVariant(order.status)}>{statusLabel(order.status)}</Badge>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
        <div className="flex flex-col gap-4">
          <Card className="shadow-sm">
            <CardHeader>
              <div className="bg-primary/10 text-primary mb-2 flex size-10 items-center justify-center rounded-lg">
                <ReceiptText className="size-5" aria-hidden />
              </div>
              <CardTitle>Reference details</CardTitle>
              <CardDescription>No account is required to view this order.</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-3 text-sm sm:grid-cols-2">
              <Detail label="Placed" value={placedAt ?? 'Unavailable'} />
              <Detail label="Confirmed" value={confirmedAt ?? 'Not confirmed yet'} />
              <Detail
                label="Payment"
                value={order.paymentId ? `#${order.paymentId}` : 'Not started'}
              />
              <Detail
                label="Items"
                value={`${order.lines.reduce((sum, line) => sum + line.quantity, 0)}`}
              />
            </CardContent>
          </Card>

          <Card className="shadow-sm">
            <CardHeader>
              <div className="mb-2 flex size-10 items-center justify-center rounded-lg bg-emerald-100 text-emerald-700">
                {order.status === 'confirmed' ? (
                  <TicketCheck className="size-5" aria-hidden />
                ) : (
                  <Clock3 className="size-5" aria-hidden />
                )}
              </div>
              <CardTitle>Tickets</CardTitle>
              <CardDescription>
                {order.status === 'confirmed'
                  ? 'Ticket display will appear here after ticket issuance is enabled.'
                  : 'Tickets appear here after the order is confirmed.'}
              </CardDescription>
            </CardHeader>
          </Card>
        </div>

        <OrderSummary
          lineItems={lineItems}
          totalAmount={order.totalAmount}
          totalCurrency={order.totalCurrency}
          discount={
            order.discountCode && order.discountAmount
              ? { code: order.discountCode, amount: order.discountAmount }
              : null
          }
        />
      </div>
    </div>
  )
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border p-3">
      <dt className="text-muted-foreground text-xs font-medium">{label}</dt>
      <dd className="mt-1 text-sm font-medium">{value}</dd>
    </div>
  )
}

function OrderNotFound({ message }: { message: string }) {
  return (
    <div className="flex max-w-xl flex-col gap-4">
      <Alert variant="destructive">
        <AlertCircle className="size-4" aria-hidden />
        <AlertDescription>{message}</AlertDescription>
      </Alert>
      <Link className="text-primary text-sm font-medium underline underline-offset-4" to="/events">
        Browse events
      </Link>
    </div>
  )
}
