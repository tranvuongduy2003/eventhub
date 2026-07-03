import { apiClient } from '@/lib/api'

export type StartCheckoutLineRequest = {
  ticketTypeId: number
  quantity: number
}

export type StartCheckoutRequest = {
  lines: StartCheckoutLineRequest[]
}

export type StartCheckoutLineResponse = {
  ticketTypeId: number
  ticketTypeName: string
  quantity: number
  unitPriceAmount: number
  unitPriceCurrency: string
  lineTotalAmount: number
  lineTotalCurrency: string
}

export type StartCheckoutResponse = {
  eventSlug: string
  eventTitle: string
  totalAmount: number
  totalCurrency: string
  lines: StartCheckoutLineResponse[]
}

export function startCheckout(slug: string, request: StartCheckoutRequest, signal?: AbortSignal) {
  return apiClient.post<StartCheckoutResponse>(`/api/events/${slug}/checkout/start`, request, {
    signal,
    suppressErrorToast: true,
  })
}
