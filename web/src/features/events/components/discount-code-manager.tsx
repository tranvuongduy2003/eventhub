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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { ApiError } from '@/types/api-problem'
import { formatPrice } from '@/lib/utils/format-price'

import * as eventsApi from '../api'
import type { DiscountCodeResponse } from '../api'

const discountCodeFormSchema = z.object({
  code: z
    .string()
    .trim()
    .min(3, 'Code must be at least 3 characters.')
    .max(30, 'Code must be 30 characters or fewer.')
    .regex(/^[a-zA-Z0-9]+$/, 'Code must contain only letters and digits.'),
  type: z.enum(['Percentage', 'FixedAmount']),
  value: z.number().min(1, 'Value must be at least 1.'),
  startAt: z.string().nullable(),
  endAt: z.string().nullable(),
  usageCap: z.number().int().min(1, 'Must be at least 1.').nullable(),
})

type DiscountCodeFormValues = z.infer<typeof discountCodeFormSchema>

interface DiscountCodeManagerProps {
  eventId: number
}

export function DiscountCodeManager({ eventId }: DiscountCodeManagerProps) {
  const queryClient = useQueryClient()
  const [editingId, setEditingId] = useState<number | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const discountCodesQuery = useQuery({
    queryKey: ['events', eventId, 'discount-codes'],
    queryFn: ({ signal }) => eventsApi.getDiscountCodes(eventId, signal),
  })

  const discountCodes = discountCodesQuery.data ?? []

  const addMutation = useMutation({
    mutationFn: (request: eventsApi.CreateDiscountCodeRequest) =>
      eventsApi.createDiscountCode(eventId, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['events', eventId, 'discount-codes'] })
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

  const editMutation = useMutation({
    mutationFn: ({
      discountCodeId,
      request,
    }: {
      discountCodeId: number
      request: eventsApi.UpdateDiscountCodeRequest
    }) => eventsApi.updateDiscountCode(eventId, discountCodeId, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['events', eventId, 'discount-codes'] })
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
    mutationFn: (discountCodeId: number) => eventsApi.deleteDiscountCode(eventId, discountCodeId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['events', eventId, 'discount-codes'] })
    },
  })

  if (discountCodesQuery.isPending) {
    return (
      <div className="space-y-3">
        <div className="bg-muted h-20 animate-pulse rounded-md" />
        <div className="bg-muted h-20 animate-pulse rounded-md" />
      </div>
    )
  }

  if (discountCodesQuery.isError) {
    return (
      <Alert variant="destructive">
        <AlertDescription>Failed to load discount codes.</AlertDescription>
      </Alert>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium">Discount codes</h3>
      </div>

      {formError && (
        <Alert variant="destructive">
          <AlertDescription>{formError}</AlertDescription>
        </Alert>
      )}

      {discountCodes.length === 0 && editingId === null && (
        <p className="text-muted-foreground text-sm">
          No discount codes yet. Create one below to offer promotional pricing.
        </p>
      )}

      {discountCodes.map((dc) => (
        <DiscountCodeCard
          key={dc.discountCodeId}
          discountCode={dc}
          isEditing={editingId === dc.discountCodeId}
          onEdit={() => {
            setEditingId(dc.discountCodeId)
            setFormError(null)
          }}
          onCancelEdit={() => {
            setEditingId(null)
            setFormError(null)
          }}
          onSave={(values) =>
            editMutation.mutate({
              discountCodeId: dc.discountCodeId,
              request: {
                type: values.type,
                value: values.value,
                startAt: values.startAt,
                endAt: values.endAt,
                usageCap: values.usageCap,
              },
            })
          }
          onRemove={() => removeMutation.mutate(dc.discountCodeId)}
          isSaving={editMutation.isPending}
          isRemoving={removeMutation.isPending}
        />
      ))}

      {editingId === -1 && (
        <DiscountCodeForm
          onSubmit={(values) =>
            addMutation.mutate({
              code: values.code.toUpperCase(),
              type: values.type,
              value: values.value,
              startAt: values.startAt,
              endAt: values.endAt,
              usageCap: values.usageCap,
            })
          }
          onCancel={() => {
            setEditingId(null)
            setFormError(null)
          }}
          isSaving={addMutation.isPending}
        />
      )}

      {editingId === null && (
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => {
            setEditingId(-1)
            setFormError(null)
          }}
        >
          Add discount code
        </Button>
      )}
    </div>
  )
}

interface DiscountCodeCardProps {
  discountCode: DiscountCodeResponse
  isEditing: boolean
  onEdit: () => void
  onCancelEdit: () => void
  onSave: (values: DiscountCodeFormValues) => void
  onRemove: () => void
  isSaving: boolean
  isRemoving: boolean
}

function DiscountCodeCard({
  discountCode,
  isEditing,
  onEdit,
  onCancelEdit,
  onSave,
  onRemove,
  isSaving,
  isRemoving,
}: DiscountCodeCardProps) {
  if (isEditing) {
    return (
      <DiscountCodeForm
        defaultValues={{
          code: discountCode.code,
          type: discountCode.type as 'Percentage' | 'FixedAmount',
          value: discountCode.value,
          startAt: discountCode.startAt,
          endAt: discountCode.endAt,
          usageCap: discountCode.usageCap,
        }}
        onSubmit={onSave}
        onCancel={onCancelEdit}
        isSaving={isSaving}
      />
    )
  }

  const statusBadge = !discountCode.isActive ? (
    <Badge variant="secondary">Inactive</Badge>
  ) : discountCode.endAt && new Date(discountCode.endAt) < new Date() ? (
    <Badge variant="secondary">Expired</Badge>
  ) : discountCode.usageCap != null && discountCode.usedCount >= discountCode.usageCap ? (
    <Badge variant="secondary">Exhausted</Badge>
  ) : (
    <Badge variant="default">Active</Badge>
  )

  const valueDisplay =
    discountCode.type === 'Percentage'
      ? `${discountCode.value}% off`
      : formatPrice(discountCode.value, 'VND')

  return (
    <Card>
      <CardContent className="flex items-center justify-between py-3">
        <div className="flex flex-col gap-1">
          <div className="flex items-center gap-2">
            <span className="font-mono font-medium">{discountCode.code}</span>
            {statusBadge}
          </div>
          <span className="text-muted-foreground text-sm">
            {valueDisplay} ·{' '}
            {discountCode.usageCap != null
              ? `${discountCode.usedCount}/${discountCode.usageCap} used`
              : `${discountCode.usedCount} used · Unlimited`}
            {discountCode.startAt &&
              ` · From ${new Date(discountCode.startAt).toLocaleDateString()}`}
            {discountCode.endAt && ` · Until ${new Date(discountCode.endAt).toLocaleDateString()}`}
          </span>
        </div>
        <div className="flex gap-1">
          <Button type="button" variant="outline" size="sm" onClick={onEdit}>
            Edit
          </Button>
          <AlertDialog>
            <AlertDialogTrigger
              render={
                <Button type="button" variant="destructive" size="sm" disabled={isRemoving}>
                  Delete
                </Button>
              }
            />
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Delete discount code?</AlertDialogTitle>
                <AlertDialogDescription>
                  This will permanently delete &quot;{discountCode.code}&quot;. Existing orders that
                  used this code will retain their discount. This action cannot be undone.
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction variant="destructive" onClick={onRemove}>
                  Delete
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </div>
      </CardContent>
    </Card>
  )
}

interface DiscountCodeFormProps {
  defaultValues?: DiscountCodeFormValues
  onSubmit: (values: DiscountCodeFormValues) => void
  onCancel: () => void
  isSaving: boolean
}

function DiscountCodeForm({ defaultValues, onSubmit, onCancel, isSaving }: DiscountCodeFormProps) {
  const form = useForm<DiscountCodeFormValues>({
    resolver: zodResolver(discountCodeFormSchema),
    defaultValues: defaultValues ?? {
      code: '',
      type: 'Percentage',
      value: 10,
      startAt: null,
      endAt: null,
      usageCap: null,
    },
  })

  const watchedType = form.watch('type')
  const watchedValue = form.watch('value')
  const showPercentageWarning = watchedType === 'Percentage' && watchedValue > 50

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
            <div className="grid gap-4 sm:grid-cols-2">
              <Field data-invalid={!!form.formState.errors.code}>
                <FieldLabel htmlFor="discount-code">Code</FieldLabel>
                <Input
                  id="discount-code"
                  autoComplete="off"
                  disabled={isSaving || !!defaultValues}
                  placeholder="e.g. EARLY20"
                  {...form.register('code', {
                    setValueAs: (v: string) => v.toUpperCase(),
                  })}
                />
                <FieldError errors={[form.formState.errors.code]} />
              </Field>

              <Field data-invalid={!!form.formState.errors.type}>
                <FieldLabel htmlFor="discount-type">Type</FieldLabel>
                <Select
                  value={form.watch('type')}
                  onValueChange={(value) =>
                    form.setValue('type', value as 'Percentage' | 'FixedAmount')
                  }
                >
                  <SelectTrigger id="discount-type" disabled={isSaving}>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Percentage">Percentage</SelectItem>
                    <SelectItem value="FixedAmount">Fixed amount</SelectItem>
                  </SelectContent>
                </Select>
                <FieldError errors={[form.formState.errors.type]} />
              </Field>
            </div>

            <Field data-invalid={!!form.formState.errors.value}>
              <FieldLabel htmlFor="discount-value">
                {watchedType === 'Percentage' ? 'Percentage off' : 'Amount off'}
              </FieldLabel>
              <Input
                id="discount-value"
                type="number"
                min={1}
                max={watchedType === 'Percentage' ? 100 : undefined}
                step={watchedType === 'Percentage' ? 1 : 1000}
                disabled={isSaving}
                {...form.register('value', { valueAsNumber: true })}
              />
              <FieldError errors={[form.formState.errors.value]} />
              {showPercentageWarning && (
                <p className="text-xs text-amber-600">
                  Warning: This code will discount more than 50% of the order total.
                </p>
              )}
            </Field>

            <div className="grid gap-4 sm:grid-cols-2">
              <Field>
                <FieldLabel htmlFor="discount-start-at">Start date (optional)</FieldLabel>
                <Input
                  id="discount-start-at"
                  type="datetime-local"
                  disabled={isSaving}
                  {...form.register('startAt', {
                    setValueAs: (v: string) => (v === '' ? null : v),
                  })}
                />
              </Field>

              <Field>
                <FieldLabel htmlFor="discount-end-at">End date (optional)</FieldLabel>
                <Input
                  id="discount-end-at"
                  type="datetime-local"
                  disabled={isSaving}
                  {...form.register('endAt', {
                    setValueAs: (v: string) => (v === '' ? null : v),
                  })}
                />
              </Field>
            </div>

            <Field data-invalid={!!form.formState.errors.usageCap}>
              <FieldLabel htmlFor="discount-usage-cap">Usage cap (optional)</FieldLabel>
              <Input
                id="discount-usage-cap"
                type="number"
                min={1}
                step={1}
                disabled={isSaving}
                placeholder="Unlimited"
                {...form.register('usageCap', {
                  setValueAs: (v: string) => (v === '' ? null : Number(v)),
                })}
              />
              <FieldError errors={[form.formState.errors.usageCap]} />
            </Field>
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
                'Create discount code'
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
