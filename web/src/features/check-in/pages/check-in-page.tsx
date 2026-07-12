import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ClipboardCheck, RefreshCw, Search, Signal, TicketCheck } from 'lucide-react'
import { useEffect, useMemo, useReducer, useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { toast } from 'sonner'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Field, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  type BatchCheckInTicketsRequest,
  type DoorCountsResponse,
  checkInByCode,
  checkInByTicketId,
  getDoorCounts,
  searchCheckInTickets,
  syncCheckIns,
} from '@/features/tickets/api'
import { ApiError } from '@/types/api-problem'

import { subscribeToEventCheckIn } from '../realtime'

type QueuedScan = BatchCheckInTicketsRequest['tickets'][number]

function parseEventId(value: string | null) {
  const parsed = Number(value)
  return Number.isInteger(parsed) && parsed > 0 ? parsed : null
}

function formatDate(value: string | null | undefined) {
  if (!value) return 'Not checked in'
  return new Date(value).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })
}

function scanQueueKey(eventId: number) {
  return `eventhub.checkInQueue.${eventId}`
}

function readQueuedScans(eventId: number): QueuedScan[] {
  const raw = window.localStorage.getItem(scanQueueKey(eventId))
  if (!raw) return []

  try {
    const parsed = JSON.parse(raw) as QueuedScan[]
    return Array.isArray(parsed) ? parsed : []
  } catch {
    return []
  }
}

function writeQueuedScans(eventId: number, scans: QueuedScan[]) {
  window.localStorage.setItem(scanQueueKey(eventId), JSON.stringify(scans))
}

export function CheckInPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const eventId = parseEventId(searchParams.get('eventId'))
  const [eventIdInput, setEventIdInput] = useState(searchParams.get('eventId') ?? '')
  const [code, setCode] = useState('')
  const [manualQuery, setManualQuery] = useState('')
  const [, refreshQueuedScans] = useReducer((version: number) => version + 1, 0)
  const queryClient = useQueryClient()
  const countsQueryKey = useMemo(() => ['check-in-counts', eventId] as const, [eventId])
  const queuedScans = eventId ? readQueuedScans(eventId) : []

  const countsQuery = useQuery({
    queryKey: countsQueryKey,
    queryFn: ({ signal }) => getDoorCounts(eventId!, signal),
    enabled: eventId !== null,
  })

  const searchQuery = useQuery({
    queryKey: ['check-in-search', eventId, manualQuery],
    queryFn: ({ signal }) => searchCheckInTickets(eventId!, manualQuery, signal),
    enabled: eventId !== null && manualQuery.trim().length >= 2,
  })

  const scanMutation = useMutation({
    mutationFn: (ticketCode: string) => checkInByCode(eventId!, { code: ticketCode }),
    onSuccess: async (ticket) => {
      toast.success(`${ticket.holderName} checked in.`)
      setCode('')
      await queryClient.invalidateQueries({ queryKey: countsQueryKey })
    },
    onError: (error) => {
      toast.error(error instanceof ApiError ? error.message : 'Ticket could not be checked in.')
    },
  })

  const manualMutation = useMutation({
    mutationFn: (ticketId: number | string) => checkInByTicketId(eventId!, ticketId),
    onSuccess: async (ticket) => {
      toast.success(`${ticket.holderName} checked in.`)
      await queryClient.invalidateQueries({ queryKey: countsQueryKey })
      await queryClient.invalidateQueries({ queryKey: ['check-in-search', eventId, manualQuery] })
    },
    onError: (error) => {
      toast.error(error instanceof ApiError ? error.message : 'Ticket could not be checked in.')
    },
  })

  const syncMutation = useMutation({
    mutationFn: (scans: QueuedScan[]) => syncCheckIns(eventId!, { tickets: scans }),
    onSuccess: async (response) => {
      const rejected = response.results.filter((result) => !result.accepted).length
      writeQueuedScans(eventId!, [])
      refreshQueuedScans()
      await queryClient.invalidateQueries({ queryKey: countsQueryKey })
      toast.success(
        rejected === 0
          ? 'Queued scans synced.'
          : `Queued scans synced with ${rejected} rejected scan${rejected === 1 ? '' : 's'}.`,
      )
    },
    onError: () => toast.error('Queued scans could not be synced.'),
  })

  useEffect(() => {
    if (eventId === null) return

    const subscription = subscribeToEventCheckIn(eventId, {
      onReconnect: async () => {
        await queryClient.invalidateQueries({ queryKey: countsQueryKey })
      },
      onUpdate: (message) => {
        queryClient.setQueryData<DoorCountsResponse>(countsQueryKey, {
          checkedIn: message.checkedIn,
          totalIssued: message.totalIssued,
        })
      },
    })

    return () => subscription.close()
  }, [countsQueryKey, eventId, queryClient])

  function handleEventSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const nextEventId = parseEventId(eventIdInput)
    if (!nextEventId) return
    setSearchParams({ eventId: String(nextEventId) })
  }

  function handleScanSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (eventId === null || code.trim().length === 0) return
    scanMutation.mutate(code.trim())
  }

  function queueScan() {
    if (eventId === null || code.trim().length === 0) return

    const nextQueue = [
      ...queuedScans,
      {
        clientScanId: crypto.randomUUID(),
        code: code.trim(),
        scannedAt: new Date().toISOString(),
      },
    ]
    writeQueuedScans(eventId, nextQueue)
    refreshQueuedScans()
    setCode('')
    toast.success('Scan queued for sync.')
  }

  const checkedIn = Number(countsQuery.data?.checkedIn ?? 0)
  const totalIssued = Number(countsQuery.data?.totalIssued ?? 0)
  const percent = totalIssued === 0 ? 0 : Math.round((checkedIn / totalIssued) * 100)

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <p className="text-primary text-sm font-medium">Door operations</p>
        <h1 className="text-2xl font-bold tracking-tight">Check-in</h1>
        <p className="text-muted-foreground max-w-2xl text-sm">
          Scan tickets, search manually, and reconcile queued scans when the connection returns.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Event</CardTitle>
        </CardHeader>
        <CardContent>
          <form
            className="flex flex-col gap-3 sm:flex-row sm:items-end"
            onSubmit={handleEventSubmit}
          >
            <Field className="max-w-xs">
              <FieldLabel htmlFor="check-in-event-id">Event ID</FieldLabel>
              <Input
                id="check-in-event-id"
                inputMode="numeric"
                value={eventIdInput}
                onChange={(event) => setEventIdInput(event.target.value)}
                required
              />
            </Field>
            <Button type="submit">Open check-in</Button>
          </form>
        </CardContent>
      </Card>

      {eventId === null ? null : (
        <>
          <div className="grid gap-3 md:grid-cols-3">
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm">Checked in</CardTitle>
              </CardHeader>
              <CardContent className="text-2xl font-semibold">
                {countsQuery.isPending ? <Skeleton className="h-8 w-24" /> : checkedIn}
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="text-sm">Issued</CardTitle>
              </CardHeader>
              <CardContent className="text-2xl font-semibold">
                {countsQuery.isPending ? <Skeleton className="h-8 w-24" /> : totalIssued}
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="pb-2">
                <CardTitle className="flex items-center gap-2 text-sm">
                  <Signal className="size-4" aria-hidden />
                  Live progress
                </CardTitle>
              </CardHeader>
              <CardContent className="text-2xl font-semibold">{percent}%</CardContent>
            </Card>
          </div>

          {countsQuery.isError ? (
            <Alert variant="destructive">
              <AlertDescription>Door counts could not be loaded for this event.</AlertDescription>
            </Alert>
          ) : null}

          <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(320px,0.8fr)]">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Scan ticket</CardTitle>
              </CardHeader>
              <CardContent>
                <form className="flex flex-col gap-3" onSubmit={handleScanSubmit}>
                  <Field>
                    <FieldLabel htmlFor="ticket-code">Ticket code</FieldLabel>
                    <Input
                      id="ticket-code"
                      value={code}
                      onChange={(event) => setCode(event.target.value)}
                      placeholder="tk_..."
                    />
                  </Field>
                  <div className="flex flex-wrap gap-2">
                    <Button
                      type="submit"
                      disabled={scanMutation.isPending || code.trim().length === 0}
                    >
                      <TicketCheck className="mr-2 size-4" aria-hidden />
                      Check in
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      disabled={code.trim().length === 0}
                      onClick={queueScan}
                    >
                      <ClipboardCheck className="mr-2 size-4" aria-hidden />
                      Queue offline
                    </Button>
                  </div>
                </form>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="text-base">Queued scans</CardTitle>
              </CardHeader>
              <CardContent className="flex flex-col gap-3">
                <div className="flex items-center justify-between gap-3">
                  <span className="text-muted-foreground text-sm">
                    {queuedScans.length} waiting to sync
                  </span>
                  <Button
                    type="button"
                    variant="outline"
                    disabled={queuedScans.length === 0 || syncMutation.isPending}
                    onClick={() => syncMutation.mutate(queuedScans)}
                  >
                    <RefreshCw className="mr-2 size-4" aria-hidden />
                    Sync
                  </Button>
                </div>
                {queuedScans.length > 0 ? (
                  <div className="flex flex-col gap-2">
                    {queuedScans.map((scan) => (
                      <div
                        key={scan.clientScanId}
                        className="border-border flex items-center justify-between rounded-md border px-3 py-2 text-sm"
                      >
                        <span className="font-mono">{scan.code}</span>
                        <Badge variant="secondary">Queued</Badge>
                      </div>
                    ))}
                  </div>
                ) : null}
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Manual lookup</CardTitle>
            </CardHeader>
            <CardContent className="flex flex-col gap-4">
              <Field>
                <FieldLabel htmlFor="check-in-search">Code or buyer email</FieldLabel>
                <div className="relative">
                  <Search className="text-muted-foreground pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2" />
                  <Input
                    id="check-in-search"
                    className="pl-9"
                    value={manualQuery}
                    onChange={(event) => setManualQuery(event.target.value)}
                  />
                </div>
              </Field>

              {searchQuery.data?.tickets.length ? (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Holder</TableHead>
                      <TableHead>Ticket</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead className="text-right">Action</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {searchQuery.data.tickets.map((ticket) => (
                      <TableRow key={ticket.ticketId}>
                        <TableCell>
                          <div className="flex flex-col">
                            <span>{ticket.holderName}</span>
                            <span className="text-muted-foreground text-xs">
                              {ticket.holderEmail}
                            </span>
                          </div>
                        </TableCell>
                        <TableCell>
                          <code className="font-mono text-xs break-all">{ticket.code}</code>
                        </TableCell>
                        <TableCell>
                          <Badge variant={ticket.status === 'checkedin' ? 'default' : 'secondary'}>
                            {ticket.status === 'checkedin'
                              ? `Checked in ${formatDate(ticket.checkedInAt)}`
                              : ticket.status}
                          </Badge>
                        </TableCell>
                        <TableCell className="text-right">
                          <Button
                            type="button"
                            size="sm"
                            disabled={ticket.status === 'checkedin' || manualMutation.isPending}
                            onClick={() => manualMutation.mutate(ticket.ticketId)}
                          >
                            Check in
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              ) : manualQuery.trim().length >= 2 && !searchQuery.isPending ? (
                <p className="text-muted-foreground text-sm">No matching tickets.</p>
              ) : null}
            </CardContent>
          </Card>
        </>
      )}
    </div>
  )
}
