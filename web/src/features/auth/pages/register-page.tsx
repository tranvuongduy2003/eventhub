import { Link } from 'react-router-dom'

import { paths } from '@/app/paths'

import { RegisterForm } from '../register-form'

export function RegisterPage() {
  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold tracking-tight">Create account</h1>
        <p className="text-muted-foreground text-sm">Create an account to get started.</p>
      </div>

      <RegisterForm />

      <p className="text-muted-foreground text-center text-sm">
        Already have an account?{' '}
        <Link to={paths.login} className="text-foreground underline-offset-4 hover:underline">
          Log in
        </Link>
      </p>
    </div>
  )
}
