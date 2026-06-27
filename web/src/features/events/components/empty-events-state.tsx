import { CalendarDays } from 'lucide-react'

type EmptyEventsStateProps = {
  isFiltered?: boolean
  onClearFilters?: () => void
}

export function EmptyEventsState({ isFiltered, onClearFilters }: EmptyEventsStateProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-4 py-16 text-center">
      <div className="bg-muted flex size-16 items-center justify-center rounded-full">
        <CalendarDays className="text-muted-foreground size-8" />
      </div>
      <div className="flex flex-col gap-2">
        <h2 className="text-xl font-semibold">
          {isFiltered ? 'No events found' : 'No upcoming events'}
        </h2>
        <p className="text-muted-foreground max-w-md text-sm">
          {isFiltered
            ? 'No events match your current filters. Try adjusting your search or clearing the filters.'
            : 'There are no published events scheduled at this time. Check back soon — organizers are always planning something new.'}
        </p>
      </div>
      {isFiltered && onClearFilters && (
        <button
          type="button"
          onClick={onClearFilters}
          className="border-input bg-background hover:bg-accent hover:text-accent-foreground inline-flex h-10 items-center rounded-md border px-4 text-sm font-medium transition-colors"
        >
          Clear filters
        </button>
      )}
    </div>
  )
}
