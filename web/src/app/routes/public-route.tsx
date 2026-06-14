import { Navigate, Outlet } from 'react-router-dom'

import { paths } from '@/app/paths'
import { Skeleton } from '@/components/ui/skeleton'
import { useSession } from '@/hooks/use-session'
import { useAuthStore } from '@/store/auth-store'

export function PublicRoute() {
  const authStatus = useAuthStore((state) => state.status)
  const sessionQuery = useSession()

  const isBootstrappingSession =
    authStatus === 'unknown' && (sessionQuery.isPending || sessionQuery.isFetching)

  if (isBootstrappingSession) {
    return (
      <div className="flex min-h-screen items-center justify-center px-4">
        <Skeleton className="h-10 w-48" />
      </div>
    )
  }

  if (authStatus === 'authenticated') {
    return <Navigate to={paths.home} replace />
  }

  return <Outlet />
}
