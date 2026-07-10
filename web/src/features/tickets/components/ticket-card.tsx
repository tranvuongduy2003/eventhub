import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

import type { TicketResponse } from '../api'

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function qrImageUrl(code: string) {
  const params = new URLSearchParams({
    size: '320x320',
    margin: '12',
    data: code,
  })

  return `https://api.qrserver.com/v1/create-qr-code/?${params.toString()}`
}

export function TicketCard({ ticket }: { ticket: TicketResponse }) {
  const location = ticket.eventIsOnline
    ? 'Online'
    : ticket.eventLocation || 'Location to be announced'

  return (
    <Card className="overflow-hidden shadow-sm">
      <CardHeader className="gap-3 pb-3">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0">
            <CardTitle className="text-lg">{ticket.ticketTypeName}</CardTitle>
            <p className="text-muted-foreground mt-1 text-sm">{ticket.eventTitle}</p>
          </div>
          <Badge variant={ticket.status === 'valid' ? 'default' : 'secondary'}>
            {ticket.status}
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="grid gap-4 sm:grid-cols-[minmax(220px,280px)_1fr] sm:items-center">
        <div className="mx-auto flex w-full max-w-[280px] flex-col items-center gap-3">
          <img
            src={qrImageUrl(ticket.code)}
            alt={`QR code for ticket ${ticket.ticketId}`}
            className="aspect-square w-full rounded border bg-white p-2"
            loading="lazy"
          />
          <code className="bg-muted block w-full rounded px-2 py-1 text-center text-xs break-all">
            {ticket.code}
          </code>
        </div>

        <dl className="grid gap-3 text-sm">
          <Detail label="Holder" value={`${ticket.holderName} · ${ticket.holderEmail}`} />
          <Detail
            label="When"
            value={`${formatDate(ticket.eventStartsAt)} (${ticket.eventTimeZoneId})`}
          />
          <Detail label="Where" value={location} />
          <Detail label="Order" value={`#${ticket.orderId}`} />
        </dl>
      </CardContent>
    </Card>
  )
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-muted-foreground text-xs font-medium">{label}</dt>
      <dd className="mt-1 font-medium break-words">{value}</dd>
    </div>
  )
}
