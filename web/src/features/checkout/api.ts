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
  eventId: number
  eventSlug: string
  eventTitle: string
  totalAmount: number
  totalCurrency: string
  lines: StartCheckoutLineResponse[]
}

export type PlaceOrderLineRequest = {
  ticketTypeId: number
  quantity: number
}

export type PlaceOrderRequest = {
  contactName: string
  contactEmail: string
  lines: PlaceOrderLineRequest[]
}

export type OrderLineResponse = {
  orderLineId: number
  ticketTypeId: number
  ticketTypeName: string
  quantity: number
  unitPriceAmount: number
  unitPriceCurrency: string
  lineTotalAmount: number
  lineTotalCurrency: string
}

export type PlaceOrderResponse = {
  orderId: number
  status: string
  totalAmount: number
  totalCurrency: string
  paymentId: number | null
  placedAt: string
  confirmedAt: string | null
  lines: OrderLineResponse[]
  discountCode: string | null
  discountAmount: number | null
  ticketUrl: string | null
}

export type OrderStatusResponse = PlaceOrderResponse

export type StartPaymentRequest = {
  successUrl: string
  cancelUrl: string
}

export type StartPaymentResponse = {
  paymentId: number
  orderId: number
  amount: number
  currency: string
  providerReference: string
  redirectUrl: string
}

export function startCheckout(slug: string, request: StartCheckoutRequest, signal?: AbortSignal) {
  return apiClient.post<StartCheckoutResponse>(`/api/events/${slug}/checkout/start`, request, {
    signal,
    suppressErrorToast: true,
  })
}

export function placeOrder(eventId: number, request: PlaceOrderRequest, signal?: AbortSignal) {
  return apiClient.post<PlaceOrderResponse>(`/api/events/${eventId}/orders`, request, {
    signal,
    suppressErrorToast: true,
  })
}

export function getOrderStatus(orderId: number, signal?: AbortSignal) {
  return apiClient.get<OrderStatusResponse>(`/api/orders/${orderId}`, {
    signal,
    suppressErrorToast: true,
  })
}

export function startPayment(orderId: number, request: StartPaymentRequest, signal?: AbortSignal) {
  return apiClient.post<StartPaymentResponse>(`/api/orders/${orderId}/payments`, request, {
    signal,
    suppressErrorToast: true,
  })
}
