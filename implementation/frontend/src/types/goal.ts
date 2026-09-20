export type GoalCategory = 'PERFORMANCE' | 'DEVELOPMENT' | 'PROJECT' | 'TECHNICAL';

export type GoalPriority = 'HIGH' | 'MEDIUM' | 'LOW';

export type GoalProvenance = 'AI_ORIGINAL' | 'AI_MODIFIED' | 'MANUAL';

export interface ProposedGoal {
  tempId: string;
  title: string;
  description: string;
  category: GoalCategory;
  metric: string;
  timeframe: string;
  priority: GoalPriority;
  provenance: GoalProvenance;
}

export interface EditableGoal extends ProposedGoal {
  localId: string;
  isEdited: boolean;
  validationErrors: Record<string, string>;
}

export interface Employee {
  id: string;
  fullName: string;
  email: string;
  department: string;
}

export const GOAL_CATEGORIES: GoalCategory[] = [
  'PERFORMANCE',
  'DEVELOPMENT',
  'PROJECT',
  'TECHNICAL',
];

export const GOAL_PRIORITIES: GoalPriority[] = ['HIGH', 'MEDIUM', 'LOW'];

export function validateGoal(goal: Partial<ProposedGoal>): Record<string, string> {
  const errors: Record<string, string> = {};

  const title = (goal.title || '').trim();
  if (!title) {
    errors.title = 'Title cannot be empty.';
  } else if (title.length < 5 || title.length > 120) {
    errors.title = 'Title must be between 5 and 120 characters.';
  }

  const desc = (goal.description || '').trim();
  if (!desc) {
    errors.description = 'Description cannot be empty.';
  } else if (desc.length < 10 || desc.length > 1000) {
    errors.description = 'Description must be between 10 and 1000 characters.';
  }

  const metric = (goal.metric || '').trim();
  if (metric.length > 500) {
    errors.metric = 'Metric cannot exceed 500 characters.';
  }

  const timeframe = (goal.timeframe || '').trim();
  if (timeframe.length > 100) {
    errors.timeframe = 'Timeframe cannot exceed 100 characters.';
  }

  return errors;
}
