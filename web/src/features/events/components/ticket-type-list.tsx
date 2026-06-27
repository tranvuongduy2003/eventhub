import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { formatPrice } from '@/lib/utils/format-price'

import type { PublicTicketTypeResponse } from '../api'

interface TicketTypeListProps {
  ticketTypes: PublicTicketTypeResponse[]
}

export function TicketTypeList({ ticketTypes }: TicketTypeListProps) {
  if (ticketTypes.length === 0) {
    return null
  }

  return (
    <div className="space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-muted-foreground text-sm font-medium">Ticket types</h3>
        <Badge variant="secondary" className="text-xs">
          All-inclusive — no hidden fees
        </Badge>
      </div>
      <p className="text-muted-foreground text-xs">Price includes applicable taxes</p>
      {ticketTypes.map((ticketType) => (
        <Card key={ticketType.ticketTypeId}>
          <CardContent className="flex items-center justify-between py-3">
            <div className="flex flex-col gap-1">
              <span className="font-medium">{ticketType.name}</span>
              <span className="text-muted-foreground text-sm">
                {ticketType.salesWindowStatus === 'not_yet_on_sale' ? (
                  <span className="text-muted-foreground">
                    Not yet on sale
                    {ticketType.salesWindowStart && (
                      <> — sales begin {new Date(ticketType.salesWindowStart).toLocaleString()}</>
                    )}
                  </span>
                ) : ticketType.salesWindowStatus === 'sales_ended' ? (
                  <span className="text-muted-foreground">Sales ended</span>
                ) : ticketType.isSoldOut ? (
                  <Badge variant="destructive">Sold out</Badge>
                ) : (
                  <span>{ticketType.available} remaining</span>
                )}
                {ticketType.maxPerOrder != null && (
                  <span> · Max {ticketType.maxPerOrder} per order</span>
                )}
              </span>
            </div>
            <span className="text-lg font-semibold">
              {formatPrice(ticketType.priceAmount, ticketType.priceCurrency)}
            </span>
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
