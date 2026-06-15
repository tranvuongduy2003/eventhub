import { create } from 'zustand'

export type AuthStatus = 'unknown' | 'authenticated' | 'unauthenticated'

type AuthSession = {
  userId: string
  displayName: string
  email: string
}

type AuthState = {
  status: AuthStatus
  userId: string | null
  displayName: string | null
  email: string | null
  setSession: (session: AuthSession) => void
  clearSession: () => void
  setStatus: (status: AuthStatus) => void
}

export const useAuthStore = create<AuthState>((set) => ({
  status: 'unknown',
  userId: null,
  displayName: null,
  email: null,
  setSession: (session) =>
    set({
      status: 'authenticated',
      userId: session.userId,
      displayName: session.displayName,
      email: session.email,
    }),
  clearSession: () =>
    set({
      status: 'unauthenticated',
      userId: null,
      displayName: null,
      email: null,
    }),
  setStatus: (status) => set({ status }),
}))
