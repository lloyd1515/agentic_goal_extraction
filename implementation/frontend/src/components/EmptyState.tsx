import React from 'react';
import { Target, Sparkles, ArrowUp } from 'lucide-react';

interface EmptyStateProps {
  onLoadSample?: () => void;
}

export const EmptyState: React.FC<EmptyStateProps> = ({ onLoadSample }) => {
  return (
    <div className="bg-[#11141c] border border-slate-800 border-dashed rounded-2xl p-10 md:p-14 text-center max-w-2xl mx-auto space-y-5">
      <div className="mx-auto w-14 h-14 rounded-2xl bg-cyan-950/60 border border-cyan-800/40 flex items-center justify-center shadow-lg shadow-cyan-950/50">
        <Target className="w-7 h-7 text-cyan-400" />
      </div>

      <div className="space-y-2">
        <h3 className="text-base md:text-lg font-semibold text-slate-100">
          No Proposed Goals in Review Canvas
        </h3>
        <p className="text-xs md:text-sm text-slate-400 max-w-md mx-auto leading-relaxed">
          Provide a 1:1 conversation transcript above or load our sample discussion to extract
          SMART goals with AI-driven alignment against existing organizational goals.
        </p>
      </div>

      <div className="flex flex-col sm:flex-row items-center justify-center gap-3 pt-2">
        {onLoadSample && (
          <button
            type="button"
            onClick={onLoadSample}
            className="px-4 py-2 rounded-lg text-xs font-semibold bg-[#161b26] hover:bg-slate-800 text-cyan-400 border border-cyan-800/50 transition flex items-center gap-2"
          >
            <Sparkles className="w-3.5 h-3.5" />
            Load Sample Discussion
          </button>
        )}
        <div className="flex items-center gap-1.5 text-xs text-slate-500 font-mono">
          <ArrowUp className="w-3.5 h-3.5 text-cyan-400 animate-bounce" />
          <span>Click &ldquo;Extract Goals with AI&rdquo; above</span>
        </div>
      </div>
    </div>
  );
};
