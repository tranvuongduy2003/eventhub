import { apiClient } from '@/lib/api'

export type LoginRequest = {
  email: string
  password: string
}

export type LoginUserResponse = {
  userId: string
  displayName: string
  email: string
  role: string
}

export type RegisterRequest = {
  displayName: string
  email: string
  password: string
}

export type UserRegistrationResponse = {
  userId: string
  displayName: string
  email: string
  createdAt: string
}

export function getCurrentUser(signal?: AbortSignal) {
  return apiClient.get<LoginUserResponse>('/api/auth/me', { signal })
}

export function login(request: LoginRequest, signal?: AbortSignal) {
  return apiClient.post<LoginUserResponse>('/api/auth/login', request, {
    signal,
    suppressErrorToast: true,
  })
}

export function register(request: RegisterRequest, signal?: AbortSignal) {
  return apiClient.post<UserRegistrationResponse>('/api/users', request, {
    signal,
    suppressErrorToast: true,
  })
}

export function logout(signal?: AbortSignal) {
  return apiClient.post<void>('/api/auth/logout', undefined, {
    signal,
    suppressErrorToast: true,
  })
}
