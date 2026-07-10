import { useMutation, useQuery } from '@tanstack/react-query'
import { Download, Mail, Save } from 'lucide-react'
import { useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { useParams } from 'react-router-dom'
import { toast } from 'sonner'

import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Switch } from '@/components/ui/switch'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Textarea } from '@/components/ui/textarea'
import { formatPrice } from '@/lib/utils/format-price'

import {
  exportEventAttendees,
  getEventAttendees,
  getEventResults,
  sendAttendeeMessage,
  setEventReminder,
} from '../api'

function formatDate(value: string | null) {
  if (!value) return 'Not checked in'
  return new Date(value).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' })
}

function downloadCsv(filename: string, csv: string) {
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = filename
  anchor.click()
  URL.revokeObjectURL(url)
}

export function EventResultsPage() {
  const { eventId } = useParams<{ eventId: string }>()
  const eventIdNum = eventId ? Number(eventId) : NaN
  const [subject, setSubject] = useState('')
  const [body, setBody] = useState('')
  const [reminderEnabled, setReminderEnabled] = useState(false)
  const [leadTimeMinutes, setLeadTimeMinutes] = useState(1440)

  const resultsQuery = useQuery({
    queryKey: ['event-results', eventIdNum],
    queryFn: ({ signal }) => getEventResults(eventIdNum, signal),
    enabled: !isNaN(eventIdNum),
  })

  const attendeesQuery = useQuery({
    queryKey: ['event-attendees', eventIdNum],
    queryFn: ({ signal }) => getEventAttendees(eventIdNum, signal),
    enabled: !isNaN(eventIdNum),
  })

  const exportMutation = useMutation({
    mutationFn: () => exportEventAttendees(eventIdNum),
    onSuccess: (csv) => downloadCsv(`event-${eventIdNum}-attendees.csv`, csv),
  })

  const messageMutation = useMutation({
    mutationFn: () => sendAttendeeMessage(eventIdNum, { subject, body }),
    onSuccess: (response) => {
      toast.success(`Message queued for ${response.acceptedRecipientCount} attendees.`)
      setSubject('')
      setBody('')
    },
  })

  const reminderMutation = useMutation({
    mutationFn: () =>
      setEventReminder(eventIdNum, {
        enabled: reminderEnabled,
        leadTimeMinutes,
      }),
    onSuccess: () => toast.success('Reminder settings saved.'),
  })

  const attendees = attendeesQuery.data?.attendees ?? []
  const results = resultsQuery.data
  const checkInPercent = useMemo(
    () => Math.round((results?.checkInRate ?? 0) * 100),
    [results?.checkInRate],
  )

  if (isNaN(eventIdNum)) {
    return (
      <Alert variant="destructive">
        <AlertDescription>Invalid event ID.</AlertDescription>
      </Alert>
    )
  }

  if (resultsQuery.isPending || attendeesQuery.isPending) {
    return (
      <div className="flex flex-col gap-6">
        <Skeleton className="h-20 w-full" />
        <Skeleton className="h-48 w-full" />
      </div>
    )
  }

  if (resultsQuery.isError || attendeesQuery.isError || !results) {
    return (
      <Alert variant="destructive">
        <AlertDescription>Results could not be loaded. Please try again.</AlertDescription>
      </Alert>
    )
  }

  function handleMessageSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    messageMutation.mutate()
  }

  function handleReminderSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    reminderMutation.mutate()
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
        <div className="flex flex-col gap-2">
          <p className="text-primary text-sm font-medium">Audience & results</p>
          <h1 className="text-2xl font-bold tracking-tight">{results.eventTitle}</h1>
        </div>
        <Button
          type="button"
          variant="outline"
          className="w-fit"
          onClick={() => exportMutation.mutate()}
          disabled={exportMutation.isPending}
        >
          <Download className="mr-2 size-4" aria-hidden />
          Export CSV
        </Button>
      </div>

      <div className="grid gap-3 md:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Sold</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-semibold">{results.issuedCount}</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Revenue</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-semibold">
            {formatPrice(results.totalRevenueAmount, results.totalRevenueCurrency)}
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Checked in</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-semibold">{checkInPercent}%</CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">No-shows</CardTitle>
          </CardHeader>
          <CardContent className="text-2xl font-semibold">{results.noShowCount}</CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Ticket types</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Type</TableHead>
                <TableHead>Sold</TableHead>
                <TableHead>Revenue</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {results.ticketsSoldByType.map((ticketType) => (
                <TableRow key={ticketType.ticketTypeId}>
                  <TableCell>{ticketType.ticketTypeName}</TableCell>
                  <TableCell>{ticketType.soldCount}</TableCell>
                  <TableCell>
                    {formatPrice(ticketType.revenueAmount, ticketType.revenueCurrency)}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Attendees</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Ticket</TableHead>
                <TableHead>Order</TableHead>
                <TableHead>Status</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {attendees.map((attendee) => (
                <TableRow key={attendee.ticketId}>
                  <TableCell>{attendee.name}</TableCell>
                  <TableCell>{attendee.email}</TableCell>
                  <TableCell>{attendee.ticketTypeName}</TableCell>
                  <TableCell>#{attendee.orderId}</TableCell>
                  <TableCell>
                    <Badge variant={attendee.checkedIn ? 'default' : 'secondary'}>
                      {attendee.checkedIn
                        ? `Checked in ${formatDate(attendee.checkedInAt)}`
                        : 'Not checked in'}
                    </Badge>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Message attendees</CardTitle>
          </CardHeader>
          <CardContent>
            <form className="flex flex-col gap-3" onSubmit={handleMessageSubmit}>
              <div className="grid gap-2">
                <Label htmlFor="message-subject">Subject</Label>
                <Input
                  id="message-subject"
                  value={subject}
                  onChange={(event) => setSubject(event.target.value)}
                />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="message-body">Body</Label>
                <Textarea
                  id="message-body"
                  value={body}
                  onChange={(event) => setBody(event.target.value)}
                />
              </div>
              <Button type="submit" className="w-fit" disabled={messageMutation.isPending}>
                <Mail className="mr-2 size-4" aria-hidden />
                Send
              </Button>
            </form>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Reminder</CardTitle>
          </CardHeader>
          <CardContent>
            <form className="flex flex-col gap-3" onSubmit={handleReminderSubmit}>
              <div className="flex items-center gap-3">
                <Switch checked={reminderEnabled} onCheckedChange={setReminderEnabled} />
                <Label>Automatic reminder</Label>
              </div>
              <div className="grid gap-2">
                <Label htmlFor="lead-time">Minutes before start</Label>
                <Input
                  id="lead-time"
                  type="number"
                  min={1}
                  value={leadTimeMinutes}
                  onChange={(event) => setLeadTimeMinutes(Number(event.target.value))}
                />
              </div>
              <Button type="submit" className="w-fit" disabled={reminderMutation.isPending}>
                <Save className="mr-2 size-4" aria-hidden />
                Save
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
