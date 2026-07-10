import type { RouteObject } from 'react-router-dom'

import { paths } from '@/app/paths'

import { EventResultsPage } from './pages/event-results-page'

export const reportingRoutes: RouteObject[] = [
  { path: paths.organizerEventResults, element: <EventResultsPage /> },
]
