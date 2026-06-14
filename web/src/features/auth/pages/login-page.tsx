import { Link, useLocation } from 'react-router-dom'

import { paths } from '@/app/paths'
import { Alert, AlertDescription } from '@/components/ui/alert'
import type { LoginLocationState } from '@/types/auth'

import { LoginForm } from '../login-form'

export function LoginPage() {
  const location = useLocation()
  const locationState = location.state as LoginLocationState | null
  const sessionExpired = locationState?.reason === 'session-expired'

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold tracking-tight">Log in</h1>
        <p className="text-muted-foreground text-sm">Sign in to your account.</p>
      </div>

      {sessionExpired ? (
        <Alert>
          <AlertDescription>Your session has expired. Please log in again.</AlertDescription>
        </Alert>
      ) : null}

      <LoginForm />

      <p className="text-muted-foreground text-center text-sm">
        No account?{' '}
        <Link to={paths.register} className="text-foreground underline-offset-4 hover:underline">
          Register
        </Link>
      </p>
    </div>
  )
}
