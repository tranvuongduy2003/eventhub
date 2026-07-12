import { buildEventMonitoringHubUrl, buildEventMonitoringWebSocketUrl } from '@/lib/env'

import type { EventResultsResponse, TicketTypeSalesResponse } from './api'

const recordSeparator = String.fromCharCode(0x1e)

export type EventSalesInventoryUpdatedMessage = {
  eventId: number
  eventTitle: string
  totalRevenueAmount: number
  totalRevenueCurrency: string
  issuedCount: number
  ticketTypes: TicketTypeSalesResponse[]
  occurredAt: string
}

export type EventSalesInventorySubscription = {
  close: () => void
}

type SubscriptionOptions = {
  onUpdate: (message: EventSalesInventoryUpdatedMessage) => void
  onReconnect: () => Promise<void>
}

type NegotiateResponse = {
  connectionToken?: string
}

export function subscribeToEventSalesInventory(
  eventId: number,
  options: SubscriptionOptions,
): EventSalesInventorySubscription {
  let socket: WebSocket | null = null
  let reconnectTimeout: number | undefined
  let closed = false
  let acceptingUpdates = false
  let pendingUpdate: EventSalesInventoryUpdatedMessage | undefined

  function send(message: unknown) {
    socket?.send(`${JSON.stringify(message)}${recordSeparator}`)
  }

  async function connect() {
    acceptingUpdates = false
    let connectionToken: string | undefined
    try {
      connectionToken = await negotiate()
    } catch {
      scheduleReconnect()
      return
    }

    if (closed) return

    socket = new WebSocket(buildEventMonitoringWebSocketUrl(connectionToken))

    socket.addEventListener('open', () => {
      void options.onReconnect().then(() => {
        if (!closed) {
          acceptingUpdates = true
          if (pendingUpdate) {
            options.onUpdate(pendingUpdate)
            pendingUpdate = undefined
          }
        }
      })
      send({ protocol: 'json', version: 1 })
      send({ type: 1, target: 'JoinEventSalesInventory', arguments: [eventId] })
    })

    socket.addEventListener('message', (event) => {
      if (typeof event.data !== 'string') return

      for (const rawMessage of event.data.split(recordSeparator)) {
        if (!rawMessage) continue

        let message: {
          type?: number
          target?: string
          arguments?: unknown[]
        }

        try {
          message = JSON.parse(rawMessage) as typeof message
        } catch {
          continue
        }

        if (message.type !== 1 || message.target !== 'eventSalesInventoryUpdated') {
          continue
        }

        const [payload] = message.arguments ?? []
        if (!isSalesInventoryMessage(payload)) {
          continue
        }

        if (acceptingUpdates) {
          options.onUpdate(payload)
        } else {
          pendingUpdate = payload
        }
      }
    })

    socket.addEventListener('close', () => {
      scheduleReconnect()
    })
  }

  connect()

  return {
    close: () => {
      closed = true
      pendingUpdate = undefined
      if (reconnectTimeout !== undefined) {
        window.clearTimeout(reconnectTimeout)
      }
      socket?.close()
    },
  }

  function scheduleReconnect() {
    if (!closed) {
      acceptingUpdates = false
      reconnectTimeout = window.setTimeout(connect, 2000)
    }
  }
}

async function negotiate(): Promise<string | undefined> {
  const response = await fetch(`${buildEventMonitoringHubUrl()}/negotiate?negotiateVersion=1`, {
    method: 'POST',
    credentials: 'include',
  })

  if (!response.ok) {
    throw new Error('Event monitoring negotiation failed.')
  }

  const body = (await response.json()) as NegotiateResponse
  return body.connectionToken
}

export function applySalesInventoryUpdate(
  current: EventResultsResponse | undefined,
  update: EventSalesInventoryUpdatedMessage,
): EventResultsResponse | undefined {
  if (!current || current.eventId !== update.eventId) {
    return current
  }

  const checkedInCount = numeric(current.checkedInCount)

  return {
    ...current,
    eventTitle: update.eventTitle,
    totalRevenueAmount: update.totalRevenueAmount,
    totalRevenueCurrency: update.totalRevenueCurrency,
    issuedCount: update.issuedCount,
    noShowCount: Math.max(0, update.issuedCount - checkedInCount),
    checkInRate: update.issuedCount === 0 ? 0 : checkedInCount / update.issuedCount,
    ticketsSoldByType: update.ticketTypes,
  }
}

function numeric(value: string | number) {
  return typeof value === 'number' ? value : Number(value)
}

function isSalesInventoryMessage(value: unknown): value is EventSalesInventoryUpdatedMessage {
  if (!value || typeof value !== 'object') return false

  const message = value as Partial<EventSalesInventoryUpdatedMessage>

  return (
    typeof message.eventId === 'number' &&
    typeof message.eventTitle === 'string' &&
    typeof message.totalRevenueAmount === 'number' &&
    typeof message.totalRevenueCurrency === 'string' &&
    typeof message.issuedCount === 'number' &&
    Array.isArray(message.ticketTypes)
  )
}
