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

export function createDraftEvent(request: CreateDraftEventRequest, signal?: AbortSignal) {
  return apiClient.post<DraftEventResponse>('/api/events', request, {
    signal,
    suppressErrorToast: true,
  })
}
