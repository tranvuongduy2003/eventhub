import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertCircle, Mail, RotateCcw, TicketCheck } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, useParams } from 'react-router-dom'
import { toast } from 'sonner'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Field, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import { ApiError } from '@/types/api-problem'

import { getOrderTickets, resendTickets, returnTicket } from '../api'
import { TicketCard } from '../components/ticket-card'

export function OrderTicketsPage() {
  const { orderId } = useParams()
  const numericOrderId = Number(orderId)
  const isValidOrderId = Number.isInteger(numericOrderId) && numericOrderId > 0
  const [email, setEmail] = useState('')
  const queryClient = useQueryClient()

  const ticketsQuery = useQuery({
    queryKey: ['order-tickets', numericOrderId],
    queryFn: ({ signal }) => getOrderTickets(numericOrderId, signal),
    enabled: isValidOrderId,
    retry: false,
  })

  const resendMutation = useMutation({
    mutationFn: () => resendTickets(numericOrderId, { email }),
    onSuccess: () => {
      toast.success('Ticket email requested.')
      setEmail('')
    },
    onError: () => toast.error('Ticket email could not be requested.'),
  })

  const returnMutation = useMutation({
    mutationFn: (ticketId: number) => returnTicket(numericOrderId, ticketId),
    onSuccess: async () => {
      toast.success('Ticket returned to the pool.')
      await queryClient.invalidateQueries({ queryKey: ['order-tickets', numericOrderId] })
    },
    onError: (error) => {
      toast.error(error instanceof ApiError ? error.message : 'Ticket could not be returned.')
    },
  })

  function handleResend(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    resendMutation.mutate()
  }

  if (!isValidOrderId) {
    return <TicketProblem message="That ticket reference is not valid." />
  }

  if (ticketsQuery.isPending) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-96 w-full" />
      </div>
    )
  }

  if (ticketsQuery.isError) {
    const message =
      ticketsQuery.error instanceof ApiError && ticketsQuery.error.problem.status === 404
        ? 'We could not find tickets for that order reference.'
        : 'Tickets could not be loaded. Please try again.'

    return <TicketProblem message={message} />
  }

  const orderTickets = ticketsQuery.data
  const hasTickets = orderTickets.tickets.length > 0

  return (
    <div className="mx-auto flex w-full max-w-4xl flex-col gap-5 px-3 py-4 sm:px-6">
      <div className="flex flex-col gap-2">
        <p className="text-primary text-sm font-medium">Tickets</p>
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-2xl font-bold tracking-tight">Order #{orderTickets.orderId}</h1>
          <span className="text-muted-foreground text-sm capitalize">
            {orderTickets.orderStatus}
          </span>
        </div>
      </div>

      {!hasTickets ? (
        <Alert>
          <TicketCheck className="size-4" aria-hidden />
          <AlertDescription>
            Tickets will appear here after the order is confirmed and issued.
          </AlertDescription>
        </Alert>
      ) : null}

      <div className="flex flex-col gap-4">
        {orderTickets.tickets.map((ticket) => (
          <div key={ticket.ticketId} className="flex flex-col gap-2">
            <TicketCard ticket={ticket} />
            <div className="flex justify-end">
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={ticket.status !== 'valid' || returnMutation.isPending}
                onClick={() => returnMutation.mutate(ticket.ticketId)}
              >
                <RotateCcw className="mr-2 size-4" aria-hidden />
                Return ticket
              </Button>
            </div>
          </div>
        ))}
      </div>

      <Card className="shadow-sm">
        <CardHeader>
          <div className="bg-primary/10 text-primary mb-2 flex size-10 items-center justify-center rounded-lg">
            <Mail className="size-5" aria-hidden />
          </div>
          <CardTitle>Resend email</CardTitle>
          <CardDescription>Use the buyer email from this order.</CardDescription>
        </CardHeader>
        <CardContent>
          <form className="flex flex-col gap-3 sm:flex-row sm:items-end" onSubmit={handleResend}>
            <Field className="flex-1">
              <FieldLabel htmlFor="ticket-resend-email">Email</FieldLabel>
              <Input
                id="ticket-resend-email"
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
              />
            </Field>
            <Button type="submit" disabled={resendMutation.isPending}>
              {resendMutation.isPending ? 'Sending...' : 'Resend'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}

function TicketProblem({ message }: { message: string }) {
  return (
    <div className="mx-auto flex w-full max-w-xl flex-col gap-4 px-3 py-4 sm:px-6">
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
