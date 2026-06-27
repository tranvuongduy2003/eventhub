import { useCallback, useMemo } from 'react'
import { useInfiniteQuery } from '@tanstack/react-query'
import { TicketIcon } from 'lucide-react'
import { Link, useSearchParams } from 'react-router-dom'

import { paths } from '@/app/paths'
import { EventCardGrid } from '@/features/events/components/event-card-grid'
import { EmptyEventsState } from '@/features/events/components/empty-events-state'
import { EventSearchFilters } from '@/features/events/components/event-search-filters'
import { getPublicEvents, type EventFilters } from '@/features/events/api'
import { useDebouncedValue } from '@/hooks/use-debounced-value'

const PAGE_SIZE = 24

export function PublicEventsPage() {
  const [searchParams, setSearchParams] = useSearchParams()

  const searchValue = searchParams.get('q') ?? ''
  const dateValue = searchParams.get('date') ?? ''
  const locationValue = searchParams.get('location') ?? ''

  const debouncedSearch = useDebouncedValue(searchValue, 300)

  const filters: EventFilters = useMemo(
    () => ({
      q: debouncedSearch || undefined,
      date: dateValue || undefined,
      location: locationValue || undefined,
    }),
    [debouncedSearch, dateValue, locationValue],
  )

  const hasActiveFilters = !!(filters.q || filters.date || filters.location)

  const { data, hasNextPage, isFetchingNextPage, fetchNextPage, isLoading, isError } =
    useInfiniteQuery({
      queryKey: ['events', 'public', filters],
      queryFn: ({ pageParam, signal }) => getPublicEvents(pageParam, PAGE_SIZE, filters, signal),
      initialPageParam: 1,
      getNextPageParam: (lastPage) => {
        const totalPages = Math.ceil(lastPage.totalCount / PAGE_SIZE)
        return lastPage.page < totalPages ? lastPage.page + 1 : undefined
      },
    })

  const events = data?.pages.flatMap((page) => page.items) ?? []

  const updateParam = useCallback(
    (key: string, value: string) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev)
          if (value) {
            next.set(key, value)
          } else {
            next.delete(key)
          }
          return next
        },
        { replace: true },
      )
    },
    [setSearchParams],
  )

  const handleClear = useCallback(() => {
    setSearchParams({}, { replace: true })
  }, [setSearchParams])

  return (
    <div className="flex min-h-screen flex-col">
      <header className="border-border bg-card sticky top-0 z-40 border-b shadow-sm">
        <div className="store-container flex h-16 items-center justify-between gap-6">
          <Link
            to={paths.events}
            className="text-primary flex shrink-0 items-center gap-2 font-semibold tracking-tight"
          >
            <span className="bg-primary text-primary-foreground flex size-8 items-center justify-center rounded-lg">
              <TicketIcon className="size-4" aria-hidden />
            </span>
            <span className="text-foreground hidden text-base sm:inline">EventHub</span>
          </Link>

          <nav className="flex items-center gap-2">
            <Link
              to={paths.login}
              className="text-muted-foreground hover:text-foreground text-sm font-medium transition-colors"
            >
              Log in
            </Link>
          </nav>
        </div>
      </header>

      <main className="store-container flex-1 py-8 lg:py-10">
        <div className="flex flex-col gap-6">
          <div className="flex flex-col gap-2">
            <h1 className="text-2xl font-bold tracking-tight">Events</h1>
            <p className="text-muted-foreground text-sm">
              Browse upcoming events and find something you will love.
            </p>
          </div>

          <EventSearchFilters
            searchValue={searchValue}
            onSearchChange={(value) => updateParam('q', value)}
            dateValue={dateValue}
            onDateChange={(value) => updateParam('date', value)}
            locationValue={locationValue}
            onLocationChange={(value) => updateParam('location', value)}
            onClear={handleClear}
            hasActiveFilters={hasActiveFilters}
          />

          {isLoading && (
            <div className="flex justify-center py-16">
              <div className="text-muted-foreground text-sm">Loading events...</div>
            </div>
          )}

          {isError && (
            <div className="flex justify-center py-16">
              <p className="text-destructive text-sm">
                Something went wrong. Please try again later.
              </p>
            </div>
          )}

          {!isLoading && !isError && events.length === 0 && (
            <EmptyEventsState isFiltered={hasActiveFilters} onClearFilters={handleClear} />
          )}

          {events.length > 0 && (
            <EventCardGrid
              events={events}
              hasNextPage={!!hasNextPage}
              isFetchingNextPage={isFetchingNextPage}
              onLoadMore={() => fetchNextPage()}
            />
          )}
        </div>
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
