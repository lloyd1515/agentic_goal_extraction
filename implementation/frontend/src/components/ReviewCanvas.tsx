import React from 'react';
import {
  Save,
  PlusCircle,
  Sparkles,
  AlertTriangle,
  CheckCircle,
  Loader2,
  ListTodo,
} from 'lucide-react';
import { EditableGoal } from '../types/goal';
import { GoalCard } from './GoalCard';

interface ReviewCanvasProps {
  summary: string | null;
  goals: EditableGoal[];
  onUpdateGoal: (localId: string, updatedFields: Partial<EditableGoal>) => void;
  onRemoveGoal: (localId: string) => void;
  onAddGoal: () => void;
  onSaveBatch: () => void;
  isSaving: boolean;
  selectedEmployeeName?: string;
}

export const ReviewCanvas: React.FC<ReviewCanvasProps> = ({
  summary,
  goals,
  onUpdateGoal,
  onRemoveGoal,
  onAddGoal,
  onSaveBatch,
  isSaving,
  selectedEmployeeName,
}) => {
  const totalGoals = goals.length;
  const invalidGoalsCount = goals.filter(
    (g) => Object.keys(g.validationErrors || {}).length > 0
  ).length;
  const isSaveDisabled = totalGoals === 0 || invalidGoalsCount > 0 || isSaving;

  return (
    <div className="space-y-6">
      {/* AI Summary Box (if present) */}
      {summary && (
        <div className="bg-[#161b26] border border-cyan-800/40 rounded-xl p-4 md:p-5 shadow-lg relative overflow-hidden">
          <div className="absolute top-0 right-0 w-32 h-32 bg-cyan-500/5 rounded-full blur-xl pointer-events-none" />
          <div className="flex items-start gap-3">
            <div className="p-2 bg-cyan-950/80 border border-cyan-800/60 rounded-lg text-cyan-400 shrink-0">
              <Sparkles className="w-4 h-4" />
            </div>
            <div className="space-y-1 flex-1">
              <h3 className="text-xs font-semibold uppercase tracking-wider text-cyan-400">
                Discussion Summary &amp; Historical Context
              </h3>
              <p className="text-xs md:text-sm text-slate-300 leading-relaxed font-normal">
                {summary}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Review Canvas Action Header */}
      <div className="flex flex-wrap items-center justify-between gap-4 bg-[#11141c] border border-slate-800 rounded-xl p-4 shadow-md">
        <div className="flex items-center space-x-3">
          <ListTodo className="w-5 h-5 text-cyan-400" />
          <div>
            <h2 className="text-sm md:text-base font-semibold text-slate-100 flex items-center gap-2">
              Proposed Goals Review Canvas
              <span className="text-xs font-mono font-normal text-slate-400 bg-slate-800/80 px-2 py-0.5 rounded-full">
                {totalGoals} {totalGoals === 1 ? 'Goal' : 'Goals'}
              </span>
            </h2>
            <p className="text-xs text-slate-400">
              Review, edit, or append goals before committing to the database.
              {selectedEmployeeName && (
                <span className="text-slate-300 ml-1">
                  Target Employee: <strong className="text-cyan-300">{selectedEmployeeName}</strong>
                </span>
              )}
            </p>
          </div>
        </div>

        {/* Header Action Buttons */}
        <div className="flex items-center gap-2.5">
          <button
            type="button"
            onClick={onAddGoal}
            disabled={isSaving}
            className="px-3.5 py-2 rounded-lg text-xs font-medium bg-[#161b26] hover:bg-slate-800 text-slate-200 border border-slate-700/70 transition flex items-center gap-1.5 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <PlusCircle className="w-3.5 h-3.5 text-cyan-400" />
            <span>Add Goal</span>
          </button>

          <div title={isSaveDisabled ? 'Resolve all field errors before saving' : 'Save goals to database'}>
            <button
              type="button"
              onClick={onSaveBatch}
              disabled={isSaveDisabled}
              title={isSaveDisabled ? 'Resolve all field errors before saving' : 'Save goals to database'}
              className={`px-4 py-2 rounded-lg text-xs font-semibold uppercase tracking-wider flex items-center gap-2 transition shadow-lg ${
                isSaveDisabled
                  ? 'bg-slate-800 text-slate-500 cursor-not-allowed border border-slate-700/50'
                  : 'bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white shadow-emerald-600/20 active:scale-[0.98]'
              }`}
            >
              {isSaving ? (
                <>
                  <Loader2 className="w-3.5 h-3.5 animate-spin" />
                  <span>Saving Batch...</span>
                </>
              ) : (
                <>
                  <Save className="w-3.5 h-3.5" />
                  <span>Save Goals to Database</span>
                </>
              )}
            </button>
          </div>
        </div>
      </div>

      {/* Validation Warning Alert */}
      {invalidGoalsCount > 0 && (
        <div className="bg-rose-950/40 border border-rose-800/60 rounded-xl p-3.5 flex items-center gap-2.5 text-rose-300 text-xs">
          <AlertTriangle className="w-4 h-4 text-rose-400 shrink-0" />
          <span>
            <strong>{invalidGoalsCount}</strong> {invalidGoalsCount === 1 ? 'goal has' : 'goals have'} validation errors. Please resolve them to enable database persistence.
          </span>
        </div>
      )}

      {/* List of Goal Cards */}
      <div className="space-y-4">
        {goals.map((goal, idx) => (
          <GoalCard
            key={goal.localId}
            goal={goal}
            index={idx}
            onChange={onUpdateGoal}
            onRemove={onRemoveGoal}
            disabled={isSaving}
          />
        ))}
      </div>

      {/* Bottom Save Action Bar for convenience on longer lists */}
      {totalGoals >= 2 && (
        <div className="flex items-center justify-between pt-2 border-t border-slate-800/80">
          <button
            type="button"
            onClick={onAddGoal}
            disabled={isSaving}
            className="px-3.5 py-2 rounded-lg text-xs font-medium bg-[#161b26] hover:bg-slate-800 text-slate-200 border border-slate-700/70 transition flex items-center gap-1.5"
          >
            <PlusCircle className="w-3.5 h-3.5 text-cyan-400" />
            <span>Add Another Goal</span>
          </button>

          <div title={isSaveDisabled ? 'Resolve all field errors before saving' : 'Save goals to database'}>
            <button
              type="button"
              onClick={onSaveBatch}
              disabled={isSaveDisabled}
              title={isSaveDisabled ? 'Resolve all field errors before saving' : 'Save goals to database'}
              className={`px-4 py-2 rounded-lg text-xs font-semibold uppercase tracking-wider flex items-center gap-2 transition shadow-lg ${
                isSaveDisabled
                  ? 'bg-slate-800 text-slate-500 cursor-not-allowed border border-slate-700/50'
                  : 'bg-gradient-to-r from-emerald-600 to-teal-600 hover:from-emerald-500 hover:to-teal-500 text-white shadow-emerald-600/20 active:scale-[0.98]'
              }`}
            >
              {isSaving ? (
                <>
                  <Loader2 className="w-3.5 h-3.5 animate-spin" />
                  <span>Saving Batch...</span>
                </>
              ) : (
                <>
                  <CheckCircle className="w-3.5 h-3.5" />
                  <span>Save All ({totalGoals}) Goals</span>
                </>
              )}
            </button>
          </div>
        </div>
      )}
    </div>
  );
};
