import { toast } from 'sonner'

import { ApiError } from '@/types/api-problem'

import type { ApiRequestContext, ApiResponseInterceptor } from './types'

export function withJsonRequestHeaders(context: ApiRequestContext): ApiRequestContext {
  const headers = new Headers(context.init.headers)

  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json')
  }

  if (context.init.body !== undefined && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }

  return {
    ...context,
    init: {
      ...context.init,
      credentials: context.init.credentials ?? 'include',
      headers,
    },
  }
}

export function withAntiForgeryHeader(getToken: () => string | null) {
  return (context: ApiRequestContext): ApiRequestContext => {
    const method = context.method.toUpperCase()
    if (method === 'GET' || method === 'HEAD' || method === 'OPTIONS') {
      return context
    }

    const token = getToken()
    if (!token) {
      return context
    }

    const headers = new Headers(context.init.headers)
    headers.set('RequestVerificationToken', token)

    return {
      ...context,
      init: {
        ...context.init,
        headers,
      },
    }
  }
}

export const unauthorizedResponseInterceptor: ApiResponseInterceptor = (context) => {
  if (context.response.status === 401) {
    window.dispatchEvent(new CustomEvent('api:unauthorized'))
  }

  return context
}

export const problemDetailsToastInterceptor: ApiResponseInterceptor = (context) => {
  if (context.response.ok) {
    return context
  }

  if (context.response.status === 401) {
    return context
  }

  if (context.request.suppressErrorToast) {
    return context
  }

  if (context.body instanceof ApiError) {
    toast.error(context.body.problem.title ?? 'Request failed', {
      description: context.body.problem.detail,
    })
  }

  return context
}
