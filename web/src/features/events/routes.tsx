import type { RouteObject } from 'react-router-dom'

import { paths } from '@/app/paths'
import { CreateEventPage } from '@/features/events/pages/create-event-page'
import { EventsPlaceholderPage } from '@/features/events/pages/events-placeholder-page'

export const eventsRoutes: RouteObject[] = [
  { path: paths.events, element: <EventsPlaceholderPage /> },
  { path: paths.createEvent, element: <CreateEventPage /> },
]
