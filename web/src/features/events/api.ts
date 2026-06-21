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

export function createDraftEvent(request: CreateDraftEventRequest, signal?: AbortSignal) {
  return apiClient.post<DraftEventResponse>('/api/events', request, {
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
