import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { formatPrice } from '@/lib/utils/format-price'

export interface OrderLineItem {
  ticketTypeName: string
  unitPrice: number
  currency: string
  quantity: number
}

interface OrderSummaryProps {
  lineItems: OrderLineItem[]
}

export function OrderSummary({ lineItems }: OrderSummaryProps) {
  const total = lineItems.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0)
  const currency = lineItems[0]?.currency ?? 'VND'

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <CardTitle className="text-lg">Order Summary</CardTitle>
          <Badge variant="secondary" className="text-xs">
            All-inclusive — no hidden fees
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <div className="text-muted-foreground grid grid-cols-[1fr_auto_auto] gap-x-4 text-sm">
          <span className="font-medium">Item</span>
          <span className="font-medium">Qty</span>
          <span className="text-right font-medium">Price</span>
        </div>
        <Separator />
        {lineItems.map((item) => (
          <div key={item.ticketTypeName} className="grid grid-cols-[1fr_auto_auto] gap-x-4 text-sm">
            <span>{item.ticketTypeName}</span>
            <span className="text-muted-foreground">{item.quantity}</span>
            <span className="text-right font-medium">
              {formatPrice(item.unitPrice * item.quantity, item.currency)}
            </span>
          </div>
        ))}
        <Separator />
        <div className="flex items-center justify-between">
          <span className="text-base font-semibold">Total</span>
          <span className="text-lg font-bold">{formatPrice(total, currency)}</span>
        </div>
        <p className="text-muted-foreground text-xs">
          All-inclusive pricing — the price you see is the price you pay. No platform fees, no
          service fees, no hidden charges. Price includes applicable taxes.
        </p>
      </CardContent>
    </Card>
  )
}
