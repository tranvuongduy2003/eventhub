import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { formatPrice } from '@/lib/utils/format-price'

import type { TicketTypeResponse } from '../api'

interface TicketTypeListProps {
  ticketTypes: TicketTypeResponse[]
  isPending: boolean
  isError: boolean
}

export function TicketTypeList({ ticketTypes, isPending, isError }: TicketTypeListProps) {
  if (isPending) {
    return (
      <div className="space-y-2">
        <Skeleton className="h-16 w-full" />
        <Skeleton className="h-16 w-full" />
      </div>
    )
  }

  if (isError) {
    return null
  }

  if (ticketTypes.length === 0) {
    return null
  }

  return (
    <div className="space-y-2">
      <h3 className="text-muted-foreground text-sm font-medium">Ticket types</h3>
      {ticketTypes.map((ticketType) => {
        const available = ticketType.capacity - ticketType.sold - ticketType.reserved
        const isSoldOut = available <= 0

        return (
          <Card key={ticketType.ticketTypeId}>
            <CardContent className="flex items-center justify-between py-3">
              <div className="flex flex-col gap-1">
                <span className="font-medium">{ticketType.name}</span>
                <span className="text-muted-foreground text-sm">
                  {isSoldOut ? (
                    <Badge variant="destructive">Sold out</Badge>
                  ) : (
                    <span>{available} remaining</span>
                  )}
                </span>
              </div>
              <span className="text-lg font-semibold">
                {formatPrice(ticketType.priceAmount, ticketType.priceCurrency)}
              </span>
            </CardContent>
          </Card>
        )
      })}
    </div>
  )
}
