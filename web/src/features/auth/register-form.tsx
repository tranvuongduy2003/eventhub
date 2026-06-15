import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRef, type FormEvent } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate } from 'react-router-dom'

import { paths } from '@/app/paths'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'
import { useAuthStore } from '@/store/auth-store'
import { registerFormSchema, type RegisterFormValues } from '@/types/auth'

import * as authApi from './api'
import { clearUserScopedQueries } from './clear-user-queries'
import { applyRegisterApiError } from './map-register-error'

export function RegisterForm() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const setSession = useAuthStore((state) => state.setSession)
  const submittingRef = useRef(false)

  const form = useForm<RegisterFormValues>({
    resolver: zodResolver(registerFormSchema),
    mode: 'onBlur',
    reValidateMode: 'onChange',
    defaultValues: {
      displayName: '',
      email: '',
      password: '',
      confirmPassword: '',
    },
  })

  const registerMutation = useMutation({
    mutationFn: (values: RegisterFormValues) =>
      authApi.register({
        displayName: values.displayName,
        email: values.email,
        password: values.password,
      }),
    onSuccess: async (response) => {
      clearUserScopedQueries(queryClient)

      setSession({
        userId: response.userId,
        displayName: response.displayName,
        email: response.email,
      })

      try {
        await queryClient.fetchQuery({
          queryKey: ['auth', 'session'],
          queryFn: ({ signal }) => authApi.getCurrentUser(signal),
        })
      } catch {
        // Account created; cookie verification failed — user can sign in manually (EC-07).
      }

      queryClient.setQueryData(['auth', 'session'], {
        userId: response.userId,
        displayName: response.displayName,
        email: response.email,
      } satisfies authApi.LoginUserResponse)

      navigate(paths.home, { replace: true })
    },
    onError: (error) => {
      form.setValue('password', '')
      form.setValue('confirmPassword', '')
      applyRegisterApiError(error, form.setError)
    },
    onSettled: () => {
      submittingRef.current = false
    },
  })

  const rootError = form.formState.errors.root

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (submittingRef.current || registerMutation.isPending) {
      return
    }

    void form.handleSubmit((values) => {
      submittingRef.current = true
      form.clearErrors('root')
      registerMutation.mutate(values)
    })(event)
  }

  return (
    <form className="flex flex-col gap-6" onSubmit={onSubmit} noValidate>
      <p className="text-muted-foreground text-sm">
        Create an account to access the boilerplate application shell.
      </p>

      {rootError?.message ? (
        <Alert variant="destructive">
          <AlertDescription>{rootError.message}</AlertDescription>
        </Alert>
      ) : null}

      <FieldGroup>
        <Field data-invalid={!!form.formState.errors.displayName}>
          <FieldLabel htmlFor="register-display-name">Username</FieldLabel>
          <Input
            id="register-display-name"
            autoComplete="name"
            disabled={registerMutation.isPending}
            {...form.register('displayName')}
          />
          <FieldError errors={[form.formState.errors.displayName]} />
        </Field>

        <Field data-invalid={!!form.formState.errors.email}>
          <FieldLabel htmlFor="register-email">Email</FieldLabel>
          <Input
            id="register-email"
            type="email"
            autoComplete="email"
            disabled={registerMutation.isPending}
            {...form.register('email')}
          />
          <FieldError errors={[form.formState.errors.email]} />
        </Field>

        <Field data-invalid={!!form.formState.errors.password}>
          <FieldLabel htmlFor="register-password">Password</FieldLabel>
          <Input
            id="register-password"
            type="password"
            autoComplete="new-password"
            disabled={registerMutation.isPending}
            {...form.register('password')}
          />
          <FieldDescription>
            8+ characters, including a letter, number, and special character.
          </FieldDescription>
          <FieldError errors={[form.formState.errors.password]} />
        </Field>

        <Field data-invalid={!!form.formState.errors.confirmPassword}>
          <FieldLabel htmlFor="register-confirm-password">Confirm password</FieldLabel>
          <Input
            id="register-confirm-password"
            type="password"
            autoComplete="new-password"
            disabled={registerMutation.isPending}
            {...form.register('confirmPassword')}
          />
          <FieldError errors={[form.formState.errors.confirmPassword]} />
        </Field>
      </FieldGroup>

      <Button type="submit" className="w-full" disabled={registerMutation.isPending}>
        {registerMutation.isPending ? (
          <>
            <Spinner className="mr-2" />
            Creating account…
          </>
        ) : (
          'Create account'
        )}
      </Button>
    </form>
  )
}
