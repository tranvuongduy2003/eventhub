import { apiClient } from '@/lib/api'

export type CreateDraftEventRequest = {
  title: string
  startsAt: string
  endsAt: string
  timeZoneId: string
  physicalAddress: string | null
  isOnline: boolean
}

export type DraftEventResponse = {
  eventId: number
  status: string
  createdAt: string
}

export type CoverImageResponse = {
  coverImageUrl: string
}

export type EditEventDetailsRequest = {
  title: string
  startsAt: string
  endsAt: string
  timeZoneId: string
  physicalAddress: string | null
  isOnline: boolean
  description: string | null
}

export type EventDetailsResponse = {
  eventId: number
  title: string
  description: string | null
  startsAt: string | null
  endsAt: string | null
  timeZoneId: string | null
  physicalAddress: string | null
  isOnline: boolean
  status: string
  updatedAt: string
}

export type PublishEventResponse = {
  status: string
  slug: string
  updatedAt: string
}

export type CloseEventResponse = {
  status: string
  updatedAt: string
}

export type CancelEventResponse = {
  status: string
  cancelledAt: string
  updatedAt: string
}

export type DuplicateEventResponse = {
  status: string
  createdAt: string
}

export function createDraftEvent(request: CreateDraftEventRequest, signal?: AbortSignal) {
  return apiClient.post<DraftEventResponse>('/api/events', request, {
    signal,
    suppressErrorToast: true,
  })
}

export function getPublicEventDetails(eventId: number, signal?: AbortSignal) {
  return apiClient.get<PublicEventResponse>(`/api/events/${eventId}/public`, {
    signal,
    suppressErrorToast: true,
  })
}

export function getEventDetails(eventId: number, signal?: AbortSignal) {
  return apiClient.get<EventDetailsResponse>(`/api/events/${eventId}`, {
    signal,
  })
}

export function editEventDetails(
  eventId: number,
  request: EditEventDetailsRequest,
  signal?: AbortSignal,
) {
  return apiClient.put<EventDetailsResponse>(`/api/events/${eventId}`, request, {
    signal,
    suppressErrorToast: true,
  })
}

export function uploadCoverImage(eventId: number, file: File, signal?: AbortSignal) {
  const formData = new FormData()
  formData.append('file', file)

  return apiClient.put<CoverImageResponse>(`/api/events/${eventId}/cover-image`, formData, {
    signal,
    suppressErrorToast: true,
  })
}

export function publishEvent(eventId: number, signal?: AbortSignal) {
  return apiClient.post<PublishEventResponse>(`/api/events/${eventId}/publish`, undefined, {
    signal,
    suppressErrorToast: true,
  })
}

export function closeEvent(eventId: number, signal?: AbortSignal) {
  return apiClient.post<CloseEventResponse>(`/api/events/${eventId}/close`, undefined, {
    signal,
    suppressErrorToast: true,
  })
}

export function cancelEvent(eventId: number, signal?: AbortSignal) {
  return apiClient.post<CancelEventResponse>(`/api/events/${eventId}/cancel`, undefined, {
    signal,
    suppressErrorToast: true,
  })
}

export function duplicateEvent(eventId: number, signal?: AbortSignal) {
  return apiClient.post<DuplicateEventResponse>(`/api/events/${eventId}/duplicate`, undefined, {
    signal,
    suppressErrorToast: true,
  })
}

export type PublicTicketTypeResponse = {
  ticketTypeId: number
  name: string
  priceAmount: number
  priceCurrency: string
  capacity: number
  maxPerOrder: number | null
  sold: number
  reserved: number
  available: number
  isSoldOut: boolean
}

export type PublicEventResponse = {
  eventId: number
  title: string
  description: string | null
  startsAt: string | null
  endsAt: string | null
  timeZoneId: string | null
  physicalAddress: string | null
  isOnline: boolean
  ticketTypes: PublicTicketTypeResponse[]
}

export type TicketTypeResponse = {
  ticketTypeId: number
  name: string
  priceAmount: number
  priceCurrency: string
  capacity: number
  maxPerOrder: number | null
  sold: number
  reserved: number
  createdAt: string
}

export function getTicketTypes(eventId: number, signal?: AbortSignal) {
  return apiClient.get<TicketTypeResponse[]>(`/api/events/${eventId}/ticket-types`, {
    signal,
    suppressErrorToast: true,
  })
}

export type EditTicketTypeRequest = {
  name: string
  priceAmount: number
  priceCurrency: string
  capacity: number
  maxPerOrder: number | null
}

export type EditTicketTypeResponse = {
  ticketTypeId: number
  name: string
  priceAmount: number
  priceCurrency: string
  capacity: number
  maxPerOrder: number | null
  sold: number
  reserved: number
  createdAt: string
  updatedAt: string
}

export function editTicketType(
  eventId: number,
  ticketTypeId: number,
  request: EditTicketTypeRequest,
  signal?: AbortSignal,
) {
  return apiClient.put<EditTicketTypeResponse>(
    `/api/events/${eventId}/ticket-types/${ticketTypeId}`,
    request,
    { signal, suppressErrorToast: true },
  )
}

export function removeTicketType(eventId: number, ticketTypeId: number, signal?: AbortSignal) {
  return apiClient.delete<void>(`/api/events/${eventId}/ticket-types/${ticketTypeId}`, {
    signal,
    suppressErrorToast: true,
  })
}

// --- Discount Codes ---

export type DiscountCodeResponse = {
  discountCodeId: number
  eventId: number
  code: string
  type: string
  value: number
  startAt: string | null
  endAt: string | null
  usageCap: number | null
  usedCount: number
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export type CreateDiscountCodeRequest = {
  code: string
  type: string
  value: number
  startAt: string | null
  endAt: string | null
  usageCap: number | null
}

export type UpdateDiscountCodeRequest = {
  type: string
  value: number
  startAt: string | null
  endAt: string | null
  usageCap: number | null
}

export type ValidateDiscountCodeResponse = {
  discountCodeId: number
  code: string
  type: string
  value: number
  discountAmount: number
  finalTotal: number
  currency: string
}

export function getDiscountCodes(eventId: number, signal?: AbortSignal) {
  return apiClient.get<DiscountCodeResponse[]>(`/api/events/${eventId}/discount-codes`, {
    signal,
    suppressErrorToast: true,
  })
}

export function createDiscountCode(
  eventId: number,
  request: CreateDiscountCodeRequest,
  signal?: AbortSignal,
) {
  return apiClient.post<DiscountCodeResponse>(`/api/events/${eventId}/discount-codes`, request, {
    signal,
    suppressErrorToast: true,
  })
}

export function updateDiscountCode(
  eventId: number,
  discountCodeId: number,
  request: UpdateDiscountCodeRequest,
  signal?: AbortSignal,
) {
  return apiClient.put<DiscountCodeResponse>(
    `/api/events/${eventId}/discount-codes/${discountCodeId}`,
    request,
    { signal, suppressErrorToast: true },
  )
}

export function deleteDiscountCode(eventId: number, discountCodeId: number, signal?: AbortSignal) {
  return apiClient.delete<void>(`/api/events/${eventId}/discount-codes/${discountCodeId}`, {
    signal,
    suppressErrorToast: true,
  })
}

export function validateDiscountCode(
  eventId: number,
  request: { code: string; orderTotalAmount: number; orderTotalCurrency: string },
  signal?: AbortSignal,
) {
  return apiClient.post<ValidateDiscountCodeResponse>(
    `/api/events/${eventId}/discount-codes/validate`,
    request,
    { signal, suppressErrorToast: true },
  )
}
