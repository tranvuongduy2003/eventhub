import { useCallback, useRef, useState } from 'react'

import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { cn } from '@/lib/utils'
import { ImageIcon, UploadIcon, XIcon } from 'lucide-react'

const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'image/webp']
const MAX_SIZE_BYTES = 5 * 1024 * 1024

type CoverImageUploadProps = {
  currentImageUrl?: string | null
  onUpload: (file: File) => Promise<void>
  onRemove?: () => void
  disabled?: boolean
}

export function CoverImageUpload({
  currentImageUrl,
  onUpload,
  onRemove,
  disabled = false,
}: CoverImageUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [error, setError] = useState<string | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)

  const validateFile = useCallback((file: File): string | null => {
    if (!ACCEPTED_TYPES.includes(file.type)) {
      return 'Only JPEG, PNG, and WebP images are supported.'
    }
    if (file.size > MAX_SIZE_BYTES) {
      return 'File size must not exceed 5 MB.'
    }
    return null
  }, [])

  const handleFileSelect = useCallback(
    async (file: File) => {
      setError(null)

      const validationError = validateFile(file)
      if (validationError) {
        setError(validationError)
        return
      }

      const objectUrl = URL.createObjectURL(file)
      setPreviewUrl(objectUrl)

      setIsUploading(true)
      try {
        await onUpload(file)
      } catch {
        setError('Upload failed. Please try again.')
        setPreviewUrl(null)
      } finally {
        setIsUploading(false)
        URL.revokeObjectURL(objectUrl)
      }
    },
    [onUpload, validateFile],
  )

  const handleInputChange = useCallback(
    (event: React.ChangeEvent<HTMLInputElement>) => {
      const file = event.target.files?.[0]
      if (file) {
        handleFileSelect(file)
      }
    },
    [handleFileSelect],
  )

  const handleDrop = useCallback(
    (event: React.DragEvent) => {
      event.preventDefault()
      const file = event.dataTransfer.files[0]
      if (file) {
        handleFileSelect(file)
      }
    },
    [handleFileSelect],
  )

  const handleDragOver = useCallback((event: React.DragEvent) => {
    event.preventDefault()
  }, [])

  const displayUrl = previewUrl ?? currentImageUrl

  return (
    <div className="space-y-3">
      <div
        className={cn(
          'border-border relative flex min-h-[200px] cursor-pointer flex-col items-center justify-center rounded-lg border-2 border-dashed transition-colors',
          'hover:border-primary/50 hover:bg-muted/50',
          disabled && 'pointer-events-none opacity-50',
          error && 'border-destructive',
        )}
        onClick={() => inputRef.current?.click()}
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        role="button"
        tabIndex={0}
        aria-label="Upload cover image"
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault()
            inputRef.current?.click()
          }
        }}
      >
        {isUploading ? (
          <div className="flex flex-col items-center gap-2">
            <Spinner className="size-8" />
            <p className="text-muted-foreground text-sm">Uploading...</p>
          </div>
        ) : displayUrl ? (
          <div className="relative w-full">
            <img
              src={displayUrl}
              alt="Cover image preview"
              className="h-auto w-full rounded-lg object-cover"
              style={{ maxHeight: '300px' }}
            />
            {onRemove && !disabled && (
              <Button
                type="button"
                variant="destructive"
                size="icon"
                className="absolute top-2 right-2"
                onClick={(e) => {
                  e.stopPropagation()
                  onRemove()
                }}
                aria-label="Remove cover image"
              >
                <XIcon className="size-4" />
              </Button>
            )}
          </div>
        ) : (
          <div className="flex flex-col items-center gap-2 p-6">
            <div className="bg-muted rounded-full p-3">
              {currentImageUrl ? (
                <ImageIcon className="text-muted-foreground size-6" />
              ) : (
                <UploadIcon className="text-muted-foreground size-6" />
              )}
            </div>
            <div className="text-center">
              <p className="text-sm font-medium">
                {currentImageUrl ? 'Replace cover image' : 'Upload cover image'}
              </p>
              <p className="text-muted-foreground mt-1 text-xs">
                JPEG, PNG, or WebP • Max 5 MB
              </p>
            </div>
          </div>
        )}
      </div>

      {error && <p className="text-destructive text-sm">{error}</p>}

      <input
        ref={inputRef}
        type="file"
        accept={ACCEPTED_TYPES.join(',')}
        className="hidden"
        onChange={handleInputChange}
        disabled={disabled}
      />
    </div>
  )
}
