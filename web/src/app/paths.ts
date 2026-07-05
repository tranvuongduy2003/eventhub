export const paths = {
  home: '/',
  login: '/login',
  register: '/register',
  events: '/events',
  organizerEvents: '/organizer/events',
  createEvent: '/organizer/events/create',
  editEvent: '/organizer/events/:eventId/edit',
  checkout: '/checkout',
  orderStatus: '/orders/:orderId',
  tickets: '/tickets',
  checkIn: '/check-in',
} as const
