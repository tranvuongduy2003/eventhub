import { CalendarDays, ScanLine, ShoppingCart, Ticket } from 'lucide-react'
import { Link } from 'react-router-dom'

import { paths } from '@/app/paths'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useAuthStore } from '@/store/auth-store'

const featureLinks = [
  {
    path: paths.events,
    label: 'Events',
    description: 'Create and manage your events, ticket types, and pricing.',
    icon: CalendarDays,
  },
  {
    path: paths.checkout,
    label: 'Checkout',
    description: 'Guest purchase flow with transparent, all-in pricing.',
    icon: ShoppingCart,
  },
  {
    path: paths.tickets,
    label: 'Tickets',
    description: 'Deliver tickets and QR codes to your attendees.',
    icon: Ticket,
  },
  {
    path: paths.checkIn,
    label: 'Check-in',
    description: 'Scan tickets at the door and track attendance.',
    icon: ScanLine,
  },
] as const

export function HomePage() {
  const userId = useAuthStore((state) => state.userId)
  const displayName = useAuthStore((state) => state.displayName)
  const email = useAuthStore((state) => state.email)

  return (
    <div className="flex flex-col gap-8">
      <section className="flex flex-col gap-2">
        <p className="text-primary text-sm font-medium">Organizer dashboard</p>
        <h1 className="text-foreground text-3xl font-bold tracking-tight">
          Welcome back{displayName ? `, ${displayName}` : ''}
        </h1>
        <p className="text-muted-foreground max-w-2xl text-base">
          Manage events, sell tickets, and check in attendees — all in one place.
        </p>
      </section>

      <section className="grid gap-4 sm:grid-cols-3">
        <Card className="shadow-sm">
          <CardHeader className="pb-2">
            <CardDescription>Account status</CardDescription>
            <CardTitle className="text-lg">Active</CardTitle>
          </CardHeader>
          <CardContent>
            <span className="bg-success-muted inline-flex rounded-full px-2.5 py-0.5 text-xs font-medium">
              Verified organizer
            </span>
          </CardContent>
        </Card>

        <Card className="shadow-sm">
          <CardHeader className="pb-2">
            <CardDescription>Email</CardDescription>
            <CardTitle className="truncate text-lg font-medium">{email ?? '—'}</CardTitle>
          </CardHeader>
          <CardContent className="text-muted-foreground text-sm">
            Used for order confirmations and event updates.
          </CardContent>
        </Card>

        <Card className="shadow-sm">
          <CardHeader className="pb-2">
            <CardDescription>User ID</CardDescription>
            <CardTitle className="font-mono text-sm font-medium">{userId ?? '—'}</CardTitle>
          </CardHeader>
          <CardContent className="text-muted-foreground text-sm">
            Your unique account reference.
          </CardContent>
        </Card>
      </section>

      <section className="flex flex-col gap-4">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">Quick access</h2>
          <p className="text-muted-foreground mt-1 text-sm">
            Jump into the areas you need to run your events.
          </p>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          {featureLinks.map((feature) => (
            <Link key={feature.path} to={feature.path} className="group block">
              <Card className="surface-card h-full">
                <CardHeader>
                  <div className="bg-primary/10 text-primary mb-2 flex size-10 items-center justify-center rounded-lg">
                    <feature.icon className="size-5" aria-hidden />
                  </div>
                  <CardTitle className="group-hover:text-primary text-lg transition-colors">
                    {feature.label}
                  </CardTitle>
                  <CardDescription>{feature.description}</CardDescription>
                </CardHeader>
              </Card>
            </Link>
          ))}
        </div>
      </section>
    </div>
  )
}
