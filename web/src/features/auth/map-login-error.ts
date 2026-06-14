import type { UseFormSetError } from 'react-hook-form'

import { ApiError } from '@/types/api-problem'
import type { LoginFormValues } from '@/types/auth'

export const loginTransientErrorMessage = 'Something went wrong. Please try again.'

export const loginInvalidCredentialsMessage = 'Email or password is incorrect.'

export const loginCookiesRequiredMessage =
  'Cookies are required to stay signed in. Enable cookies for this site and try again.'

export function applyLoginApiError(
  error: unknown,
  setError: UseFormSetError<LoginFormValues>,
): void {
  if (!(error instanceof ApiError)) {
    setError('root', { message: loginTransientErrorMessage })
    return
  }

  if (error.problem.code === 'INTERNAL_ERROR' || error.status >= 500) {
    setError('root', { message: loginTransientErrorMessage })
    return
  }

  if (error.problem.code === 'INVALID_CREDENTIALS' || error.status === 401) {
    setError('root', { message: loginInvalidCredentialsMessage })
    return
  }

  const validationErrors = error.problem.errors
  if (!validationErrors) {
    return
  }

  for (const [field, messages] of Object.entries(validationErrors)) {
    setError(field as keyof LoginFormValues, { message: messages.join(' ') })
  }
}
