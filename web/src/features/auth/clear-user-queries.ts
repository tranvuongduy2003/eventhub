import type { QueryClient } from '@tanstack/react-query'

export function clearUserScopedQueries(queryClient: QueryClient) {
  queryClient.removeQueries({ queryKey: ['auth', 'session'] })
}
