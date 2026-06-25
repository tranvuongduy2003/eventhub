import { ShoppingCart } from 'lucide-react'

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

import { OrderSummary } from '../components/order-summary'
import type { OrderLineItem } from '../components/order-summary'

const demoLineItems: OrderLineItem[] = [
  { ticketTypeName: 'General Admission', unitPrice: 150_000, currency: 'VND', quantity: 2 },
  { ticketTypeName: 'VIP', unitPrice: 350_000, currency: 'VND', quantity: 1 },
]

export function CheckoutPlaceholderPage() {
  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <p className="text-primary text-sm font-medium">Purchase flow</p>
        <h1 className="text-2xl font-bold tracking-tight">Checkout</h1>
        <p className="text-muted-foreground max-w-2xl text-sm">
          A streamlined checkout with clear line items and all-inclusive pricing.
        </p>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
        <Card className="shadow-sm">
          <CardHeader>
            <div className="bg-primary/10 text-primary mb-2 flex size-10 items-center justify-center rounded-lg">
              <ShoppingCart className="size-5" aria-hidden />
            </div>
            <CardTitle>Coming soon</CardTitle>
            <CardDescription>
              Guest checkout and order holds (EP-5) will live here — ticket selection, price
              summary, and payment handoff.
            </CardDescription>
          </CardHeader>
          <CardContent className="text-muted-foreground text-sm">
            Buyers will see the final price upfront with no hidden fees at the last step.
          </CardContent>
        </Card>

        <OrderSummary lineItems={demoLineItems} />
      </div>
    </div>
  )
}
