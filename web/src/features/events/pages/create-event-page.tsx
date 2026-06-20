import { CreateEventForm } from '../create-event-form'

export function CreateEventPage() {
  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <p className="text-primary text-sm font-medium">Organizer tools</p>
        <h1 className="text-2xl font-bold tracking-tight">Create event</h1>
        <p className="text-muted-foreground max-w-2xl text-sm">
          Set up your event with the core details. You can add ticket types and publish later.
        </p>
      </div>

      <div className="max-w-xl">
        <CreateEventForm />
      </div>
    </div>
  )
}
