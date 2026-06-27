import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useEffect } from 'react'
import { HelmetProvider } from 'react-helmet-async'
import { RouterProvider } from 'react-router-dom'

import { paths } from '@/app/paths'
import { router } from '@/app/router'
import { Toaster } from '@/components/ui/sonner'
import { useAuthStore } from '@/store/auth-store'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
})

function UnauthorizedListener() {
  const clearSession = useAuthStore((state) => state.clearSession)

  useEffect(() => {
    const handleUnauthorized = () => {
      if (useAuthStore.getState().status !== 'authenticated') {
        return
      }

      router.navigate(paths.login, {
        replace: true,
        state: { reason: 'session-expired' },
      })
      clearSession()
      queryClient.clear()
    }

    window.addEventListener('api:unauthorized', handleUnauthorized)
    return () => window.removeEventListener('api:unauthorized', handleUnauthorized)
  }, [clearSession])

  return null
}

export function AppProviders() {
  return (
    <HelmetProvider>
      <QueryClientProvider client={queryClient}>
        <UnauthorizedListener />
        <RouterProvider router={router} />
        <Toaster richColors closeButton />
      </QueryClientProvider>
    </HelmetProvider>
  )
}
