import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation } from '@tanstack/react-query'
import { useRef, type FormEvent, useMemo } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'

import { paths } from '@/app/paths'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Field, FieldError, FieldGroup, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { Switch } from '@/components/ui/switch'
import { Textarea } from '@/components/ui/textarea'
import { ApiError } from '@/types/api-problem'
import { editEventFormSchema, type EditEventFormValues } from '@/types/event'

import * as eventsApi from './api'
import type { EventDetailsResponse } from './api'

const COMMON_TIMEZONES = [
  'UTC',
  'America/New_York',
  'America/Chicago',
  'America/Denver',
  'America/Los_Angeles',
  'America/Sao_Paulo',
  'Europe/London',
  'Europe/Paris',
  'Europe/Berlin',
  'Europe/Moscow',
  'Asia/Dubai',
  'Asia/Kolkata',
  'Asia/Bangkok',
  'Asia/Ho_Chi_Minh',
  'Asia/Shanghai',
  'Asia/Tokyo',
  'Australia/Sydney',
  'Pacific/Auckland',
]

function toIsoString(date: string, time: string): string {
  return new Date(`${date}T${time}`).toISOString()
}

function splitIsoDateTime(isoString: string): { date: string; time: string } {
  const d = new Date(isoString)
  const year = d.getFullYear()
  const month = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  const hours = String(d.getHours()).padStart(2, '0')
  const minutes = String(d.getMinutes()).padStart(2, '0')
  return {
    date: `${year}-${month}-${day}`,
    time: `${hours}:${minutes}`,
  }
}

interface EditEventFormProps {
  event: EventDetailsResponse
}

export function EditEventForm({ event }: EditEventFormProps) {
  const navigate = useNavigate()
  const submittingRef = useRef(false)

  const { date: startDate, time: startTime } = useMemo(
    () => splitIsoDateTime(event.startsAt),
    [event.startsAt],
  )
  const { date: endDate, time: endTime } = useMemo(
    () => splitIsoDateTime(event.endsAt),
    [event.endsAt],
  )

  const isTerminal = event.status === 'Closed' || event.status === 'Cancelled'

  const form = useForm<EditEventFormValues>({
    resolver: zodResolver(editEventFormSchema),
    mode: 'onBlur',
    reValidateMode: 'onChange',
    defaultValues: {
      title: event.title,
      startDate,
      startTime,
      endDate,
      endTime,
      timeZoneId: event.timeZoneId,
      isOnline: event.isOnline,
      physicalAddress: event.physicalAddress ?? '',
      description: event.description ?? '',
    },
  })

  const isOnline = form.watch('isOnline')

  const editMutation = useMutation({
    mutationFn: (values: EditEventFormValues) =>
      eventsApi.editEventDetails(event.eventId, {
        title: values.title.trim(),
        startsAt: toIsoString(values.startDate, values.startTime),
        endsAt: toIsoString(values.endDate, values.endTime),
        timeZoneId: values.timeZoneId,
        physicalAddress: values.isOnline ? null : (values.physicalAddress?.trim() ?? null),
        isOnline: values.isOnline,
        description: values.description?.trim() || null,
      }),
    onSuccess: () => {
      navigate(paths.events, { replace: true })
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 422 && error.problem.errors) {
        for (const [field, messages] of Object.entries(error.problem.errors)) {
          const fieldName = field.charAt(0).toLowerCase() + field.slice(1)
          if (fieldName in form.getValues()) {
            form.setError(fieldName as keyof EditEventFormValues, {
              message: Array.isArray(messages) ? messages[0] : String(messages),
            })
          } else {
            form.setError('root', {
              message: Array.isArray(messages) ? messages[0] : String(messages),
            })
          }
        }
      } else {
        form.setError('root', { message: 'Something went wrong. Please try again.' })
      }
    },
    onSettled: () => {
      submittingRef.current = false
    },
  })

  const rootError = form.formState.errors.root

  if (isTerminal) {
    return (
      <Alert>
        <AlertDescription>
          This event is {event.status.toLowerCase()} and cannot be edited.
        </AlertDescription>
      </Alert>
    )
  }

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()

    if (submittingRef.current || editMutation.isPending) {
      return
    }

    void form.handleSubmit((values) => {
      submittingRef.current = true
      form.clearErrors('root')
      editMutation.mutate(values)
    })(e)
  }

  return (
    <form className="flex flex-col gap-6" onSubmit={onSubmit} noValidate>
      {rootError?.message ? (
        <Alert variant="destructive">
          <AlertDescription>{rootError.message}</AlertDescription>
        </Alert>
      ) : null}

      {event.status === 'Published' ? (
        <Alert>
          <AlertDescription>
            This event is published. Changes will be visible to attendees.
          </AlertDescription>
        </Alert>
      ) : null}

      <FieldGroup>
        <Field data-invalid={!!form.formState.errors.title}>
          <FieldLabel htmlFor="edit-event-title">Event title</FieldLabel>
          <Input
            id="edit-event-title"
            autoComplete="off"
            disabled={editMutation.isPending}
            {...form.register('title')}
          />
          <FieldError errors={[form.formState.errors.title]} />
        </Field>

        <Field data-invalid={!!form.formState.errors.description}>
          <FieldLabel htmlFor="edit-event-description">Description</FieldLabel>
          <Textarea
            id="edit-event-description"
            autoComplete="off"
            disabled={editMutation.isPending}
            placeholder="Optional event description"
            rows={4}
            {...form.register('description')}
          />
          <FieldError errors={[form.formState.errors.description]} />
        </Field>

        <div className="grid gap-4 sm:grid-cols-2">
          <Field data-invalid={!!form.formState.errors.startDate}>
            <FieldLabel htmlFor="edit-event-start-date">Start date</FieldLabel>
            <Input
              id="edit-event-start-date"
              type="date"
              disabled={editMutation.isPending}
              {...form.register('startDate')}
            />
            <FieldError errors={[form.formState.errors.startDate]} />
          </Field>

          <Field data-invalid={!!form.formState.errors.startTime}>
            <FieldLabel htmlFor="edit-event-start-time">Start time</FieldLabel>
            <Input
              id="edit-event-start-time"
              type="time"
              disabled={editMutation.isPending}
              {...form.register('startTime')}
            />
            <FieldError errors={[form.formState.errors.startTime]} />
          </Field>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <Field data-invalid={!!form.formState.errors.endDate}>
            <FieldLabel htmlFor="edit-event-end-date">End date</FieldLabel>
            <Input
              id="edit-event-end-date"
              type="date"
              disabled={editMutation.isPending}
              {...form.register('endDate')}
            />
            <FieldError errors={[form.formState.errors.endDate]} />
          </Field>

          <Field data-invalid={!!form.formState.errors.endTime}>
            <FieldLabel htmlFor="edit-event-end-time">End time</FieldLabel>
            <Input
              id="edit-event-end-time"
              type="time"
              disabled={editMutation.isPending}
              {...form.register('endTime')}
            />
            <FieldError errors={[form.formState.errors.endTime]} />
          </Field>
        </div>

        <Field data-invalid={!!form.formState.errors.timeZoneId}>
          <FieldLabel htmlFor="edit-event-timezone">Time zone</FieldLabel>
          <Controller
            control={form.control}
            name="timeZoneId"
            render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange}>
                <SelectTrigger id="edit-event-timezone" className="w-full">
                  <SelectValue placeholder="Select time zone" />
                </SelectTrigger>
                <SelectContent>
                  {COMMON_TIMEZONES.map((tz) => (
                    <SelectItem key={tz} value={tz}>
                      {tz}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          />
          <FieldError errors={[form.formState.errors.timeZoneId]} />
        </Field>

        <Field>
          <div className="flex items-center gap-3">
            <Controller
              control={form.control}
              name="isOnline"
              render={({ field }) => (
                <Switch
                  id="edit-event-is-online"
                  checked={field.value}
                  onCheckedChange={field.onChange}
                  disabled={editMutation.isPending}
                />
              )}
            />
            <FieldLabel htmlFor="edit-event-is-online" className="cursor-pointer">
              Online event
            </FieldLabel>
          </div>
        </Field>

        {!isOnline ? (
          <Field data-invalid={!!form.formState.errors.physicalAddress}>
            <FieldLabel htmlFor="edit-event-address">Physical address</FieldLabel>
            <Input
              id="edit-event-address"
              autoComplete="off"
              disabled={editMutation.isPending}
              placeholder="e.g. 123 Main St, City"
              {...form.register('physicalAddress')}
            />
            <FieldError errors={[form.formState.errors.physicalAddress]} />
          </Field>
        ) : null}
      </FieldGroup>

      <div className="flex gap-3">
        <Button type="submit" disabled={editMutation.isPending}>
          {editMutation.isPending ? (
            <>
              <Spinner className="mr-2" />
              Saving changes…
            </>
          ) : (
            'Save changes'
          )}
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={editMutation.isPending}
          onClick={() => navigate(paths.events)}
        >
          Cancel
        </Button>
      </div>
    </form>
  )
}
