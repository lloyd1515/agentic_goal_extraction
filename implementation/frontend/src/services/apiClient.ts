import { ProblemDetails } from '../types/api';

export class ApiError extends Error {
  public readonly status: number;
  public readonly problemDetails?: ProblemDetails;
  public readonly correlationId?: string;

  constructor(status: number, message: string, problemDetails?: ProblemDetails, correlationId?: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problemDetails = problemDetails;
    this.correlationId = correlationId;
  }
}

export function generateCorrelationId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

export const generateUuid = generateCorrelationId;

export interface RequestOptions extends RequestInit {
  correlationId?: string;
  idempotencyKey?: string;
}

export async function request<T>(endpoint: string, options: RequestOptions = {}): Promise<T> {
  const correlationId = options.correlationId || generateCorrelationId();
  
  const headers = new Headers(options.headers || {});
  if (!headers.has('Content-Type') && options.body && typeof options.body === 'string') {
    headers.set('Content-Type', 'application/json');
  }
  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json, application/problem+json');
  }
  if (!headers.has('X-Correlation-ID')) {
    headers.set('X-Correlation-ID', correlationId);
  }

  if (options.idempotencyKey) {
    if (!headers.has('Idempotency-Key')) {
      headers.set('Idempotency-Key', options.idempotencyKey);
    }
  } else if (
    endpoint.includes('/api/goals/batch') &&
    (options.method?.toUpperCase() === 'POST' || (!options.method && options.body))
  ) {
    if (!headers.has('Idempotency-Key')) {
      const key =
        typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function'
          ? crypto.randomUUID()
          : generateCorrelationId();
      headers.set('Idempotency-Key', key);
    }
  }

  let response: Response;
  try {
    response = await fetch(endpoint, {
      ...options,
      headers,
    });
  } catch (err: unknown) {
    const networkMessage = err instanceof Error ? err.message : 'Network error';
    throw new ApiError(
      0,
      `Failed to connect to backend server (${networkMessage}). Please verify the backend API is running.`,
      undefined,
      correlationId
    );
  }

  const responseCorrelationId = response.headers.get('X-Correlation-ID') || correlationId;

  if (!response.ok) {
    let errorDetails: ProblemDetails | undefined;
    let errorMessage = `Request failed with status ${response.status} (${response.statusText})`;

    try {
      const contentType = response.headers.get('content-type');
      if (contentType && contentType.includes('json')) {
        errorDetails = await response.json();
        if (errorDetails) {
          if (errorDetails.detail) {
            errorMessage = errorDetails.detail;
          } else if (errorDetails.title) {
            errorMessage = errorDetails.title;
          }

          if (errorDetails.errors && typeof errorDetails.errors === 'object') {
            const fieldErrors = Object.entries(errorDetails.errors)
              .map(([field, msgs]) => `${field}: ${(Array.isArray(msgs) ? msgs : [msgs]).join(', ')}`)
              .join('; ');
            if (fieldErrors) {
              errorMessage = `${errorMessage} (${fieldErrors})`;
            }
          }
        }
      } else {
        const text = await response.text();
        if (text) {
          errorMessage = text;
        }
      }
    } catch {
      // Body reading or json parse failed, keep default message
    }

    throw new ApiError(response.status, errorMessage, errorDetails, responseCorrelationId);
  }

  if (response.status === 204) {
    return {} as T;
  }

  return (await response.json()) as T;
}

export const apiClient = {
  get: <T>(url: string, options?: RequestOptions) => request<T>(url, { ...options, method: 'GET' }),
  post: <T>(url: string, body?: unknown, options?: RequestOptions) =>
    request<T>(url, {
      ...options,
      method: 'POST',
      body: body !== undefined ? JSON.stringify(body) : undefined,
    }),
  generateUuid,
};
