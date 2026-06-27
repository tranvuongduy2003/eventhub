import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Field, FieldError, FieldGroup, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'
import { ApiError } from '@/types/api-problem'
import { formatPrice } from '@/lib/utils/format-price'

import * as eventsApi from '../api'
import type { TicketTypeResponse } from '../api'

const ticketTypeFormSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'Name is required.')
    .max(200, 'Name must be 200 characters or fewer.'),
  priceAmount: z.number().min(0, 'Price must be non-negative.'),
  priceCurrency: z.string().trim().min(1, 'Currency is required.'),
  capacity: z
    .number()
    .int('Capacity must be a whole number.')
    .min(1, 'Capacity must be at least 1.'),
  maxPerOrder: z.number().int().min(1, 'Must be at least 1.').nullable(),
  salesWindowStart: z.string().nullable(),
  salesWindowEnd: z.string().nullable(),
})

type TicketTypeFormValues = z.infer<typeof ticketTypeFormSchema>

interface TicketTypeManagerProps {
  eventId: number
  eventStatus: string
}

export function TicketTypeManager({ eventId, eventStatus }: TicketTypeManagerProps) {
  const queryClient = useQueryClient()
  const [editingId, setEditingId] = useState<number | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const isDraft = eventStatus === 'Draft'

  const ticketTypesQuery = useQuery({
    queryKey: ['events', eventId, 'ticket-types'],
    queryFn: ({ signal }) => eventsApi.getTicketTypes(eventId, signal),
  })

  const ticketTypes = ticketTypesQuery.data ?? []

  const addMutation = useMutation({
    mutationFn: (request: eventsApi.EditTicketTypeRequest) =>
      eventsApi.editTicketType(eventId, 0, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['events', eventId, 'ticket-types'] })
      void queryClient.invalidateQueries({ queryKey: ['events', eventId] })
      setFormError(null)
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 422) {
        setFormError(error.problem.detail ?? 'Validation failed.')
      } else {
        setFormError('Something went wrong. Please try again.')
      }
    },
  })

  const editMutation = useMutation({
    mutationFn: ({
      ticketTypeId,
      request,
    }: {
      ticketTypeId: number
      request: eventsApi.EditTicketTypeRequest
    }) => eventsApi.editTicketType(eventId, ticketTypeId, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['events', eventId, 'ticket-types'] })
      void queryClient.invalidateQueries({ queryKey: ['events', eventId] })
      setEditingId(null)
      setFormError(null)
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 422) {
        setFormError(error.problem.detail ?? 'Validation failed.')
      } else {
        setFormError('Something went wrong. Please try again.')
      }
    },
  })

  const removeMutation = useMutation({
    mutationFn: (ticketTypeId: number) => eventsApi.removeTicketType(eventId, ticketTypeId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['events', eventId, 'ticket-types'] })
      void queryClient.invalidateQueries({ queryKey: ['events', eventId] })
    },
  })

  if (ticketTypesQuery.isPending) {
    return (
      <div className="space-y-3">
        <div className="bg-muted h-20 animate-pulse rounded-md" />
        <div className="bg-muted h-20 animate-pulse rounded-md" />
      </div>
    )
  }

  if (ticketTypesQuery.isError) {
    return (
      <Alert variant="destructive">
        <AlertDescription>Failed to load ticket types.</AlertDescription>
      </Alert>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium">Ticket types</h3>
        <span className="text-muted-foreground text-xs">{ticketTypes.length}/10</span>
      </div>

      {formError && (
        <Alert variant="destructive">
          <AlertDescription>{formError}</AlertDescription>
        </Alert>
      )}

      {ticketTypes.length === 0 && (
        <p className="text-muted-foreground text-sm">No ticket types yet. Add one below.</p>
      )}

      {ticketTypes.map((ticketType) => (
        <TicketTypeCard
          key={ticketType.ticketTypeId}
          ticketType={ticketType}
          isEditing={editingId === ticketType.ticketTypeId}
          isDraft={isDraft}
          onEdit={() => {
            setEditingId(ticketType.ticketTypeId)
            setFormError(null)
          }}
          onCancelEdit={() => {
            setEditingId(null)
            setFormError(null)
          }}
          onSave={(values) =>
            editMutation.mutate({
              ticketTypeId: ticketType.ticketTypeId,
              request: values,
            })
          }
          onRemove={() => removeMutation.mutate(ticketType.ticketTypeId)}
          isSaving={editMutation.isPending}
          isRemoving={removeMutation.isPending}
        />
      ))}

      {editingId === -1 && (
        <TicketTypeForm
          onSubmit={(values) => addMutation.mutate(values)}
          onCancel={() => {
            setEditingId(null)
            setFormError(null)
          }}
          isSaving={addMutation.isPending}
        />
      )}

      {isDraft && editingId === null && ticketTypes.length < 10 && (
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => {
            setEditingId(-1)
            setFormError(null)
          }}
        >
          Add ticket type
        </Button>
      )}

      {isDraft && ticketTypes.length >= 10 && (
        <p className="text-muted-foreground text-xs">Maximum of 10 ticket types reached.</p>
      )}
    </div>
  )
}

interface TicketTypeCardProps {
  ticketType: TicketTypeResponse
  isEditing: boolean
  isDraft: boolean
  onEdit: () => void
  onCancelEdit: () => void
  onSave: (values: TicketTypeFormValues) => void
  onRemove: () => void
  isSaving: boolean
  isRemoving: boolean
}

function TicketTypeCard({
  ticketType,
  isEditing,
  isDraft,
  onEdit,
  onCancelEdit,
  onSave,
  onRemove,
  isSaving,
  isRemoving,
}: TicketTypeCardProps) {
  const hasSales = ticketType.reserved + ticketType.sold > 0

  if (isEditing) {
    return (
      <TicketTypeForm
        defaultValues={{
          name: ticketType.name,
          priceAmount: ticketType.priceAmount,
          priceCurrency: ticketType.priceCurrency,
          capacity: ticketType.capacity,
          maxPerOrder: ticketType.maxPerOrder,
          salesWindowStart: ticketType.salesWindowStart,
          salesWindowEnd: ticketType.salesWindowEnd,
        }}
        onSubmit={onSave}
        onCancel={onCancelEdit}
        isSaving={isSaving}
        isPublishedEdit={!isDraft}
      />
    )
  }

  return (
    <Card>
      <CardContent className="flex items-center justify-between py-3">
        <div className="flex flex-col gap-1">
          <div className="flex items-center gap-2">
            <span className="font-medium">{ticketType.name}</span>
            {ticketType.reserved + ticketType.sold >= ticketType.capacity && (
              <Badge variant="destructive">Sold out</Badge>
            )}
          </div>
          <span className="text-muted-foreground text-sm">
            {ticketType.capacity} capacity · {ticketType.sold} sold · {ticketType.reserved} reserved
            {ticketType.maxPerOrder != null && ` · Max ${ticketType.maxPerOrder} per order`}
          </span>
          {ticketType.salesWindowStart != null && ticketType.salesWindowEnd != null && (
            <span className="text-muted-foreground text-xs">
              Sales: {new Date(ticketType.salesWindowStart).toLocaleString()} –{' '}
              {new Date(ticketType.salesWindowEnd).toLocaleString()}
            </span>
          )}
          {ticketType.salesWindowStart == null && ticketType.salesWindowEnd == null && (
            <span className="text-muted-foreground text-xs">Always on sale</span>
          )}
        </div>
        <div className="flex items-center gap-3">
          <span className="text-lg font-semibold">
            {formatPrice(ticketType.priceAmount, ticketType.priceCurrency)}
          </span>
          {
            <div className="flex gap-1">
              <Button type="button" variant="outline" size="sm" onClick={onEdit}>
                Edit
              </Button>
              {isDraft && (
                <AlertDialog>
                  <AlertDialogTrigger
                    render={
                      <Button type="button" variant="destructive" size="sm" disabled={isRemoving}>
                        Remove
                      </Button>
                    }
                  />
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>Remove ticket type?</AlertDialogTitle>
                      <AlertDialogDescription>
                        {hasSales
                          ? 'This ticket type has reserved or sold tickets and cannot be removed.'
                          : `This will remove "${ticketType.name}" from the event. This action cannot be undone.`}
                      </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>Cancel</AlertDialogCancel>
                      {!hasSales && (
                        <AlertDialogAction variant="destructive" onClick={onRemove}>
                          Remove
                        </AlertDialogAction>
                      )}
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>
              )}
            </div>
          }
        </div>
      </CardContent>
    </Card>
  )
}

interface TicketTypeFormProps {
  defaultValues?: TicketTypeFormValues
  onSubmit: (values: TicketTypeFormValues) => void
  onCancel: () => void
  isSaving: boolean
  isPublishedEdit?: boolean
}

function TicketTypeForm({
  defaultValues,
  onSubmit,
  onCancel,
  isSaving,
  isPublishedEdit,
}: TicketTypeFormProps) {
  const form = useForm<TicketTypeFormValues>({
    resolver: zodResolver(ticketTypeFormSchema),
    defaultValues: defaultValues ?? {
      name: '',
      priceAmount: 0,
      priceCurrency: 'VND',
      capacity: 100,
      maxPerOrder: null,
      salesWindowStart: null,
      salesWindowEnd: null,
    },
  })

  return (
    <Card>
      <CardContent className="py-4">
        <form
          className="flex flex-col gap-3"
          onSubmit={(e) => {
            e.preventDefault()
            void form.handleSubmit(onSubmit)(e)
          }}
          noValidate
        >
          <FieldGroup>
            <Field data-invalid={!!form.formState.errors.name}>
              <FieldLabel htmlFor="ticket-type-name">Name</FieldLabel>
              <Input
                id="ticket-type-name"
                autoComplete="off"
                disabled={isSaving || isPublishedEdit}
                placeholder="e.g. General Admission"
                {...form.register('name')}
              />
              <FieldError errors={[form.formState.errors.name]} />
            </Field>

            <div className="grid gap-4 sm:grid-cols-3">
              <Field data-invalid={!!form.formState.errors.priceAmount}>
                <FieldLabel htmlFor="ticket-type-price">Price</FieldLabel>
                <Input
                  id="ticket-type-price"
                  type="number"
                  min={0}
                  step={1000}
                  disabled={isSaving || isPublishedEdit}
                  {...form.register('priceAmount')}
                />
                <FieldError errors={[form.formState.errors.priceAmount]} />
              </Field>

              <Field data-invalid={!!form.formState.errors.priceCurrency}>
                <FieldLabel htmlFor="ticket-type-currency">Currency</FieldLabel>
                <Input
                  id="ticket-type-currency"
                  autoComplete="off"
                  disabled={isSaving || isPublishedEdit}
                  {...form.register('priceCurrency')}
                />
                <FieldError errors={[form.formState.errors.priceCurrency]} />
              </Field>

              <Field data-invalid={!!form.formState.errors.capacity}>
                <FieldLabel htmlFor="ticket-type-capacity">Capacity</FieldLabel>
                <Input
                  id="ticket-type-capacity"
                  type="number"
                  min={1}
                  disabled={isSaving || isPublishedEdit}
                  {...form.register('capacity')}
                />
                <FieldError errors={[form.formState.errors.capacity]} />
              </Field>
            </div>

            <Field data-invalid={!!form.formState.errors.maxPerOrder}>
              <FieldLabel htmlFor="ticket-type-max-per-order">Max per order</FieldLabel>
              <Input
                id="ticket-type-max-per-order"
                type="number"
                min={1}
                step={1}
                disabled={isSaving}
                placeholder="No limit"
                {...form.register('maxPerOrder', {
                  setValueAs: (v: string) => (v === '' ? null : Number(v)),
                })}
              />
              <FieldError errors={[form.formState.errors.maxPerOrder]} />
            </Field>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field data-invalid={!!form.formState.errors.salesWindowStart}>
                <FieldLabel htmlFor="ticket-type-sales-start">Sales window start</FieldLabel>
                <Input
                  id="ticket-type-sales-start"
                  type="datetime-local"
                  disabled={isSaving}
                  placeholder="Always on sale"
                  {...form.register('salesWindowStart', {
                    setValueAs: (v: string) => (v === '' ? null : v),
                  })}
                />
                <FieldError errors={[form.formState.errors.salesWindowStart]} />
              </Field>

              <Field data-invalid={!!form.formState.errors.salesWindowEnd}>
                <FieldLabel htmlFor="ticket-type-sales-end">Sales window end</FieldLabel>
                <Input
                  id="ticket-type-sales-end"
                  type="datetime-local"
                  disabled={isSaving}
                  placeholder="Always on sale"
                  {...form.register('salesWindowEnd', {
                    setValueAs: (v: string) => (v === '' ? null : v),
                  })}
                />
                <FieldError errors={[form.formState.errors.salesWindowEnd]} />
              </Field>
            </div>
          </FieldGroup>

          <div className="flex gap-2">
            <Button type="submit" size="sm" disabled={isSaving}>
              {isSaving ? (
                <>
                  <Spinner className="mr-2" />
                  Saving…
                </>
              ) : defaultValues ? (
                'Save changes'
              ) : (
                'Add ticket type'
              )}
            </Button>
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={isSaving}
              onClick={onCancel}
            >
              Cancel
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  )
}
