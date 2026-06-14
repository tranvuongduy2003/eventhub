import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useAuthStore } from '@/store/auth-store'

export function HomePage() {
  const userId = useAuthStore((state) => state.userId)
  const username = useAuthStore((state) => state.username)
  const email = useAuthStore((state) => state.email)

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold tracking-tight">Welcome</h1>
        <p className="text-muted-foreground text-sm">
          You are signed in. This shell uses cookie session auth against the boilerplate API.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Account</CardTitle>
          <CardDescription>Loaded from GET /api/auth/me after login.</CardDescription>
        </CardHeader>
        <CardContent>
          <dl className="grid gap-4 text-sm">
            <div>
              <dt className="text-muted-foreground">Username</dt>
              <dd className="font-medium">{username ?? '—'}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Email</dt>
              <dd className="font-medium">{email ?? '—'}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">User ID</dt>
              <dd className="font-mono text-xs">{userId ?? '—'}</dd>
            </div>
          </dl>
        </CardContent>
      </Card>
    </div>
  )
}
