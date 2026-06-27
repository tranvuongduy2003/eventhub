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
import { ApiError } from '@/types/api-problem'
import { createEventFormSchema, type CreateEventFormValues } from '@/types/event'

import * as eventsApi from './api'

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

function detectTimeZone(): string {
  try {
    const tz = Intl.DateTimeFormat().resolvedOptions().timeZone
    return COMMON_TIMEZONES.includes(tz) ? tz : 'UTC'
  } catch {
    return 'UTC'
  }
}

function toIsoString(date: string, time: string): string {
  return new Date(`${date}T${time}`).toISOString()
}

export function CreateEventForm() {
  const navigate = useNavigate()
  const submittingRef = useRef(false)

  const defaultTimeZone = useMemo(() => detectTimeZone(), [])

  const form = useForm<CreateEventFormValues>({
    resolver: zodResolver(createEventFormSchema),
    mode: 'onBlur',
    reValidateMode: 'onChange',
    defaultValues: {
      title: '',
      startDate: '',
      startTime: '',
      endDate: '',
      endTime: '',
      timeZoneId: defaultTimeZone,
      isOnline: false,
      physicalAddress: '',
    },
  })

  const isOnline = form.watch('isOnline')

  const createMutation = useMutation({
    mutationFn: (values: CreateEventFormValues) =>
      eventsApi.createDraftEvent({
        title: values.title.trim(),
        startsAt: toIsoString(values.startDate, values.startTime),
        endsAt: toIsoString(values.endDate, values.endTime),
        timeZoneId: values.timeZoneId,
        physicalAddress: values.isOnline ? null : (values.physicalAddress?.trim() ?? null),
        isOnline: values.isOnline,
      }),
    onSuccess: () => {
      navigate(paths.organizerEvents, { replace: true })
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 422 && error.problem.errors) {
        for (const [field, messages] of Object.entries(error.problem.errors)) {
          const fieldName = field.charAt(0).toLowerCase() + field.slice(1)
          if (fieldName in form.getValues()) {
            form.setError(fieldName as keyof CreateEventFormValues, {
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

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (submittingRef.current || createMutation.isPending) {
      return
    }

    void form.handleSubmit((values) => {
      submittingRef.current = true
      form.clearErrors('root')
      createMutation.mutate(values)
    })(event)
  }

  return (
    <form className="flex flex-col gap-6" onSubmit={onSubmit} noValidate>
      {rootError?.message ? (
        <Alert variant="destructive">
          <AlertDescription>{rootError.message}</AlertDescription>
        </Alert>
      ) : null}

      <FieldGroup>
        <Field data-invalid={!!form.formState.errors.title}>
          <FieldLabel htmlFor="create-event-title">Event title</FieldLabel>
          <Input
            id="create-event-title"
            autoComplete="off"
            disabled={createMutation.isPending}
            {...form.register('title')}
          />
          <FieldError errors={[form.formState.errors.title]} />
        </Field>

        <div className="grid gap-4 sm:grid-cols-2">
          <Field data-invalid={!!form.formState.errors.startDate}>
            <FieldLabel htmlFor="create-event-start-date">Start date</FieldLabel>
            <Input
              id="create-event-start-date"
              type="date"
              disabled={createMutation.isPending}
              {...form.register('startDate')}
            />
            <FieldError errors={[form.formState.errors.startDate]} />
          </Field>

          <Field data-invalid={!!form.formState.errors.startTime}>
            <FieldLabel htmlFor="create-event-start-time">Start time</FieldLabel>
            <Input
              id="create-event-start-time"
              type="time"
              disabled={createMutation.isPending}
              {...form.register('startTime')}
            />
            <FieldError errors={[form.formState.errors.startTime]} />
          </Field>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          <Field data-invalid={!!form.formState.errors.endDate}>
            <FieldLabel htmlFor="create-event-end-date">End date</FieldLabel>
            <Input
              id="create-event-end-date"
              type="date"
              disabled={createMutation.isPending}
              {...form.register('endDate')}
            />
            <FieldError errors={[form.formState.errors.endDate]} />
          </Field>

          <Field data-invalid={!!form.formState.errors.endTime}>
            <FieldLabel htmlFor="create-event-end-time">End time</FieldLabel>
            <Input
              id="create-event-end-time"
              type="time"
              disabled={createMutation.isPending}
              {...form.register('endTime')}
            />
            <FieldError errors={[form.formState.errors.endTime]} />
          </Field>
        </div>

        <Field data-invalid={!!form.formState.errors.timeZoneId}>
          <FieldLabel htmlFor="create-event-timezone">Time zone</FieldLabel>
          <Controller
            control={form.control}
            name="timeZoneId"
            render={({ field }) => (
              <Select value={field.value} onValueChange={field.onChange}>
                <SelectTrigger id="create-event-timezone" className="w-full">
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
                  id="create-event-is-online"
                  checked={field.value}
                  onCheckedChange={field.onChange}
                  disabled={createMutation.isPending}
                />
              )}
            />
            <FieldLabel htmlFor="create-event-is-online" className="cursor-pointer">
              Online event
            </FieldLabel>
          </div>
        </Field>

        {!isOnline ? (
          <Field data-invalid={!!form.formState.errors.physicalAddress}>
            <FieldLabel htmlFor="create-event-address">Physical address</FieldLabel>
            <Input
              id="create-event-address"
              autoComplete="off"
              disabled={createMutation.isPending}
              placeholder="e.g. 123 Main St, City"
              {...form.register('physicalAddress')}
            />
            <FieldError errors={[form.formState.errors.physicalAddress]} />
          </Field>
        ) : null}
      </FieldGroup>

      <div className="flex gap-3">
        <Button type="submit" disabled={createMutation.isPending}>
          {createMutation.isPending ? (
            <>
              <Spinner className="mr-2" />
              Creating event…
            </>
          ) : (
            'Create event'
          )}
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={createMutation.isPending}
          onClick={() => navigate(paths.organizerEvents)}
        >
          Cancel
        </Button>
      </div>
    </form>
  )
}
