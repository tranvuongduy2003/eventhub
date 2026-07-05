import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { formatPrice } from '@/lib/utils/format-price'

export interface OrderLineItem {
  ticketTypeName: string
  unitPrice: number
  currency: string
  quantity: number
  lineTotal: number
}

export interface DiscountInfo {
  code: string
  amount: number
}

interface OrderSummaryProps {
  lineItems: OrderLineItem[]
  discount?: DiscountInfo | null
  totalAmount?: number
  totalCurrency?: string
}

export function OrderSummary({
  lineItems,
  discount,
  totalAmount,
  totalCurrency,
}: OrderSummaryProps) {
  const subtotal = lineItems.reduce((sum, item) => sum + item.lineTotal, 0)
  const discountAmount = discount?.amount ?? 0
  const total = totalAmount ?? Math.max(0, subtotal - discountAmount)
  const currency = totalCurrency ?? lineItems[0]?.currency ?? 'VND'

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between gap-3">
          <CardTitle className="text-lg">Order Summary</CardTitle>
          <Badge variant="secondary" className="text-xs">
            All-inclusive
          </Badge>
        </div>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        <div className="text-muted-foreground grid grid-cols-[minmax(0,1fr)_auto_auto] gap-x-4 text-sm">
          <span className="font-medium">Item</span>
          <span className="font-medium">Qty</span>
          <span className="text-right font-medium">Line total</span>
        </div>
        <Separator />
        {lineItems.map((item) => (
          <div
            key={item.ticketTypeName}
            className="grid grid-cols-[minmax(0,1fr)_auto_auto] gap-x-4 text-sm"
          >
            <span className="min-w-0">
              <span className="block truncate">{item.ticketTypeName}</span>
              <span className="text-muted-foreground block text-xs">
                {formatPrice(item.unitPrice, item.currency)} each
              </span>
            </span>
            <span className="text-muted-foreground">{item.quantity}</span>
            <span className="text-right font-medium">
              {formatPrice(item.lineTotal, item.currency)}
            </span>
          </div>
        ))}
        <Separator />
        {discount && discountAmount > 0 && (
          <div className="flex items-center justify-between text-sm">
            <span className="text-muted-foreground">Discount ({discount.code})</span>
            <span className="font-medium text-green-600">
              -{formatPrice(discountAmount, currency)}
            </span>
          </div>
        )}
        <div className="flex items-center justify-between">
          <span className="text-base font-semibold">Total</span>
          <span className="text-lg font-bold">{formatPrice(total, currency)}</span>
        </div>
        <p className="text-muted-foreground text-xs">
          All-inclusive pricing. The price you see is the price you pay.
        </p>
      </CardContent>
    </Card>
  )
}
