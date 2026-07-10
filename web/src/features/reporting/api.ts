import { apiClient } from '@/lib/api'

export type EventAttendeeResponse = {
  name: string
  email: string
  ticketTypeId: number
  ticketTypeName: string
  orderId: number
  ticketId: number
  checkedIn: boolean
  checkedInAt: string | null
}

export type EventAttendeeListResponse = {
  attendees: EventAttendeeResponse[]
}

export type TicketTypeSalesResponse = {
  ticketTypeId: number
  ticketTypeName: string
  soldCount: number
  revenueAmount: number
  revenueCurrency: string
}

export type EventResultsResponse = {
  eventId: number
  eventTitle: string
  totalRevenueAmount: number
  totalRevenueCurrency: string
  issuedCount: number
  checkedInCount: number
  noShowCount: number
  checkInRate: number
  ticketsSoldByType: TicketTypeSalesResponse[]
}

export type OwnedEventOverviewResponse = {
  eventId: number
  title: string
  status: string
  startsAt: string | null
  timeZoneId: string | null
  soldCount: number
  totalRevenueAmount: number
  totalRevenueCurrency: string
  checkedInCount: number
  issuedCount: number
}

export type StaffEventOverviewResponse = {
  eventId: number
  title: string
  status: string
  startsAt: string | null
  timeZoneId: string | null
  checkedInCount: number
  issuedCount: number
}

export type OrganizerAudienceOverviewResponse = {
  ownedEvents: OwnedEventOverviewResponse[]
  staffEvents: StaffEventOverviewResponse[]
}

export type SendAttendeeMessageRequest = {
  subject: string
  body: string
}

export type SendAttendeeMessageResponse = {
  acceptedRecipientCount: number
}

export type EventReminderSettingsRequest = {
  enabled: boolean
  leadTimeMinutes: number
}

export type EventReminderSettingsResponse = {
  eventId: number
  enabled: boolean
  leadTimeMinutes: number
  updatedAt: string
  lastSentAt: string | null
}

export function getOrganizerAudienceOverview(signal?: AbortSignal) {
  return apiClient.get<OrganizerAudienceOverviewResponse>('/api/organizer/audience/events', {
    signal,
    suppressErrorToast: true,
  })
}

export function getEventAttendees(eventId: number, signal?: AbortSignal) {
  return apiClient.get<EventAttendeeListResponse>(`/api/events/${eventId}/audience/attendees`, {
    signal,
    suppressErrorToast: true,
  })
}

export function exportEventAttendees(eventId: number, signal?: AbortSignal) {
  return apiClient.get<string>(`/api/events/${eventId}/audience/attendees.csv`, {
    signal,
    suppressErrorToast: true,
  })
}

export function getEventResults(eventId: number, signal?: AbortSignal) {
  return apiClient.get<EventResultsResponse>(`/api/events/${eventId}/results`, {
    signal,
    suppressErrorToast: true,
  })
}

export function sendAttendeeMessage(
  eventId: number,
  request: SendAttendeeMessageRequest,
  signal?: AbortSignal,
) {
  return apiClient.post<SendAttendeeMessageResponse>(
    `/api/events/${eventId}/audience/messages`,
    request,
    { signal, suppressErrorToast: true },
  )
}

export function setEventReminder(
  eventId: number,
  request: EventReminderSettingsRequest,
  signal?: AbortSignal,
) {
  return apiClient.put<EventReminderSettingsResponse>(
    `/api/events/${eventId}/audience/reminder`,
    request,
    { signal, suppressErrorToast: true },
  )
}
