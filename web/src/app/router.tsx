import { createBrowserRouter, Navigate } from 'react-router-dom'

import { paths } from '@/app/paths'
import { ProtectedRoute } from '@/app/routes/protected-route'
import { PublicRoute } from '@/app/routes/public-route'
import { authRoutes } from '@/features/auth'
import { HomePage } from '@/features/home/pages/home-page'
import { AppLayout } from '@/layouts/app-layout'
import { AuthLayout } from '@/layouts/auth-layout'

export const router = createBrowserRouter([
  {
    element: <PublicRoute />,
    children: [
      {
        element: <AuthLayout />,
        children: authRoutes,
      },
    ],
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppLayout />,
        children: [{ index: true, element: <HomePage /> }],
      },
    ],
  },
  { path: '*', element: <Navigate to={paths.home} replace /> },
])
