import type { RouteObject } from 'react-router-dom'

import { OrderTicketsPage } from '@/features/tickets/pages/order-tickets-page'

export const ticketsRoutes: RouteObject[] = [
  { path: '/tickets/orders/:orderId', element: <OrderTicketsPage /> },
]
