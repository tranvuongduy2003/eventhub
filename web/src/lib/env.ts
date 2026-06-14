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
} as const

export function buildApiUrl(path: string): string {
  const normalizedPath = path.startsWith('/') ? path : `/${path}`
  return `${env.apiUrl}${normalizedPath}`
}

export function buildSimulationHubUrl(): string {
  return `${env.apiUrl}${env.simulationHubPath}`
}
