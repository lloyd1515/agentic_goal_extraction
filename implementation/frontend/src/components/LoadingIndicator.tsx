import { Sparkles, Database } from 'lucide-react';

interface LoadingIndicatorProps {
  message?: string;
}

export const LoadingIndicator: React.FC<LoadingIndicatorProps> = ({
  message = 'Agent consulting database & extracting SMART goals...',
}) => {
  return (
    <div className="bg-[#11141c] border border-cyan-800/40 rounded-2xl p-8 md:p-12 text-center max-w-xl mx-auto shadow-2xl relative overflow-hidden">
      {/* Background glow */}
      <div className="absolute -right-10 -top-10 w-40 h-40 bg-cyan-500/10 rounded-full blur-2xl pointer-events-none" />
      <div className="absolute -left-10 -bottom-10 w-40 h-40 bg-blue-500/10 rounded-full blur-2xl pointer-events-none" />

      <div className="relative z-10 flex flex-col items-center space-y-4">
        {/* Animated Spinners & Icons */}
        <div className="relative flex items-center justify-center">
          <div className="w-16 h-16 rounded-full border-2 border-cyan-500/20 border-t-cyan-400 animate-spin" />
          <Sparkles className="w-6 h-6 text-cyan-400 absolute animate-pulse" />
        </div>

        {/* Message and Sub-indicators */}
        <div className="space-y-2">
          <h4 className="text-base font-semibold text-white tracking-wide">
            {message}
          </h4>
          <div className="flex items-center justify-center gap-2 text-xs text-slate-400">
            <Database className="w-3.5 h-3.5 text-cyan-400" />
            <span>Querying active goals via read-only tools</span>
            <span className="text-slate-600">•</span>
            <span className="font-mono text-cyan-300">Gemini 2.5 Flash</span>
          </div>
        </div>

        {/* Pulsing skeleton bar */}
        <div className="w-48 h-1.5 bg-slate-800 rounded-full overflow-hidden mt-2">
          <div className="w-full h-full bg-gradient-to-r from-transparent via-cyan-400 to-transparent animate-[shimmer_1.5s_infinite]" />
        </div>
      </div>
    </div>
  );
};
