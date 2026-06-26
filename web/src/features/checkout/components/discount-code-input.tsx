import { useState } from 'react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'

interface DiscountCodeInputProps {
  onApply: (code: string) => void
  onRemove: () => void
  appliedCode: string | null
  isLoading: boolean
  error: string | null
}

export function DiscountCodeInput({
  onApply,
  onRemove,
  appliedCode,
  isLoading,
  error,
}: DiscountCodeInputProps) {
  const [code, setCode] = useState('')

  if (appliedCode) {
    return (
      <div className="flex items-center justify-between rounded-md border px-3 py-2">
        <div className="flex items-center gap-2">
          <span className="text-muted-foreground text-sm">Code:</span>
          <span className="font-mono text-sm font-medium">{appliedCode}</span>
        </div>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={onRemove}
          className="text-muted-foreground h-auto px-2 py-1 text-xs"
        >
          Remove
        </Button>
      </div>
    )
  }

  return (
    <div className="space-y-1">
      <div className="flex gap-2">
        <Input
          placeholder="Discount code"
          value={code}
          onChange={(e) => setCode(e.target.value.toUpperCase())}
          disabled={isLoading}
          className="font-mono text-sm"
          onKeyDown={(e) => {
            if (e.key === 'Enter') {
              e.preventDefault()
              if (code.trim()) {
                onApply(code.trim())
              }
            }
          }}
        />
        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={isLoading || !code.trim()}
          onClick={() => onApply(code.trim())}
        >
          {isLoading ? <Spinner className="mr-1" /> : null}
          Apply
        </Button>
      </div>
      {error && <p className="text-destructive text-xs">{error}</p>}
    </div>
  )
}
