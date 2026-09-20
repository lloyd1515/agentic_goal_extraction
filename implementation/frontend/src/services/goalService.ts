import { apiClient } from './apiClient';
import {
  ExtractGoalsRequest,
  GoalExtractionResultDto,
  SaveGoalsBatchRequestDto,
  SaveGoalsBatchResultDto,
  Employee,
} from '../types/api';

export interface GoalServiceOptions {
  idempotencyKey?: string;
  correlationId?: string;
}

export const goalService = {
  /**
   * Invokes the agentic extraction endpoint to parse SMART goals from transcript.
   */
  async extractGoals(
    request: ExtractGoalsRequest,
    options?: GoalServiceOptions
  ): Promise<GoalExtractionResultDto> {
    return apiClient.post<GoalExtractionResultDto>('/api/goals/extract', request, options);
  },

  /**
   * Atomically persists an approved batch of goals.
   * Generates and includes an Idempotency-Key header using crypto.randomUUID() when omitted.
   */
  async saveGoalsBatch(
    request: SaveGoalsBatchRequestDto,
    options?: GoalServiceOptions
  ): Promise<SaveGoalsBatchResultDto> {
    const idempotencyKey =
      options?.idempotencyKey ||
      (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function'
        ? crypto.randomUUID()
        : apiClient.generateUuid());

    return apiClient.post<SaveGoalsBatchResultDto>('/api/goals/batch', request, {
      ...options,
      idempotencyKey,
    });
  },

  /**
   * Fetches the list of seeded employees for context selection.
   */
  async fetchEmployees(options?: GoalServiceOptions): Promise<Employee[]> {
    return apiClient.get<Employee[]>('/api/employees', options);
  },

  /**
   * Fetches the active goals for a specific employee.
   */
  async fetchEmployeeGoals(
    employeeId: string,
    limit: number = 20,
    options?: GoalServiceOptions
  ): Promise<unknown[]> {
    return apiClient.get<unknown[]>(`/api/employees/${employeeId}/goals?limit=${limit}`, options);
  },

  /**
   * Checks backend health status.
   */
  async checkHealth(): Promise<boolean> {
    try {
      const response = await fetch('/health');
      return response.ok;
    } catch {
      return false;
    }
  },
};
