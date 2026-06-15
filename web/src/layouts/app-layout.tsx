import { LogOutIcon, TicketIcon } from 'lucide-react'
import { NavLink, Outlet } from 'react-router-dom'

import { paths } from '@/app/paths'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { useLogout } from '@/features/auth/use-logout'
import { useAuthStore } from '@/store/auth-store'
import { cn } from '@/lib/utils'

const navItems = [
  { path: paths.home, label: 'Dashboard', end: true },
  { path: paths.events, label: 'Events', end: false },
  { path: paths.tickets, label: 'Tickets', end: false },
  { path: paths.checkout, label: 'Checkout', end: false },
  { path: paths.checkIn, label: 'Check-in', end: false },
] as const

function UserMenu() {
  const status = useAuthStore((state) => state.status)
  const displayName = useAuthStore((state) => state.displayName)
  const email = useAuthStore((state) => state.email)
  const { logout, isPending } = useLogout()

  if (status !== 'authenticated') {
    return null
  }

  const menuLabel = displayName ?? 'Account'
  const initials =
    menuLabel
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('') || '?'

  return (
    <div className="flex items-center gap-3">
      <div className="hidden text-right sm:block">
        <p className="text-sm leading-none font-medium">{menuLabel}</p>
        {email ? <p className="text-muted-foreground mt-1 text-xs">{email}</p> : null}
      </div>
      <Avatar size="sm">
        <AvatarFallback className="bg-primary/10 text-primary">{initials}</AvatarFallback>
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
        className="border-border bg-card sticky top-0 z-40 border-b shadow-sm"
        style={{ viewTransitionName: 'site-header' }}
      >
        <div className="store-container flex h-16 items-center justify-between gap-6">
          <div className="flex min-w-0 items-center gap-8">
            <NavLink
              to={paths.home}
              className="text-primary flex shrink-0 items-center gap-2 font-semibold tracking-tight"
            >
              <span className="bg-primary text-primary-foreground flex size-8 items-center justify-center rounded-lg">
                <TicketIcon className="size-4" aria-hidden />
              </span>
              <span className="text-foreground hidden text-base sm:inline">EventHub</span>
            </NavLink>

            <nav className="hidden items-center gap-1 md:flex" aria-label="Main navigation">
              {navItems.map((item) => (
                <NavLink
                  key={item.path}
                  to={item.path}
                  end={item.end}
                  className={({ isActive }) =>
                    cn(
                      'rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                      isActive
                        ? 'bg-primary/10 text-primary'
                        : 'text-muted-foreground hover:bg-muted hover:text-foreground',
                    )
                  }
                >
                  {item.label}
                </NavLink>
              ))}
            </nav>
          </div>

          <UserMenu />
        </div>
      </header>

      <main className="store-container flex-1 py-8 lg:py-10">
        <Outlet />
      </main>

      <footer className="border-border bg-card mt-auto border-t">
        <div className="store-container text-muted-foreground flex h-14 items-center justify-between text-xs">
          <p>EventHub — Event ticketing platform</p>
          <p>All prices include fees</p>
        </div>
      </footer>
    </div>
  )
}
