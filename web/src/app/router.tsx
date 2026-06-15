import { createBrowserRouter, Navigate } from 'react-router-dom'

import { paths } from '@/app/paths'
import { ProtectedRoute } from '@/app/routes/protected-route'
import { PublicRoute } from '@/app/routes/public-route'
import { authRoutes } from '@/features/auth'
import { checkInRoutes } from '@/features/check-in/routes'
import { checkoutRoutes } from '@/features/checkout/routes'
import { eventsRoutes } from '@/features/events/routes'
import { HomePage } from '@/features/home/pages/home-page'
import { ticketsRoutes } from '@/features/tickets/routes'
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
      ...checkoutRoutes,
      ...ticketsRoutes,
    ],
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <AppLayout />,
        children: [{ index: true, element: <HomePage /> }, ...eventsRoutes, ...checkInRoutes],
      },
    ],
  },
  { path: '*', element: <Navigate to={paths.home} replace /> },
])
