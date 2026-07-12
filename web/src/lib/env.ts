const apiUrl = import.meta.env.VITE_API_URL
const defaultSymbol = import.meta.env.VITE_DEFAULT_SYMBOL

function resolveApiUrl(): string {
  if (typeof apiUrl === 'string' && apiUrl.length > 0) {
    return apiUrl.replace(/\/$/, '')
  }

  return 'https://localhost:8000'
}

export const env = {
  apiUrl: resolveApiUrl(),
  defaultSymbol:
    typeof defaultSymbol === 'string' && defaultSymbol.length > 0 ? defaultSymbol : 'AAPL',
  simulationHubPath: '/hubs/simulation',
  eventMonitoringHubPath: '/hubs/events',
} as const

export function buildApiUrl(path: string): string {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return `${env.apiUrl}${normalizedPath}`
}

export function buildSimulationHubUrl(): string {
  return `${env.apiUrl}${env.simulationHubPath}`
}

export function buildEventMonitoringHubUrl(): string {
  return `${env.apiUrl}${env.eventMonitoringHubPath}`
}

export function buildEventMonitoringWebSocketUrl(connectionToken?: string): string {
  const url = new URL(`${env.apiUrl}${env.eventMonitoringHubPath}`)
  url.protocol = url.protocol === 'https:' ? 'wss:' : 'ws:'
  if (connectionToken) {
    url.searchParams.set('id', connectionToken)
  }
  return url.toString()
}
