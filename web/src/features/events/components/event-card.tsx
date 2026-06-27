import { Link } from 'react-router-dom'

import { CoverImageDisplay } from '@/features/events/cover-image-display'
import { formatPrice } from '@/lib/utils/format-price'
import type { PublicEventListItemResponse } from '@/features/events/api'

type EventCardProps = {
  event: PublicEventListItemResponse
}

function formatDate(startsAt: string | null): string {
  if (!startsAt) return ''

  const date = new Date(startsAt)
  return date.toLocaleDateString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}

function formatLocation(physicalAddress: string | null, isOnline: boolean): string {
  if (isOnline) return 'Online'
  if (!physicalAddress) return ''
  // Show city only (last part after comma)
  const parts = physicalAddress.split(',')
  return parts.length > 1 ? parts[parts.length - 1].trim() : physicalAddress
}

export function EventCard({ event }: EventCardProps) {
  const priceDisplay = event.isSoldOut
    ? 'Sold out'
    : event.lowestPriceAmount !== null && event.lowestPriceCurrency
      ? `From ${formatPrice(event.lowestPriceAmount, event.lowestPriceCurrency)}`
      : null

  return (
    <Link
      to={`/events/${event.slug}`}
      className="group block overflow-hidden rounded-lg border transition-shadow hover:shadow-md"
    >
      <CoverImageDisplay
        imageUrl={event.coverImageUrl}
        alt={event.title}
        className="aspect-video rounded-none"
      />
      <div className="flex flex-col gap-1.5 p-4">
        <h3 className="group-hover:text-primary line-clamp-2 text-base font-semibold transition-colors">
          {event.title}
        </h3>
        <p className="text-muted-foreground text-sm">{formatDate(event.startsAt)}</p>
        {formatLocation(event.physicalAddress, event.isOnline) && (
          <p className="text-muted-foreground text-sm">
            {formatLocation(event.physicalAddress, event.isOnline)}
          </p>
        )}
        {priceDisplay && (
          <p
            className={
              event.isSoldOut ? 'text-destructive text-sm font-medium' : 'text-sm font-medium'
            }
          >
            {priceDisplay}
          </p>
        )}
      </div>
    </Link>
  )
}
