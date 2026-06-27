import { CalendarDays } from 'lucide-react'

export function EmptyEventsState() {
  return (
    <div className="flex flex-col items-center justify-center gap-4 py-16 text-center">
      <div className="bg-muted flex size-16 items-center justify-center rounded-full">
        <CalendarDays className="text-muted-foreground size-8" />
      </div>
      <div className="flex flex-col gap-2">
        <h2 className="text-xl font-semibold">No upcoming events</h2>
        <p className="text-muted-foreground max-w-md text-sm">
          There are no published events scheduled at this time. Check back soon — organizers are
          always planning something new.
        </p>
      </div>
    </div>
  )
}
