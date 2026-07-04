import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery } from '@tanstack/react-query'
import { useMemo, useRef, type FormEvent } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useSearchParams } from 'react-router-dom'
import { CheckCircle2, ShoppingCart, UserRound } from 'lucide-react'
import { z } from 'zod'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Field, FieldError, FieldGroup, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { Spinner } from '@/components/ui/spinner'
import { ApiError } from '@/types/api-problem'

import { placeOrder, startCheckout, type PlaceOrderResponse } from '../api'
import { OrderSummary, type OrderLineItem } from '../components/order-summary'
import {
  decodeTicketSelection,
  readStoredSelection,
  selectionToLines,
  writeStoredSelection,
} from '../selection-storage'

const guestCheckoutSchema = z.object({
  contactName: z
    .string()
    .trim()
    .min(1, 'Name is required.')
    .max(200, 'Name must not exceed 200 characters.'),
  contactEmail: z
    .string()
    .trim()
    .min(1, 'Email is required.')
    .max(254, 'Email cannot exceed 254 characters.')
    .email('Email address format is invalid.'),
})

type GuestCheckoutValues = z.infer<typeof guestCheckoutSchema>

function checkoutErrorMessage(error: unknown) {
  if (error instanceof ApiError) {
    return error.problem.detail ?? error.message
  }

  return 'Checkout could not be completed. Please try again.'
}

export function CheckoutPlaceholderPage() {
  const [searchParams] = useSearchParams()
  const eventSlug = searchParams.get('event') ?? ''
  const submittingRef = useRef(false)

  const selection = useMemo(() => {
    const fromUrl = decodeTicketSelection(searchParams.get('tickets'))
    return Object.keys(fromUrl).length > 0 || !eventSlug ? fromUrl : readStoredSelection(eventSlug)
  }, [eventSlug, searchParams])

  const lines = useMemo(() => selectionToLines(selection), [selection])

  const form = useForm<GuestCheckoutValues>({
    resolver: zodResolver(guestCheckoutSchema),
    mode: 'onBlur',
    reValidateMode: 'onChange',
    defaultValues: {
      contactName: '',
      contactEmail: '',
    },
  })

  const checkoutQuery = useQuery({
    queryKey: ['checkout-start', eventSlug, searchParams.get('tickets')],
    queryFn: ({ signal }) => startCheckout(eventSlug, { lines }, signal),
    enabled: !!eventSlug && lines.length > 0,
    retry: false,
  })

  const orderMutation = useMutation({
    mutationFn: (values: GuestCheckoutValues) => {
      const checkout = checkoutQuery.data
      if (!checkout) {
        throw new Error('Ticket selection could not be validated.')
      }

      return placeOrder(checkout.eventId, {
        contactName: values.contactName,
        contactEmail: values.contactEmail,
        lines,
      })
    },
    onSuccess: () => {
      if (eventSlug) {
        writeStoredSelection(eventSlug, {})
      }
    },
    onError: (error) => {
      if (error instanceof ApiError && error.problem.errors) {
        for (const [field, messages] of Object.entries(error.problem.errors)) {
          const normalizedField = field.charAt(0).toLowerCase() + field.slice(1)
          if (normalizedField === 'contactName' || normalizedField === 'contactEmail') {
            form.setError(normalizedField, { message: messages.join(' ') })
          }
        }
      }

      form.setError('root', { message: checkoutErrorMessage(error) })
    },
    onSettled: () => {
      submittingRef.current = false
    },
  })

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (submittingRef.current || orderMutation.isPending) {
      return
    }

    void form.handleSubmit((values) => {
      submittingRef.current = true
      form.clearErrors()
      orderMutation.mutate(values)
    })(event)
  }

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
  const placedOrder = orderMutation.data
  const lineItems: OrderLineItem[] = checkout.lines.map((line) => ({
    ticketTypeName: line.ticketTypeName,
    unitPrice: line.unitPriceAmount,
    currency: line.unitPriceCurrency,
    quantity: line.quantity,
  }))

  if (placedOrder) {
    return (
      <AcceptedOrder
        eventTitle={checkout.eventTitle}
        eventSlug={checkout.eventSlug}
        order={placedOrder}
        lineItems={lineItems}
      />
    )
  }

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
              <UserRound className="size-5" aria-hidden />
            </div>
            <CardTitle>Guest checkout</CardTitle>
            <CardDescription>
              Enter the buyer details for this order. No account is required.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form className="flex flex-col gap-5" onSubmit={onSubmit} noValidate>
              {form.formState.errors.root?.message ? (
                <Alert variant="destructive">
                  <AlertDescription>{form.formState.errors.root.message}</AlertDescription>
                </Alert>
              ) : null}

              <FieldGroup>
                <Field data-invalid={!!form.formState.errors.contactName}>
                  <FieldLabel htmlFor="guest-contact-name">Name</FieldLabel>
                  <Input
                    id="guest-contact-name"
                    type="text"
                    autoComplete="name"
                    disabled={orderMutation.isPending}
                    {...form.register('contactName')}
                  />
                  <FieldError errors={[form.formState.errors.contactName]} />
                </Field>

                <Field data-invalid={!!form.formState.errors.contactEmail}>
                  <FieldLabel htmlFor="guest-contact-email">Email</FieldLabel>
                  <Input
                    id="guest-contact-email"
                    type="email"
                    autoComplete="email"
                    disabled={orderMutation.isPending}
                    {...form.register('contactEmail')}
                  />
                  <FieldError errors={[form.formState.errors.contactEmail]} />
                </Field>
              </FieldGroup>

              <Button type="submit" className="w-full" disabled={orderMutation.isPending}>
                {orderMutation.isPending ? (
                  <>
                    <Spinner className="mr-2" />
                    Placing order...
                  </>
                ) : (
                  <>
                    <ShoppingCart className="mr-2 size-4" aria-hidden />
                    Continue as guest
                  </>
                )}
              </Button>
            </form>

            <Link
              className="text-primary mt-4 inline-flex text-sm font-medium underline underline-offset-4"
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

interface AcceptedOrderProps {
  eventTitle: string
  eventSlug: string
  order: PlaceOrderResponse
  lineItems: OrderLineItem[]
}

function AcceptedOrder({ eventTitle, eventSlug, order, lineItems }: AcceptedOrderProps) {
  const statusLabel = order.status.charAt(0).toUpperCase() + order.status.slice(1)

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <p className="text-primary text-sm font-medium">Order accepted</p>
        <h1 className="text-2xl font-bold tracking-tight">{eventTitle}</h1>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
        <Card className="shadow-sm">
          <CardHeader>
            <div className="mb-2 flex size-10 items-center justify-center rounded-lg bg-green-100 text-green-700">
              <CheckCircle2 className="size-5" aria-hidden />
            </div>
            <CardTitle>Order #{order.orderId}</CardTitle>
            <CardDescription>Status: {statusLabel}</CardDescription>
          </CardHeader>
          <CardContent className="text-muted-foreground flex flex-col gap-3 text-sm">
            <p>Your guest details were accepted for this order.</p>
            <Link
              className="text-primary font-medium underline underline-offset-4"
              to={`/events/${eventSlug}`}
            >
              Back to event
            </Link>
          </CardContent>
        </Card>

        <OrderSummary
          lineItems={lineItems}
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
