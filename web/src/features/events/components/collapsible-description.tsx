import { useState } from 'react'
import { ChevronDown, ChevronUp } from 'lucide-react'

import { cn } from '@/lib/utils'

interface CollapsibleDescriptionProps {
  description: string
  className?: string
}

export function CollapsibleDescription({ description, className }: CollapsibleDescriptionProps) {
  const [isOpen, setIsOpen] = useState(false)

  return (
    <div className={className}>
      <p
        className={cn(
          'text-muted-foreground text-base',
          !isOpen && 'line-clamp-4 md:line-clamp-none',
        )}
      >
        {description}
      </p>
      <button
        type="button"
        onClick={() => setIsOpen(!isOpen)}
        className="text-muted-foreground hover:text-foreground mt-2 flex min-h-[44px] items-center gap-1 text-sm font-medium underline underline-offset-4 md:hidden"
        aria-expanded={isOpen}
      >
        {isOpen ? (
          <>
            Show less <ChevronUp className="size-4" />
          </>
        ) : (
          <>
            Read more <ChevronDown className="size-4" />
          </>
        )}
      </button>
    </div>
  )
}
