import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { apiClient, ApiError, request } from '../services/apiClient';
import { goalService } from '../services/goalService';

describe('apiClient and request', () => {
  const originalFetch = globalThis.fetch;

  beforeEach(() => {
    vi.restoreAllMocks();
  });

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it('performs GET request and parses JSON response successfully', async () => {
    const mockData = { id: 'emp-1', fullName: 'Elena Rostova' };
    const mockResponse = new Response(JSON.stringify(mockData), {
      status: 200,
      headers: {
        'content-type': 'application/json',
        'X-Correlation-ID': 'test-corr-123',
      },
    });

    globalThis.fetch = vi.fn().mockResolvedValue(mockResponse);

    const result = await apiClient.get<typeof mockData>('/api/employees');

    expect(result).toEqual(mockData);
    expect(globalThis.fetch).toHaveBeenCalledTimes(1);

    const [url, init] = (globalThis.fetch as any).mock.calls[0];
    expect(url).toBe('/api/employees');
    expect(init.method).toBe('GET');

    const headers = init.headers as Headers;
    expect(headers.get('Accept')).toBe('application/json, application/problem+json');
    expect(headers.has('X-Correlation-ID')).toBe(true);
  });

  it('preserves user-provided correlationId in request headers', async () => {
    const mockResponse = new Response(JSON.stringify({ ok: true }), {
      status: 200,
      headers: { 'content-type': 'application/json' },
    });
    globalThis.fetch = vi.fn().mockResolvedValue(mockResponse);

    await apiClient.get('/api/test', { correlationId: 'custom-cid-456' });

    const [, init] = (globalThis.fetch as any).mock.calls[0];
    const headers = init.headers as Headers;
    expect(headers.get('X-Correlation-ID')).toBe('custom-cid-456');
  });

  it('performs POST request with stringified JSON payload and Content-Type', async () => {
    const payload = { prompt: 'Develop automated tests' };
    const mockResponse = new Response(JSON.stringify({ success: true, count: 1 }), {
      status: 200,
      headers: { 'content-type': 'application/json' },
    });
    globalThis.fetch = vi.fn().mockResolvedValue(mockResponse);

    const result = await apiClient.post<{ success: boolean; count: number }>(
      '/api/goals/batch',
      payload
    );

    expect(result).toEqual({ success: true, count: 1 });
    const [, init] = (globalThis.fetch as any).mock.calls[0];
    expect(init.method).toBe('POST');
    expect(init.body).toBe(JSON.stringify(payload));
    const headers = init.headers as Headers;
    expect(headers.get('Content-Type')).toBe('application/json');
    expect(headers.has('Idempotency-Key')).toBe(true);
  });

  it('generates a unique UUID Idempotency-Key header on POST /api/goals/batch', async () => {
    const mockResponse = new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { 'content-type': 'application/json' },
    });
    globalThis.fetch = vi.fn().mockResolvedValue(mockResponse);

    await apiClient.post('/api/goals/batch', { items: [] });

    const [, init] = (globalThis.fetch as any).mock.calls[0];
    const headers = init.headers as Headers;
    const idempotencyKey = headers.get('Idempotency-Key');
    expect(idempotencyKey).toBeTruthy();
    expect(idempotencyKey).toMatch(
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i
    );
  });

  it('preserves user-provided idempotencyKey on POST /api/goals/batch', async () => {
    const mockResponse = new Response(JSON.stringify({ success: true }), {
      status: 200,
      headers: { 'content-type': 'application/json' },
    });
    globalThis.fetch = vi.fn().mockResolvedValue(mockResponse);

    await apiClient.post(
      '/api/goals/batch',
      { items: [] },
      { idempotencyKey: 'custom-idempotency-key-789' }
    );

    const [, init] = (globalThis.fetch as any).mock.calls[0];
    const headers = init.headers as Headers;
    expect(headers.get('Idempotency-Key')).toBe('custom-idempotency-key-789');
  });

  it('handles 204 No Content response properly', async () => {
    const mockResponse = new Response(null, {
      status: 204,
      statusText: 'No Content',
    });
    globalThis.fetch = vi.fn().mockResolvedValue(mockResponse);

    const result = await request('/api/goals/123', { method: 'DELETE' });
    expect(result).toEqual({});
  });

  it('throws ApiError with ProblemDetails information on 400 Bad Request', async () => {
    const problemDetails = {
      title: 'One or more validation errors occurred.',
      status: 400,
      detail: 'Validation failed for the request payload.',
      errors: {
        Title: ['Title must be between 5 and 120 characters.'],
        Timeframe: ['Timeframe cannot exceed 100 characters.'],
      },
    };

    const mockResponse = new Response(JSON.stringify(problemDetails), {
      status: 400,
      statusText: 'Bad Request',
      headers: {
        'content-type': 'application/problem+json',
        'X-Correlation-ID': 'corr-err-400',
      },
    });
    globalThis.fetch = vi.fn().mockResolvedValue(mockResponse);

    await expect(apiClient.post('/api/goals/batch', {})).rejects.toSatisfy((err: unknown) => {
      expect(err).toBeInstanceOf(ApiError);
      const apiErr = err as ApiError;
      expect(apiErr.status).toBe(400);
      expect(apiErr.problemDetails).toEqual(problemDetails);
      expect(apiErr.correlationId).toBe('corr-err-400');
      expect(apiErr.message).toContain('Validation failed for the request payload.');
      expect(apiErr.message).toContain('Title: Title must be between 5 and 120 characters.');
      return true;
    });
  });

  it('handles network failure by throwing friendly ApiError with status 0', async () => {
    globalThis.fetch = vi.fn().mockRejectedValue(new Error('Failed to fetch'));

    await expect(apiClient.get('/api/health')).rejects.toSatisfy((err: unknown) => {
      expect(err).toBeInstanceOf(ApiError);
      const apiErr = err as ApiError;
      expect(apiErr.status).toBe(0);
      expect(apiErr.message).toContain('Failed to connect to backend server');
      expect(apiErr.correlationId).toBeDefined();
      return true;
    });
  });

  it('handles non-JSON error response from backend or reverse proxy', async () => {
    const mockResponse = new Response('502 Bad Gateway', {
      status: 502,
      statusText: 'Bad Gateway',
      headers: { 'content-type': 'text/plain' },
    });
    globalThis.fetch = vi.fn().mockResolvedValue(mockResponse);

    await expect(apiClient.get('/api/endpoint')).rejects.toSatisfy((err: unknown) => {
      expect(err).toBeInstanceOf(ApiError);
      const apiErr = err as ApiError;
      expect(apiErr.status).toBe(502);
      expect(apiErr.message).toBe('502 Bad Gateway');
      return true;
    });
  });

  it('generates crypto.randomUUID() Idempotency-Key on goalService.saveGoalsBatch', async () => {
    const mockResponse = new Response(
      JSON.stringify({ savedCount: 1, correlationId: 'test-cid' }),
      {
        status: 200,
        headers: { 'content-type': 'application/json' },
      }
    );
    globalThis.fetch = vi.fn().mockResolvedValue(mockResponse);

    await goalService.saveGoalsBatch({ employeeId: 'emp-1', goals: [] });

    const [url, init] = (globalThis.fetch as any).mock.calls[0];
    expect(url).toBe('/api/goals/batch');
    const headers = init.headers as Headers;
    const key = headers.get('Idempotency-Key');
    expect(key).toBeTruthy();
    expect(key).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i);
  });
});
