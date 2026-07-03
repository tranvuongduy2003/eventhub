import { Minus, Plus } from 'lucide-react'

import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { formatPrice } from '@/lib/utils/format-price'

import type { PublicTicketTypeResponse } from '../api'

interface TicketTypeListProps {
  ticketTypes: PublicTicketTypeResponse[]
  purchasable: boolean
  quantities: Record<number, number>
  lineErrors: Record<number, string>
  onQuantityChange: (ticketTypeId: number, quantity: number) => void
  onStartCheckout: () => void
  checkoutDisabled: boolean
  checkoutPending: boolean
}

export function TicketTypeList({
  ticketTypes,
  purchasable,
  quantities,
  lineErrors,
  onQuantityChange,
  onStartCheckout,
  checkoutDisabled,
  checkoutPending,
}: TicketTypeListProps) {
  if (ticketTypes.length === 0) {
    return <p className="text-muted-foreground text-sm">No tickets available.</p>
  }

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h3 className="text-muted-foreground text-sm font-medium">Ticket types</h3>
        <Badge variant="secondary" className="text-xs">
          All-inclusive, no hidden fees
        </Badge>
      </div>
      <p className="text-muted-foreground text-xs">Price includes applicable taxes</p>
      {ticketTypes.map((ticketType) => {
        const quantity = quantities[ticketType.ticketTypeId] ?? 0
        const lineError = lineErrors[ticketType.ticketTypeId]
        const maxQuantity = ticketType.maxPerOrder ?? 99
        const canIncrease = purchasable && ticketType.isPurchasable && quantity < maxQuantity

        return (
          <Card key={ticketType.ticketTypeId}>
            <CardContent className="grid gap-3 py-3 sm:grid-cols-[1fr_auto] sm:items-center">
              <div className="flex min-w-0 flex-col gap-1">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-medium">{ticketType.name}</span>
                  {!ticketType.isPurchasable && (
                    <Badge variant="secondary" className="text-xs">
                      {ticketType.availabilityState === 'sold_out' ? 'Sold out' : 'Unavailable'}
                    </Badge>
                  )}
                  {ticketType.availabilityState === 'limited' && (
                    <Badge variant="secondary" className="text-xs">
                      Limited
                    </Badge>
                  )}
                </div>
                <span className="text-lg font-semibold">
                  {formatPrice(ticketType.priceAmount, ticketType.priceCurrency)}
                </span>
                <span className="text-muted-foreground text-sm">
                  {ticketType.availabilityReason}
                  {ticketType.maxPerOrder != null && (
                    <span> · Max {ticketType.maxPerOrder} per order</span>
                  )}
                </span>
                {lineError && <span className="text-destructive text-sm">{lineError}</span>}
              </div>

              <div className="grid w-full grid-cols-[44px_1fr_44px] items-center rounded-md border sm:w-36">
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="h-11 rounded-none"
                  disabled={quantity === 0 || checkoutPending}
                  onClick={() =>
                    onQuantityChange(ticketType.ticketTypeId, Math.max(0, quantity - 1))
                  }
                  aria-label={`Decrease ${ticketType.name}`}
                >
                  <Minus className="size-4" aria-hidden />
                </Button>
                <span className="text-center text-sm font-semibold tabular-nums">{quantity}</span>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="h-11 rounded-none"
                  disabled={!canIncrease || checkoutPending}
                  onClick={() => onQuantityChange(ticketType.ticketTypeId, quantity + 1)}
                  aria-label={`Increase ${ticketType.name}`}
                  title={!ticketType.isPurchasable ? ticketType.availabilityReason : undefined}
                >
                  <Plus className="size-4" aria-hidden />
                </Button>
              </div>
            </CardContent>
          </Card>
        )
      })}

      {purchasable && (
        <Button
          className="h-11 w-full md:h-9"
          size="lg"
          disabled={checkoutDisabled || checkoutPending}
          onClick={onStartCheckout}
        >
          {checkoutPending ? 'Checking tickets...' : 'Continue to checkout'}
        </Button>
      )}
    </div>
  )
}
