import type { RouteObject } from 'react-router-dom'

import { paths } from '@/app/paths'
import { CheckoutPlaceholderPage } from '@/features/checkout/pages/checkout-placeholder-page'
import { OrderStatusPage } from '@/features/checkout/pages/order-status-page'

export const checkoutRoutes: RouteObject[] = [
  { path: paths.checkout, element: <CheckoutPlaceholderPage /> },
  { path: paths.orderStatus, element: <OrderStatusPage /> },
]
