/**
 * Types generated from contracts/openapi/api.v1.yaml.
 * Regenerate: yarn --cwd web api:codegen
 */
export type { components, operations, paths } from '@/generated/api-schema'

import type { paths } from '@/generated/api-schema'

export type ApiPath = keyof paths

export type ApiJsonBody<
  Path extends ApiPath,
  Method extends keyof paths[Path],
> = paths[Path][Method] extends { requestBody: { content: { 'application/json': infer Body } } }
  ? Body
  : never

export type ApiJsonResponse<
  Path extends ApiPath,
  Method extends keyof paths[Path],
  Status extends number = 200,
> = paths[Path][Method] extends {
  responses: { [K in Status]: { content: { 'application/json': infer Body } } }
}
  ? Body
  : never
