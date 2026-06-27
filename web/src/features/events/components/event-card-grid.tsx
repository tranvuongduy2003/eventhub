import { Spinner } from '@/components/ui/spinner'
import { EventCard } from '@/features/events/components/event-card'
import type { PublicEventListItemResponse } from '@/features/events/api'

type EventCardGridProps = {
  events: PublicEventListItemResponse[]
  hasNextPage: boolean
  isFetchingNextPage: boolean
  onLoadMore: () => void
}

export function EventCardGrid({
  events,
  hasNextPage,
  isFetchingNextPage,
  onLoadMore,
}: EventCardGridProps) {
  return (
    <div className="flex flex-col gap-8">
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {events.map((event) => (
          <EventCard key={event.slug} event={event} />
        ))}
      </div>

      {hasNextPage && (
        <div className="flex justify-center">
          <button
            type="button"
            onClick={onLoadMore}
            disabled={isFetchingNextPage}
            className="border-input bg-background hover:bg-accent hover:text-accent-foreground inline-flex h-10 items-center gap-2 rounded-md border px-6 text-sm font-medium transition-colors disabled:pointer-events-none disabled:opacity-50"
          >
            {isFetchingNextPage && <Spinner className="size-4" />}
            Load more
          </button>
        </div>
      )}
    </div>
  )
}
