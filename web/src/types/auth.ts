import { z } from 'zod'

const passwordSpecialCharacterPattern = /[!@#$%^&*()_+\-=[\]{}|;:'",.<>?/`~]/

export const registerFormSchema = z
  .object({
    displayName: z
      .string()
      .trim()
      .min(1, 'Display name is required.')
      .max(64, 'Display name must be between 1 and 64 characters.'),
    email: z
      .string()
      .trim()
      .min(1, 'Email is required.')
      .max(254, 'Email cannot exceed 254 characters.')
      .email('Email address format is invalid.'),
    password: z
      .string()
      .min(1, 'Password is required.')
      .min(8, 'Password must be at least 8 characters.')
      .regex(/[A-Za-z]/, 'Password must include at least one letter.')
      .regex(/[0-9]/, 'Password must include at least one digit.')
      .regex(
        passwordSpecialCharacterPattern,
        'Password must include at least one special character.',
      ),
    confirmPassword: z.string().min(1, 'Confirm your password.'),
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: 'Passwords do not match.',
    path: ['confirmPassword'],
  })

export type RegisterFormValues = z.infer<typeof registerFormSchema>

export const loginFormSchema = z.object({
  email: z
    .string()
    .trim()
    .min(1, 'Email is required.')
    .max(254, 'Email cannot exceed 254 characters.')
    .email('Email address format is invalid.'),
  password: z.string().min(1, 'Password is required.'),
})

export type LoginFormValues = z.infer<typeof loginFormSchema>

export type LoginLocationState = {
  from?: string
  reason?: 'session-expired'
}
