import { buildApiUrl } from '@/lib/env'
import { apiProblemSchema, ApiError } from '@/types/api-problem'

import {
  problemDetailsToastInterceptor,
  unauthorizedResponseInterceptor,
  withAntiForgeryHeader,
  withJsonRequestHeaders,
} from './interceptors'
import type {
  ApiRequestContext,
  ApiRequestInterceptor,
  ApiRequestOptions,
  ApiResponseInterceptor,
  HttpMethod,
} from './types'

function serializeBody(body: unknown): BodyInit | undefined {
  if (body === undefined || body === null) {
    return undefined
  }

  if (body instanceof FormData || body instanceof URLSearchParams || typeof body === 'string') {
    return body
  }

  return JSON.stringify(body)
}

function readAntiForgeryTokenFromDocument(): string | null {
  const meta = document.querySelector<HTMLMetaElement>('meta[name="csrf-token"]')
  return meta?.content ?? null
}

export class ApiClient {
  private readonly requestInterceptors: ApiRequestInterceptor[] = [
    withJsonRequestHeaders,
    withAntiForgeryHeader(readAntiForgeryTokenFromDocument),
  ]

  private readonly responseInterceptors: ApiResponseInterceptor[] = [
    unauthorizedResponseInterceptor,
    problemDetailsToastInterceptor,
  ]

  addRequestInterceptor(interceptor: ApiRequestInterceptor): () => void {
    this.requestInterceptors.push(interceptor)
    return () => {
      const index = this.requestInterceptors.indexOf(interceptor)
      if (index >= 0) {
        this.requestInterceptors.splice(index, 1)
      }
    }
  }

  addResponseInterceptor(interceptor: ApiResponseInterceptor): () => void {
    this.responseInterceptors.push(interceptor)
    return () => {
      const index = this.responseInterceptors.indexOf(interceptor)
      if (index >= 0) {
        this.responseInterceptors.splice(index, 1)
      }
    }
  }

  async request<TResponse>(path: string, options: ApiRequestOptions = {}): Promise<TResponse> {
    const method = (options.method ?? 'GET') as HttpMethod
    const { body, parseJson = true, headers, ...rest } = options

    let context: ApiRequestContext = {
      method,
      path,
      url: buildApiUrl(path),
      suppressErrorToast: options.suppressErrorToast,
      init: {
        ...rest,
        method,
        headers,
        body: serializeBody(body),
      },
    }

    for (const interceptor of this.requestInterceptors) {
      context = await interceptor(context)
    }

    const response = await fetch(context.url, {
      ...context.init,
      signal: options.signal,
    })

    let parsedBody: unknown = null

    if (response.status !== 204) {
      const contentType = response.headers.get('content-type') ?? ''

      if (
        contentType.includes('application/json') ||
        contentType.includes('application/problem+json')
      ) {
        parsedBody = await response.json()
      } else if (parseJson) {
        parsedBody = await response.text()
      }
    }

    if (!response.ok) {
      const parsedProblem = apiProblemSchema.safeParse(parsedBody)
      const problem = parsedProblem.success
        ? parsedProblem.data
        : {
            title: response.statusText,
            status: response.status,
            detail: typeof parsedBody === 'string' ? parsedBody : undefined,
          }

      const apiError = new ApiError(response.status, problem)
      let errorContext = {
        request: context,
        response,
        body: apiError,
      }

      for (const interceptor of this.responseInterceptors) {
        errorContext = await interceptor(errorContext)
      }

      throw apiError
    }

    let successContext = {
      request: context,
      response,
      body: parsedBody as TResponse,
    }

    for (const interceptor of this.responseInterceptors) {
      successContext = await interceptor(successContext)
    }

    return successContext.body
  }

  get<TResponse>(path: string, options?: Omit<ApiRequestOptions, 'method' | 'body'>) {
    return this.request<TResponse>(path, { ...options, method: 'GET' })
  }

  post<TResponse>(
    path: string,
    body?: unknown,
    options?: Omit<ApiRequestOptions, 'method' | 'body'>,
  ) {
    return this.request<TResponse>(path, { ...options, method: 'POST', body })
  }

  delete<TResponse>(path: string, options?: Omit<ApiRequestOptions, 'method' | 'body'>) {
    return this.request<TResponse>(path, { ...options, method: 'DELETE' })
  }
}

export const apiClient = new ApiClient()
