import type { RouteObject } from 'react-router-dom'

import { LoginPage } from './pages/login-page'
import { RegisterPage } from './pages/register-page'

export const authRoutes: RouteObject[] = [
  { path: 'login', element: <LoginPage /> },
  { path: 'register', element: <RegisterPage /> },
]
