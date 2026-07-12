import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { RotateCcw, Ticket } from 'lucide-react'
import { toast } from 'sonner'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { ApiError } from '@/types/api-problem'

import { getMyTickets, returnTicket } from '../api'
import { TicketCard } from '../components/ticket-card'

export function TicketWalletPage() {
  const queryClient = useQueryClient()
  const ticketsQuery = useQuery({
    queryKey: ['my-tickets'],
    queryFn: ({ signal }) => getMyTickets(signal),
    retry: false,
  })

  const returnMutation = useMutation({
    mutationFn: ({ orderId, ticketId }: { orderId: number; ticketId: number }) =>
      returnTicket(orderId, ticketId),
    onSuccess: async () => {
      toast.success('Ticket returned to the pool.')
      await queryClient.invalidateQueries({ queryKey: ['my-tickets'] })
    },
    onError: (error) => {
      toast.error(error instanceof ApiError ? error.message : 'Ticket could not be returned.')
    },
  })

  if (ticketsQuery.isPending) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-24 w-full" />
        <Skeleton className="h-96 w-full" />
      </div>
    )
  }

  const tickets = ticketsQuery.data?.tickets ?? []

  return (
    <div className="flex flex-col gap-5">
      <div className="flex flex-col gap-2">
        <p className="text-primary text-sm font-medium">Wallet</p>
        <h1 className="text-2xl font-bold tracking-tight">Tickets</h1>
      </div>

      {ticketsQuery.isError ? (
        <Alert variant="destructive">
          <AlertDescription>Tickets could not be loaded.</AlertDescription>
        </Alert>
      ) : null}

      {tickets.length === 0 && !ticketsQuery.isError ? (
        <Alert>
          <Ticket className="size-4" aria-hidden />
          <AlertDescription>No tickets are linked to this account email yet.</AlertDescription>
        </Alert>
      ) : null}

      <div className="flex flex-col gap-4">
        {tickets.map((ticket) => (
          <div key={ticket.ticketId} className="flex flex-col gap-2">
            <TicketCard ticket={ticket} />
            <div className="flex justify-end">
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={ticket.status !== 'valid' || returnMutation.isPending}
                onClick={() =>
                  returnMutation.mutate({ orderId: ticket.orderId, ticketId: ticket.ticketId })
                }
              >
                <RotateCcw className="mr-2 size-4" aria-hidden />
                Return ticket
              </Button>
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}
