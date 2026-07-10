import { createBrowserRouter, Navigate } from 'react-router-dom'

import { paths } from '@/app/paths'
import { ProtectedRoute } from '@/app/routes/protected-route'
import { PublicRoute } from '@/app/routes/public-route'
import { authRoutes } from '@/features/auth'
import { checkInRoutes } from '@/features/check-in/routes'
import { checkoutRoutes } from '@/features/checkout/routes'
import { eventsRoutes } from '@/features/events/routes'
import { PublicEventPage } from '@/features/events/pages/public-event-page'
import { PublicEventsPage } from '@/features/events/pages/public-events-page'
import { HomePage } from '@/features/home/pages/home-page'
import { reportingRoutes } from '@/features/reporting/routes'
import { ticketsRoutes } from '@/features/tickets/routes'
import { TicketWalletPage } from '@/features/tickets/pages/ticket-wallet-page'
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
        children: [
          { index: true, element: <HomePage /> },
          ...eventsRoutes,
          ...reportingRoutes,
          ...checkInRoutes,
          { path: paths.tickets, element: <TicketWalletPage /> },
        ],
      },
    ],
  },
  ...checkoutRoutes,
  ...ticketsRoutes,
  { path: paths.events, element: <PublicEventsPage /> },
  { path: '/events/:slug', element: <PublicEventPage /> },
  { path: '*', element: <Navigate to={paths.home} replace /> },
])
