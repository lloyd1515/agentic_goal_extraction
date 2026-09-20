import React from 'react';
import { Trash2, Sparkles, Edit3, UserPlus, AlertCircle } from 'lucide-react';
import {
  EditableGoal,
  GoalCategory,
  GoalPriority,
  GOAL_CATEGORIES,
  GOAL_PRIORITIES,
  validateGoal,
} from '../types/goal';

interface GoalCardProps {
  goal: EditableGoal;
  index: number;
  onChange: (localId: string, updatedFields: Partial<EditableGoal>) => void;
  onRemove: (localId: string) => void;
  disabled?: boolean;
}

export const GoalCard: React.FC<GoalCardProps> = ({
  goal,
  index,
  onChange,
  onRemove,
  disabled = false,
}) => {
  // Track initial values from props for keyboard navigation revert (Escape key AC 5.2)
  const initialValuesRef = React.useRef({
    title: goal.title,
    description: goal.description,
    category: goal.category,
    priority: goal.priority,
    metric: goal.metric,
    timeframe: goal.timeframe,
  });

  const lastLocalIdRef = React.useRef(goal.localId);
  if (lastLocalIdRef.current !== goal.localId) {
    lastLocalIdRef.current = goal.localId;
    initialValuesRef.current = {
      title: goal.title,
      description: goal.description,
      category: goal.category,
      priority: goal.priority,
      metric: goal.metric,
      timeframe: goal.timeframe,
    };
  }

  const handleKeyDown = (
    fieldName: keyof EditableGoal,
    e: React.KeyboardEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => {
    if (e.key === 'Escape') {
      e.preventDefault();
      const initialValue = initialValuesRef.current[fieldName as keyof typeof initialValuesRef.current];

      if (initialValue !== undefined) {
        const target = e.target as HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement;
        if (target) {
          target.value = String(initialValue ?? '');
        }
        const updatedDraft = {
          ...goal,
          [fieldName]: initialValue,
        };
        const validationErrors = validateGoal(updatedDraft);
        onChange(goal.localId, {
          [fieldName]: initialValue,
          validationErrors,
        });
      }
    }
  };

  const handleFieldChange = (fieldName: keyof EditableGoal, value: unknown) => {
    const nextProvenance = goal.provenance === 'AI_ORIGINAL' ? 'AI_MODIFIED' : goal.provenance;
    const updatedDraft = {
      ...goal,
      [fieldName]: value,
      isEdited: true,
      provenance: nextProvenance,
    };

    const validationErrors = validateGoal(updatedDraft);

    onChange(goal.localId, {
      [fieldName]: value,
      isEdited: true,
      provenance: nextProvenance,
      validationErrors,
    });
  };

  const renderProvenanceBadge = () => {
    switch (goal.provenance) {
      case 'AI_ORIGINAL':
        return (
          <span className="inline-flex items-center gap-1 text-[11px] font-medium bg-blue-950/80 text-blue-400 border border-blue-800/60 px-2 py-0.5 rounded-md">
            <Sparkles className="w-3 h-3 text-blue-400" />
            AI Suggested
          </span>
        );
      case 'AI_MODIFIED':
        return (
          <span className="inline-flex items-center gap-1 text-[11px] font-medium bg-amber-950/80 text-amber-400 border border-amber-800/60 px-2 py-0.5 rounded-md">
            <Edit3 className="w-3 h-3 text-amber-400" />
            AI Suggested (Edited)
          </span>
        );
      case 'MANUAL':
        return (
          <span className="inline-flex items-center gap-1 text-[11px] font-medium bg-purple-950/80 text-purple-400 border border-purple-800/60 px-2 py-0.5 rounded-md">
            <UserPlus className="w-3 h-3 text-purple-400" />
            Manually Added
          </span>
        );
      default:
        return null;
    }
  };

  const hasErrors = Object.keys(goal.validationErrors || {}).length > 0;

  return (
    <div
      className={`bg-[#161b26] border rounded-xl p-5 transition shadow-md relative ${
        hasErrors
          ? 'border-rose-500/60 ring-1 ring-rose-500/20'
          : 'border-slate-800 hover:border-slate-700'
      }`}
    >
      {/* Header Bar: Goal #, Provenance, and Remove Action */}
      <div className="flex items-center justify-between gap-3 pb-3 border-b border-slate-800/80">
        <div className="flex items-center gap-2">
          <span className="text-xs font-mono font-semibold text-slate-400 bg-slate-800/70 px-2 py-0.5 rounded">
            #{index + 1}
          </span>
          {renderProvenanceBadge()}
        </div>

        <button
          type="button"
          onClick={() => onRemove(goal.localId)}
          disabled={disabled}
          className="text-slate-400 hover:text-rose-400 p-1.5 rounded-lg hover:bg-rose-950/40 border border-transparent hover:border-rose-900/60 transition disabled:opacity-50 disabled:cursor-not-allowed"
          title="Remove Goal"
          aria-label="Remove Goal"
        >
          <Trash2 className="w-4 h-4" />
        </button>
      </div>

      {/* Editable Fields Grid */}
      <div className="pt-4 space-y-4">
        {/* Title Input */}
        <div>
          <label className="block text-xs font-medium text-slate-300 mb-1">
            Goal Title <span className="text-rose-400">*</span>
          </label>
          <input
            type="text"
            value={goal.title}
            onKeyDown={(e) => handleKeyDown('title', e)}
            onChange={(e) => handleFieldChange('title', e.target.value)}
            disabled={disabled}
            placeholder="e.g. Migrate legacy auth to OAuth 2.1"
            className={`w-full bg-[#11141c] text-slate-100 border rounded-lg px-3 py-2 text-sm focus:outline-none transition ${
              goal.validationErrors?.title
                ? 'border-rose-500/80 focus:border-rose-400'
                : 'border-slate-800 focus:border-cyan-500/70'
            }`}
          />
          {goal.validationErrors?.title && (
            <p className="flex items-center gap-1 text-xs text-rose-400 mt-1 font-medium">
              <AlertCircle className="w-3 h-3" />
              {goal.validationErrors.title}
            </p>
          )}
        </div>

        {/* Description Textarea */}
        <div>
          <label className="block text-xs font-medium text-slate-300 mb-1">
            Description <span className="text-rose-400">*</span>
          </label>
          <textarea
            value={goal.description}
            onKeyDown={(e) => handleKeyDown('description', e)}
            onChange={(e) => handleFieldChange('description', e.target.value)}
            disabled={disabled}
            rows={3}
            placeholder="Detailed description of objective, context, and expected outcome..."
            className={`w-full bg-[#11141c] text-slate-100 border rounded-lg px-3 py-2 text-xs focus:outline-none transition leading-relaxed resize-y ${
              goal.validationErrors?.description
                ? 'border-rose-500/80 focus:border-rose-400'
                : 'border-slate-800 focus:border-cyan-500/70'
            }`}
          />
          {goal.validationErrors?.description && (
            <p className="flex items-center gap-1 text-xs text-rose-400 mt-1 font-medium">
              <AlertCircle className="w-3 h-3" />
              {goal.validationErrors.description}
            </p>
          )}
        </div>

        {/* 4-Column Metadata Controls: Category, Priority, Metric, Timeframe */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
          {/* Category */}
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">Category</label>
            <select
              value={goal.category}
              onKeyDown={(e) => handleKeyDown('category', e)}
              onChange={(e) => handleFieldChange('category', e.target.value as GoalCategory)}
              disabled={disabled}
              className="w-full bg-[#11141c] text-slate-200 border border-slate-800 rounded-lg px-2.5 py-1.5 text-xs focus:outline-none focus:border-cyan-500/70"
            >
              {GOAL_CATEGORIES.map((cat) => (
                <option key={cat} value={cat}>
                  {cat}
                </option>
              ))}
            </select>
          </div>

          {/* Priority */}
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">Priority</label>
            <select
              value={goal.priority}
              onKeyDown={(e) => handleKeyDown('priority', e)}
              onChange={(e) => handleFieldChange('priority', e.target.value as GoalPriority)}
              disabled={disabled}
              className="w-full bg-[#11141c] text-slate-200 border border-slate-800 rounded-lg px-2.5 py-1.5 text-xs focus:outline-none focus:border-cyan-500/70"
            >
              {GOAL_PRIORITIES.map((pri) => (
                <option key={pri} value={pri}>
                  {pri}
                </option>
              ))}
            </select>
          </div>

          {/* Metric */}
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">Metric</label>
            <input
              type="text"
              value={goal.metric}
              onKeyDown={(e) => handleKeyDown('metric', e)}
              onChange={(e) => handleFieldChange('metric', e.target.value)}
              disabled={disabled}
              placeholder="e.g. 85% coverage, zero downtime"
              className={`w-full bg-[#11141c] text-slate-100 border rounded-lg px-2.5 py-1.5 text-xs focus:outline-none transition ${
                goal.validationErrors?.metric
                  ? 'border-rose-500/80 focus:border-rose-400'
                  : 'border-slate-800 focus:border-cyan-500/70'
              }`}
            />
            {goal.validationErrors?.metric && (
              <p className="flex items-center gap-1 text-[11px] text-rose-400 mt-1 font-medium">
                <AlertCircle className="w-3 h-3" />
                {goal.validationErrors.metric}
              </p>
            )}
          </div>

          {/* Timeframe */}
          <div>
            <label className="block text-xs font-medium text-slate-300 mb-1">Timeframe</label>
            <input
              type="text"
              value={goal.timeframe}
              onKeyDown={(e) => handleKeyDown('timeframe', e)}
              onChange={(e) => handleFieldChange('timeframe', e.target.value)}
              disabled={disabled}
              placeholder="e.g. Q3 2026, 60 days"
              className={`w-full bg-[#11141c] text-slate-100 border rounded-lg px-2.5 py-1.5 text-xs focus:outline-none transition ${
                goal.validationErrors?.timeframe
                  ? 'border-rose-500/80 focus:border-rose-400'
                  : 'border-slate-800 focus:border-cyan-500/70'
              }`}
            />
            {goal.validationErrors?.timeframe && (
              <p className="flex items-center gap-1 text-[11px] text-rose-400 mt-1 font-medium">
                <AlertCircle className="w-3 h-3" />
                {goal.validationErrors.timeframe}
              </p>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
