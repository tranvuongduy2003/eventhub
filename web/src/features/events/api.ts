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
  startsAt: string
  endsAt: string
  timeZoneId: string
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

export function createDraftEvent(request: CreateDraftEventRequest, signal?: AbortSignal) {
  return apiClient.post<DraftEventResponse>('/api/events', request, {
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
