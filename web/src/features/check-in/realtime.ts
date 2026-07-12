import { buildEventMonitoringHubUrl, buildEventMonitoringWebSocketUrl } from '@/lib/env'

const recordSeparator = String.fromCharCode(0x1e)

export type EventCheckInUpdatedMessage = {
  eventId: number
  checkedIn: number
  totalIssued: number
  checkInRate: number
  occurredAt: string
}

export type EventCheckInSubscription = {
  close: () => void
}

type SubscriptionOptions = {
  onUpdate: (message: EventCheckInUpdatedMessage) => void
  onReconnect: () => Promise<void>
}

type NegotiateResponse = {
  connectionToken?: string
}

export function subscribeToEventCheckIn(
  eventId: number,
  options: SubscriptionOptions,
): EventCheckInSubscription {
  let socket: WebSocket | null = null
  let reconnectTimeout: number | undefined
  let closed = false

  function send(message: unknown) {
    socket?.send(`${JSON.stringify(message)}${recordSeparator}`)
  }

  async function connect() {
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
      void options.onReconnect()
      send({ protocol: 'json', version: 1 })
      send({ type: 1, target: 'JoinEventCheckIn', arguments: [eventId] })
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

        if (message.type !== 1 || message.target !== 'eventCheckInUpdated') {
          continue
        }

        const [payload] = message.arguments ?? []
        if (isCheckInMessage(payload)) {
          options.onUpdate(payload)
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
      if (reconnectTimeout !== undefined) {
        window.clearTimeout(reconnectTimeout)
      }
      socket?.close()
    },
  }

  function scheduleReconnect() {
    if (!closed) {
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

function isCheckInMessage(value: unknown): value is EventCheckInUpdatedMessage {
  if (!value || typeof value !== 'object') return false

  const message = value as Partial<EventCheckInUpdatedMessage>

  return (
    typeof message.eventId === 'number' &&
    typeof message.checkedIn === 'number' &&
    typeof message.totalIssued === 'number' &&
    typeof message.checkInRate === 'number'
  )
}
