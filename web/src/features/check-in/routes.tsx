import type { RouteObject } from 'react-router-dom'

import { paths } from '@/app/paths'
import { CheckInPage } from '@/features/check-in/pages/check-in-page'

export const checkInRoutes: RouteObject[] = [{ path: paths.checkIn, element: <CheckInPage /> }]
