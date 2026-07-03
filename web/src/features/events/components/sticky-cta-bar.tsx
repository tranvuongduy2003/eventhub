import { Button } from '@/components/ui/button'

interface StickyCtaBarProps {
  eventTitle: string
  visible: boolean
  selectedCount: number
  disabled: boolean
  pending: boolean
  onStartCheckout: () => void
}

export function StickyCtaBar({
  eventTitle,
  visible,
  selectedCount,
  disabled,
  pending,
  onStartCheckout,
}: StickyCtaBarProps) {
  if (!visible) {
    return null
  }

  return (
    <div
      className="bg-background/95 fixed inset-x-0 bottom-0 z-50 flex items-center justify-between gap-3 border-t px-4 py-3 backdrop-blur md:hidden"
      style={{ paddingBottom: 'max(0.75rem, env(safe-area-inset-bottom))' }}
    >
      <span className="min-w-0 truncate text-sm font-medium">
        {selectedCount > 0 ? `${selectedCount} selected` : eventTitle}
      </span>
      <Button
        size="lg"
        className="h-11 shrink-0"
        disabled={disabled || pending}
        onClick={onStartCheckout}
        aria-label="Continue to checkout"
      >
        {pending ? 'Checking...' : 'Checkout'}
      </Button>
    </div>
  )
}
