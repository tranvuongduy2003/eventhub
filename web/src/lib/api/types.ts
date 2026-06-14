export type HttpMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'

export type ApiRequestInit = Omit<RequestInit, 'method'> & {
  method: HttpMethod
}

export type ApiRequestContext = {
  method: HttpMethod
  path: string
  url: string
  init: ApiRequestInit
  suppressErrorToast?: boolean
}

export type ApiResponseContext<TBody = unknown> = {
  request: ApiRequestContext
  response: Response
  body: TBody
}

export type ApiRequestInterceptor = (
  context: ApiRequestContext,
) => ApiRequestContext | Promise<ApiRequestContext>

export type ApiResponseInterceptor = <TBody>(
  context: ApiResponseContext<TBody>,
) => ApiResponseContext<TBody> | Promise<ApiResponseContext<TBody>>

export type ApiRequestOptions = Omit<RequestInit, 'method' | 'body'> & {
  method?: HttpMethod
  body?: unknown
  parseJson?: boolean
  suppressErrorToast?: boolean
}
