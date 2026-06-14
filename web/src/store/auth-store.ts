import { create } from 'zustand'

export type AuthStatus = 'unknown' | 'authenticated' | 'unauthenticated'

type AuthSession = {
  userId: string
  username: string
  email: string
}

type AuthState = {
  status: AuthStatus
  userId: string | null
  username: string | null
  email: string | null
  setSession: (session: AuthSession) => void
  clearSession: () => void
  setStatus: (status: AuthStatus) => void
}

export const useAuthStore = create<AuthState>((set) => ({
  status: 'unknown',
  userId: null,
  username: null,
  email: null,
  setSession: (session) =>
    set({
      status: 'authenticated',
      userId: session.userId,
      username: session.username,
      email: session.email,
    }),
  clearSession: () =>
    set({
      status: 'unauthenticated',
      userId: null,
      username: null,
      email: null,
    }),
  setStatus: (status) => set({ status }),
}))
