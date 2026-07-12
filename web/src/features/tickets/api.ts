import { apiClient } from '@/lib/api'
import type { ApiJsonBody, ApiJsonResponse } from '@/lib/api'

export type TicketResponse = {
  ticketId: number
  eventId: number
  eventTitle: string
  eventStartsAt: string
  eventEndsAt: string
  eventTimeZoneId: string
  eventLocation: string | null
  eventIsOnline: boolean
  orderId: number
  ticketTypeId: number
  ticketTypeName: string
  code: string
  holderName: string
  holderEmail: string
  status: string
  issuedAt: string
}

export type OrderTicketsResponse = {
  orderId: number
  orderStatus: string
  tickets: TicketResponse[]
}

export type ResendTicketsRequest = {
  email: string
}

export type ResendTicketsResponse = {
  accepted: boolean
}

export type MyTicketsResponse = {
  tickets: TicketResponse[]
}

export type ReturnTicketResponse = ApiJsonResponse<
  '/api/orders/{orderId}/tickets/{ticketId}/return',
  'post'
>

export function getOrderTickets(orderId: number, signal?: AbortSignal) {
  return apiClient.get<OrderTicketsResponse>(`/api/orders/${orderId}/tickets`, {
    signal,
    suppressErrorToast: true,
  })
}

export function resendTickets(orderId: number, request: ResendTicketsRequest) {
  return apiClient.post<ResendTicketsResponse>(`/api/orders/${orderId}/tickets/resend`, request, {
    suppressErrorToast: true,
  })
}

export function getMyTickets(signal?: AbortSignal) {
  return apiClient.get<MyTicketsResponse>('/api/me/tickets', {
    signal,
    suppressErrorToast: true,
  })
}

export function returnTicket(orderId: number, ticketId: number) {
  return apiClient.post<ReturnTicketResponse>(
    `/api/orders/${orderId}/tickets/${ticketId}/return`,
    undefined,
    { suppressErrorToast: true },
  )
}

export type CheckInTicketRequest = ApiJsonBody<'/api/events/{eventId}/check-ins/scan', 'post'>
export type CheckInTicketResponse = ApiJsonResponse<'/api/events/{eventId}/check-ins/scan', 'post'>
export type SearchCheckInTicketsResponse = ApiJsonResponse<
  '/api/events/{eventId}/check-ins/tickets',
  'get'
>
export type DoorCountsResponse = ApiJsonResponse<'/api/events/{eventId}/check-ins/counts', 'get'>
export type BatchCheckInTicketsRequest = ApiJsonBody<'/api/events/{eventId}/check-ins/sync', 'post'>
export type BatchCheckInTicketsResponse = ApiJsonResponse<
  '/api/events/{eventId}/check-ins/sync',
  'post'
>

export function checkInByCode(
  eventId: number,
  request: CheckInTicketRequest,
  signal?: AbortSignal,
) {
  return apiClient.post<CheckInTicketResponse>(`/api/events/${eventId}/check-ins/scan`, request, {
    signal,
    suppressErrorToast: true,
  })
}

export function searchCheckInTickets(eventId: number, query: string, signal?: AbortSignal) {
  return apiClient.get<SearchCheckInTicketsResponse>(
    `/api/events/${eventId}/check-ins/tickets?query=${encodeURIComponent(query)}`,
    { signal, suppressErrorToast: true },
  )
}

export function checkInByTicketId(
  eventId: number,
  ticketId: number | string,
  signal?: AbortSignal,
) {
  return apiClient.post<CheckInTicketResponse>(
    `/api/events/${eventId}/check-ins/tickets/${ticketId}`,
    undefined,
    { signal, suppressErrorToast: true },
  )
}

export function getDoorCounts(eventId: number, signal?: AbortSignal) {
  return apiClient.get<DoorCountsResponse>(`/api/events/${eventId}/check-ins/counts`, {
    signal,
    suppressErrorToast: true,
  })
}

export function syncCheckIns(
  eventId: number,
  request: BatchCheckInTicketsRequest,
  signal?: AbortSignal,
) {
  return apiClient.post<BatchCheckInTicketsResponse>(
    `/api/events/${eventId}/check-ins/sync`,
    request,
    { signal, suppressErrorToast: true },
  )
}
