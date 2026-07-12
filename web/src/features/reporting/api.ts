import { apiClient } from '@/lib/api'
import type { ApiJsonBody, ApiJsonResponse } from '@/lib/api'

export type EventAttendeeListResponse = ApiJsonResponse<
  '/api/events/{eventId}/audience/attendees',
  'get'
>
export type EventAttendeeResponse = EventAttendeeListResponse['attendees'][number]
export type EventResultsResponse = ApiJsonResponse<'/api/events/{eventId}/results', 'get'>
export type TicketTypeSalesResponse = EventResultsResponse['ticketsSoldByType'][number]
export type OrganizerAudienceOverviewResponse = ApiJsonResponse<
  '/api/organizer/audience/events',
  'get'
>
export type OwnedEventOverviewResponse = OrganizerAudienceOverviewResponse['ownedEvents'][number]
export type StaffEventOverviewResponse = OrganizerAudienceOverviewResponse['staffEvents'][number]
export type SendAttendeeMessageRequest = ApiJsonBody<
  '/api/events/{eventId}/audience/messages',
  'post'
>
export type SendAttendeeMessageResponse = ApiJsonResponse<
  '/api/events/{eventId}/audience/messages',
  'post',
  202
>
export type EventReminderSettingsRequest = ApiJsonBody<
  '/api/events/{eventId}/audience/reminder',
  'put'
>
export type EventReminderSettingsResponse = ApiJsonResponse<
  '/api/events/{eventId}/audience/reminder',
  'put'
>

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
