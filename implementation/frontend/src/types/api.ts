import { GoalCategory, GoalPriority, GoalProvenance, ProposedGoal, Employee } from './goal';

export interface ExtractGoalsContext {
  employeeId?: string;
  department?: string;
}

export interface ExtractGoalsRequest {
  transcript: string;
  context?: ExtractGoalsContext;
}

export type ExtractGoalsRequestDto = ExtractGoalsRequest;

export interface GoalExtractionResultDto {
  correlationId: string;
  transcriptHash: string;
  summary: string;
  goals: ProposedGoal[];
  toolCallsExecuted: string[];
  extractedAt: string;
}

export interface SaveGoalItemDto {
  title: string;
  description: string;
  category: GoalCategory;
  metric: string;
  timeframe: string;
  priority: GoalPriority;
  provenance: GoalProvenance;
}

export interface SaveGoalsBatchRequestDto {
  employeeId: string;
  sourceTranscriptHash?: string;
  reviewerId?: string;
  goals: SaveGoalItemDto[];
}

export interface SaveGoalsBatchResultDto {
  correlationId: string;
  success: boolean;
  savedCount: number;
  savedGoalIds: string[];
  savedAt: string;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}

export type { Employee };
