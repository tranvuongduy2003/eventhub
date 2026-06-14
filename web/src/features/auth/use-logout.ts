import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'

import { paths } from '@/app/paths'
import { useAuthStore } from '@/store/auth-store'
import { ApiError } from '@/types/api-problem'

import * as authApi from './api'
import { clearUserScopedQueries } from './clear-user-queries'

const logoutFailedMessage = 'Could not log out. Check your connection and try again.'

export function useLogout() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const clearSession = useAuthStore((state) => state.clearSession)

  const finalizeLogout = () => {
    clearSession()
    clearUserScopedQueries(queryClient)
    navigate(paths.login, { replace: true })
  }

  const mutation = useMutation({
    mutationFn: () => authApi.logout(),
    onSuccess: () => {
      finalizeLogout()
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 401) {
        finalizeLogout()
        return
      }

      toast.error(logoutFailedMessage)
    },
  })

  return {
    logout: mutation.mutate,
    isPending: mutation.isPending,
  }
}
