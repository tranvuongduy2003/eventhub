import { CalendarDays } from 'lucide-react'
import { Link } from 'react-router-dom'

import { paths } from '@/app/paths'
import { buttonVariants } from '@/components/ui/button-variants'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'

export function EventsPlaceholderPage() {
  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <p className="text-primary text-sm font-medium">Organizer tools</p>
        <h1 className="text-2xl font-bold tracking-tight">Events</h1>
        <p className="text-muted-foreground max-w-2xl text-sm">
          Create event listings, set ticket types, and publish when you are ready to sell.
        </p>
      </div>

      <Link
        to={paths.createEvent}
        className={buttonVariants({ variant: 'default', className: 'w-fit' })}
      >
        Create event
      </Link>

      <Card className="shadow-sm">
        <CardHeader>
          <div className="bg-primary/10 text-primary mb-2 flex size-10 items-center justify-center rounded-lg">
            <CalendarDays className="size-5" aria-hidden />
          </div>
          <CardTitle>Coming soon</CardTitle>
          <CardDescription>
            Organizer event management (EP-2) will live here — create drafts, ticket types, and
            publish.
          </CardDescription>
        </CardHeader>
        <CardContent className="text-muted-foreground text-sm">
          You will be able to manage your catalog of events from a single dashboard, similar to an
          e-commerce product listing.
        </CardContent>
      </Card>
    </div>
  )
}
