import { z } from 'zod'

export const apiProblemSchema = z.object({
  type: z.string().optional(),
  title: z.string().optional(),
  status: z.number().optional(),
  detail: z.string().optional(),
  code: z.string().optional(),
  errors: z.record(z.string(), z.array(z.string())).optional(),
  nextEligibleAt: z.string().optional(),
})

export type ApiProblem = z.infer<typeof apiProblemSchema>

export class ApiError extends Error {
  readonly status: number
  readonly problem: ApiProblem

  constructor(status: number, problem: ApiProblem) {
    super(problem.detail ?? problem.title ?? 'Request failed')
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }
}
