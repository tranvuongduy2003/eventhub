import type { FieldPath, UseFormSetError } from 'react-hook-form'

import { ApiError } from '@/types/api-problem'
import type { RegisterFormValues } from '@/types/auth'

export const registerTransientErrorMessage = 'Something went wrong. Please try again.'

type RegisterFormField = FieldPath<RegisterFormValues> | 'root'

const fieldErrorMessages: Record<string, Partial<Record<keyof RegisterFormValues, string>>> = {
  USERNAME_TAKEN: {
    username: 'That username is already in use.',
  },
  EMAIL_TAKEN: {
    email: 'An account with this email already exists.',
  },
}

function setRootError(setError: UseFormSetError<RegisterFormValues>, message: string): void {
  setError('root' as RegisterFormField, { message })
}

export function applyRegisterApiError(
  error: unknown,
  setError: UseFormSetError<RegisterFormValues>,
): void {
  if (!(error instanceof ApiError)) {
    setRootError(setError, registerTransientErrorMessage)
    return
  }

  const code = error.problem.code
  if (code === 'INTERNAL_ERROR' || error.status >= 500) {
    setRootError(setError, registerTransientErrorMessage)
    return
  }

  if (code) {
    const mapped = fieldErrorMessages[code]
    if (mapped) {
      for (const [field, message] of Object.entries(mapped)) {
        setError(field as keyof RegisterFormValues, { message })
      }
      return
    }
  }

  const validationErrors = error.problem.errors
  if (!validationErrors) {
    return
  }

  for (const [field, messages] of Object.entries(validationErrors)) {
    setError(field as keyof RegisterFormValues, { message: messages.join(' ') })
  }
}
