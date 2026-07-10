import { apiClient } from '@/lib/api'

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
