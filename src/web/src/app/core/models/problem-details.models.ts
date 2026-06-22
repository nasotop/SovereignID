/** RFC 7807 Problem Details — transversal error shape (see ADR-0001) */
export interface ProblemDetails {
  readonly title: string;
  readonly status: number;
  readonly detail: string;
  readonly error?: string;
}

export function isProblemDetails(value: unknown): value is ProblemDetails {
  return (
    typeof value === 'object' &&
    value !== null &&
    'title' in value &&
    typeof value.title === 'string' &&
    'status' in value &&
    typeof value.status === 'number' &&
    'detail' in value &&
    typeof value.detail === 'string'
  );
}
