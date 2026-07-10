import { useQuery } from '@tanstack/react-query'
import { Ticket } from 'lucide-react'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Skeleton } from '@/components/ui/skeleton'

import { getMyTickets } from '../api'
import { TicketCard } from '../components/ticket-card'

export function TicketWalletPage() {
  const ticketsQuery = useQuery({
    queryKey: ['my-tickets'],
    queryFn: ({ signal }) => getMyTickets(signal),
    retry: false,
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
          <TicketCard key={ticket.ticketId} ticket={ticket} />
        ))}
      </div>
    </div>
  )
}
