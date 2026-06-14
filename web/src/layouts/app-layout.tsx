import { LogOutIcon } from 'lucide-react'
import { Outlet } from 'react-router-dom'

import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { useLogout } from '@/features/auth/use-logout'
import { useAuthStore } from '@/store/auth-store'

function UserMenu() {
  const status = useAuthStore((state) => state.status)
  const username = useAuthStore((state) => state.username)
  const email = useAuthStore((state) => state.email)
  const { logout, isPending } = useLogout()

  if (status !== 'authenticated') {
    return null
  }

  const displayName = username ?? 'Account'
  const initials =
    displayName
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('') || '?'

  return (
    <div className="flex items-center gap-3">
      <div className="hidden text-right sm:block">
        <p className="text-sm leading-none font-medium">{displayName}</p>
        {email ? <p className="text-muted-foreground mt-1 text-xs">{email}</p> : null}
      </div>
      <Avatar size="sm">
        <AvatarFallback>{initials}</AvatarFallback>
      </Avatar>
      <Button
        type="button"
        variant="outline"
        size="sm"
        disabled={isPending}
        onClick={() => logout()}
        aria-label="Log out"
      >
        {isPending ? <Spinner className="size-4" /> : <LogOutIcon className="size-4" />}
        <span className="hidden sm:inline">Log out</span>
      </Button>
    </div>
  )
}

export function AppLayout() {
  return (
    <div className="flex min-h-screen flex-col">
      <header
        className="border-border bg-card/60 supports-[backdrop-filter]:bg-card/40 sticky top-0 z-40 border-b backdrop-blur"
        style={{ viewTransitionName: 'site-header' }}
      >
        <div className="mx-auto flex h-14 w-full max-w-3xl items-center justify-between gap-4 px-4">
          <p className="text-sm font-semibold tracking-tight">Solution</p>
          <UserMenu />
        </div>
      </header>

      <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-8">
        <Outlet />
      </main>
    </div>
  )
}
