import { useMutation } from '@tanstack/react-query'
import { CreditCard, RotateCw } from 'lucide-react'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { formatPrice } from '@/lib/utils/format-price'
import { ApiError } from '@/types/api-problem'

import { startPayment } from '../api'

type PaymentActionProps = {
  orderId: number
  totalAmount: number
  totalCurrency: string
  buttonLabel?: string
  compact?: boolean
}

function paymentErrorMessage(error: unknown) {
  if (error instanceof ApiError) {
    return error.problem.detail ?? error.message
  }

  return 'Payment could not be started. Please try again.'
}

function orderUrl(orderId: number, paymentState: 'returned' | 'cancelled') {
  const url = new URL(`/orders/${orderId}`, window.location.origin)
  url.searchParams.set('payment', paymentState)
  return url.toString()
}

export function PaymentAction({
  orderId,
  totalAmount,
  totalCurrency,
  buttonLabel = 'Pay securely',
  compact = false,
}: PaymentActionProps) {
  const paymentMutation = useMutation({
    mutationFn: () =>
      startPayment(orderId, {
        successUrl: orderUrl(orderId, 'returned'),
        cancelUrl: orderUrl(orderId, 'cancelled'),
      }),
    onSuccess: (payment) => {
      window.location.assign(payment.redirectUrl)
    },
  })

  const amount = formatPrice(totalAmount, totalCurrency)

  return (
    <div className={compact ? 'flex flex-col gap-3' : 'flex flex-col gap-4'}>
      {paymentMutation.isError ? (
        <Alert variant="destructive">
          <AlertDescription>{paymentErrorMessage(paymentMutation.error)}</AlertDescription>
        </Alert>
      ) : null}

      <div className="rounded-lg border p-3 text-sm">
        <p className="font-medium">Provider checkout</p>
        <p className="text-muted-foreground mt-1">
          You will pay {amount} with the payment provider. EventHub adds no platform fee and never
          asks for card details.
        </p>
      </div>

      <Button
        type="button"
        className="w-full sm:w-fit"
        disabled={paymentMutation.isPending}
        onClick={() => paymentMutation.mutate()}
      >
        {paymentMutation.isPending ? (
          <>
            <Spinner className="mr-2" />
            Opening provider...
          </>
        ) : paymentMutation.isError ? (
          <>
            <RotateCw className="mr-2 size-4" aria-hidden />
            Try payment again
          </>
        ) : (
          <>
            <CreditCard className="mr-2 size-4" aria-hidden />
            {buttonLabel}
          </>
        )}
      </Button>
    </div>
  )
}
