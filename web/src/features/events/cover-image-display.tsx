import { ImageIcon } from 'lucide-react'

import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'

type CoverImageDisplayProps = {
  imageUrl?: string | null
  alt?: string
  isLoading?: boolean
  className?: string
}

export function CoverImageDisplay({
  imageUrl,
  alt = 'Event cover image',
  isLoading = false,
  className,
}: CoverImageDisplayProps) {
  if (isLoading) {
    return <Skeleton className={cn('aspect-video w-full rounded-lg', className)} />
  }

  if (!imageUrl) {
    return (
      <div
        className={cn(
          'bg-muted flex aspect-video w-full items-center justify-center rounded-lg',
          className,
        )}
      >
        <ImageIcon className="text-muted-foreground size-12" />
      </div>
    )
  }

  return (
    <img
      src={imageUrl}
      alt={alt}
      className={cn('aspect-video w-full rounded-lg object-cover', className)}
      loading="lazy"
    />
  )
}
