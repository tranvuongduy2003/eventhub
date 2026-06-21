import { useMutation, useQueryClient } from '@tanstack/react-query'

import { uploadCoverImage } from './api'

export function useCoverImageUpload(eventId: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (file: File) => uploadCoverImage(eventId, file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['events', eventId] })
    },
  })
}
