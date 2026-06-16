import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRef, type FormEvent } from 'react'
import { useForm } from 'react-hook-form'
import { useLocation, useNavigate } from 'react-router-dom'

import { paths } from '@/app/paths'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Field, FieldError, FieldGroup, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'
import { useAuthStore } from '@/store/auth-store'
import { ApiError } from '@/types/api-problem'
import { loginFormSchema, type LoginFormValues, type LoginLocationState } from '@/types/auth'

import * as authApi from './api'
import { clearUserScopedQueries } from './clear-user-queries'
import {
  applyLoginApiError,
  loginCookiesRequiredMessage,
  loginTransientErrorMessage,
} from './map-login-error'

function areCookiesEnabled(): boolean {
  return typeof navigator === 'undefined' || navigator.cookieEnabled
}

export function LoginForm() {
  const navigate = useNavigate()
  const location = useLocation()
  const queryClient = useQueryClient()
  const setSession = useAuthStore((state) => state.setSession)
  const submittingRef = useRef(false)

  const form = useForm<LoginFormValues>({
    resolver: zodResolver(loginFormSchema),
    mode: 'onBlur',
    reValidateMode: 'onChange',
    defaultValues: {
      email: '',
      password: '',
    },
  })

  const loginMutation = useMutation({
    mutationFn: (values: LoginFormValues) =>
      authApi.login({
        email: values.email,
        password: values.password,
      }),
    onSuccess: async (response) => {
      clearUserScopedQueries(queryClient)

      try {
        await queryClient.fetchQuery({
          queryKey: ['auth', 'session'],
          queryFn: ({ signal }) => authApi.getCurrentUser(signal),
        })
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          form.setError('root', { message: loginCookiesRequiredMessage })
          return
        }

        form.setError('root', { message: loginTransientErrorMessage })
        return
      }

      setSession({
        userId: response.userId,
        displayName: response.displayName,
        email: response.email,
        role: response.role,
      })

      const from = (location.state as LoginLocationState | null)?.from
      navigate(typeof from === 'string' ? from : paths.home, { replace: true })
    },
    onError: (error) => {
      form.setValue('password', '')
      applyLoginApiError(error, form.setError)
    },
    onSettled: () => {
      submittingRef.current = false
    },
  })

  const rootError = form.formState.errors.root

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (submittingRef.current || loginMutation.isPending) {
      return
    }

    void form.handleSubmit((values) => {
      submittingRef.current = true
      form.clearErrors('root')

      if (!areCookiesEnabled()) {
        form.setError('root', { message: loginCookiesRequiredMessage })
        submittingRef.current = false
        return
      }

      loginMutation.mutate(values)
    })(event)
  }

  return (
    <form className="flex flex-col gap-6" onSubmit={onSubmit} noValidate>
      {rootError?.message ? (
        <Alert variant="destructive">
          <AlertDescription>{rootError.message}</AlertDescription>
        </Alert>
      ) : null}

      <FieldGroup>
        <Field data-invalid={!!form.formState.errors.email}>
          <FieldLabel htmlFor="login-email">Email</FieldLabel>
          <Input
            id="login-email"
            type="email"
            autoComplete="email"
            disabled={loginMutation.isPending}
            {...form.register('email')}
          />
          <FieldError errors={[form.formState.errors.email]} />
        </Field>

        <Field data-invalid={!!form.formState.errors.password}>
          <FieldLabel htmlFor="login-password">Password</FieldLabel>
          <Input
            id="login-password"
            type="password"
            autoComplete="current-password"
            disabled={loginMutation.isPending}
            {...form.register('password')}
          />
          <FieldError errors={[form.formState.errors.password]} />
        </Field>
      </FieldGroup>

      <Button type="submit" className="w-full" disabled={loginMutation.isPending}>
        {loginMutation.isPending ? (
          <>
            <Spinner className="mr-2" />
            Signing in…
          </>
        ) : (
          'Log in'
        )}
      </Button>
    </form>
  )
}
