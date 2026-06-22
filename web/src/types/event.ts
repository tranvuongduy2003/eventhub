import { z } from 'zod'

export const createEventFormSchema = z
  .object({
    title: z
      .string()
      .trim()
      .min(1, 'Title is required.')
      .max(200, 'Title must be 200 characters or fewer.'),
    startDate: z.string().min(1, 'Start date is required.'),
    startTime: z.string().min(1, 'Start time is required.'),
    endDate: z.string().min(1, 'End date is required.'),
    endTime: z.string().min(1, 'End time is required.'),
    timeZoneId: z.string().min(1, 'Time zone is required.'),
    isOnline: z.boolean(),
    physicalAddress: z.string().optional(),
  })
  .refine(
    (values) => {
      const start = new Date(`${values.startDate}T${values.startTime}`)
      const end = new Date(`${values.endDate}T${values.endTime}`)
      return end > start
    },
    { message: 'End must be after start.', path: ['endDate'] },
  )
  .refine(
    (values) => {
      if (values.isOnline) return true
      return !!values.physicalAddress?.trim()
    },
    { message: 'Address is required for in-person events.', path: ['physicalAddress'] },
  )

export type CreateEventFormValues = z.infer<typeof createEventFormSchema>

export const editEventFormSchema = z
  .object({
    title: z
      .string()
      .trim()
      .min(1, 'Title is required.')
      .max(200, 'Title must be 200 characters or fewer.'),
    startDate: z.string().min(1, 'Start date is required.'),
    startTime: z.string().min(1, 'Start time is required.'),
    endDate: z.string().min(1, 'End date is required.'),
    endTime: z.string().min(1, 'End time is required.'),
    timeZoneId: z.string().min(1, 'Time zone is required.'),
    isOnline: z.boolean(),
    physicalAddress: z.string().optional(),
    description: z.string().max(2000, 'Description must be 2000 characters or fewer.').optional(),
  })
  .refine(
    (values) => {
      const start = new Date(`${values.startDate}T${values.startTime}`)
      const end = new Date(`${values.endDate}T${values.endTime}`)
      return end > start
    },
    { message: 'End must be after start.', path: ['endDate'] },
  )
  .refine(
    (values) => {
      if (values.isOnline) return true
      return !!values.physicalAddress?.trim()
    },
    { message: 'Address is required for in-person events.', path: ['physicalAddress'] },
  )

export type EditEventFormValues = z.infer<typeof editEventFormSchema>
