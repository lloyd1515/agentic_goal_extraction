import React from 'react';
import { Sparkles, FileText, AlertCircle, RefreshCw } from 'lucide-react';

interface TranscriptInputProps {
  transcript: string;
  onChange: (value: string) => void;
  onExtract: () => void;
  isLoading: boolean;
  onLoadSample: () => void;
}

const MIN_LENGTH = 20;
const MAX_LENGTH = 32000;

export const SAMPLE_TRANSCRIPT = `Manager (Marcus Vance): Hi Elena, thanks for taking the time to sync today for our Q3 planning session. Let's look over your key technical and personal development goals for the second half of the year. How is the authentication architecture modernization progressing?

Elena Rostova: Thanks Marcus. Our legacy identity setup still relies on OAuth 1.0a and deprecated JWT flows, which poses both security and maintenance bottlenecks. My primary technical commitment for this period is to migrate our core identity provider and all internal microservices to OAuth 2.1 RFC specifications by the end of Q3 2026. This migration will enforce PKCE across all service-to-service communication.

Manager (Marcus Vance): That is vital for our SOC2 compliance. What metric will define success for this migration?

Elena Rostova: 100% of internal services transitioned with zero authentication downtime during deployments, validated by automated synthetic monitors.

Manager (Marcus Vance): Fantastic. Turning to engineering practices, our automated test coverage in the billing service has plateaued at 70%. Can we establish a measurable target there?

Elena Rostova: Absolutely. I will raise our integration and unit test coverage to 85% by the end of Q4 2026, specifically targeting the payment processing and invoice reconciliation modules.

Manager (Marcus Vance): Outstanding. On the leadership and team growth side, we have three junior engineers joining next month. Would you be open to mentoring them?

Elena Rostova: I would love to. I plan to conduct bi-weekly architecture walkthroughs and pair-programming sessions throughout Q3 and Q4, guiding all three junior engineers to independently deliver their first production PRs within 60 days of onboarding.`;

export const TranscriptInput: React.FC<TranscriptInputProps> = ({
  transcript,
  onChange,
  onExtract,
  isLoading,
  onLoadSample,
}) => {
  const trimmed = transcript.trim();
  const charCount = transcript.length;

  let validationError: string | null = null;
  if (transcript.length > 0 && trimmed.length < MIN_LENGTH) {
    validationError = `Transcript must be at least ${MIN_LENGTH} characters (currently ${trimmed.length}).`;
  } else if (charCount > MAX_LENGTH) {
    validationError = `Transcript exceeds maximum limit of ${MAX_LENGTH.toLocaleString()} characters (currently ${charCount.toLocaleString()}).`;
  }

  const isValid = trimmed.length >= MIN_LENGTH && charCount <= MAX_LENGTH;

  return (
    <div className="bg-[#11141c] border border-slate-800 rounded-xl p-5 shadow-lg space-y-4">
      {/* Card Header & Preset Action */}
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-800/80 pb-3">
        <div className="flex items-center space-x-2">
          <FileText className="w-4 h-4 text-cyan-400" />
          <h2 className="text-sm font-semibold text-slate-100 tracking-wide uppercase">
            1:1 Meeting Transcript
          </h2>
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={onLoadSample}
            disabled={isLoading}
            className="text-xs px-3 py-1.5 rounded-lg font-medium bg-[#161b26] hover:bg-slate-800 text-cyan-400 hover:text-cyan-300 border border-cyan-800/40 hover:border-cyan-700 transition flex items-center gap-1.5 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <RefreshCw className="w-3 h-3" />
            Load Sample 1:1 Discussion
          </button>
        </div>
      </div>

      {/* Textarea Input */}
      <div className="relative">
        <textarea
          aria-label="Discussion Transcript"
          value={transcript}
          onChange={(e) => onChange(e.target.value)}
          placeholder="Paste manager-employee 1:1 conversation transcript or performance review notes here..."
          rows={7}
          disabled={isLoading}
          className={`w-full bg-[#161b26] text-slate-100 border rounded-lg p-3.5 text-sm font-normal focus:outline-none transition resize-y font-mono leading-relaxed placeholder:text-slate-500 ${
            validationError
              ? 'border-rose-500/70 focus:border-rose-400 ring-1 ring-rose-500/30'
              : 'border-slate-800 focus:border-cyan-500/70 focus:ring-1 focus:ring-cyan-500/30'
          }`}
        />
      </div>

      {/* Footer: Character Counter, Error, and Extract Button */}
      <div className="flex flex-wrap items-center justify-between gap-3 pt-1">
        <div className="flex items-center space-x-3 text-xs">
          <span
            className={`font-mono ${
              charCount > MAX_LENGTH
                ? 'text-rose-400 font-semibold'
                : charCount > 0 && trimmed.length < MIN_LENGTH
                ? 'text-amber-400'
                : 'text-slate-400'
            }`}
          >
            {charCount.toLocaleString()} / {MAX_LENGTH.toLocaleString()} characters
          </span>

          {validationError && (
            <div className="flex items-center space-x-1 text-rose-400 text-xs">
              <AlertCircle className="w-3.5 h-3.5 shrink-0" />
              <span>{validationError}</span>
            </div>
          )}
        </div>

        <button
          type="button"
          onClick={onExtract}
          disabled={!isValid || isLoading}
          className={`px-4 py-2 rounded-lg text-xs font-semibold uppercase tracking-wider flex items-center gap-2 transition shadow-md ${
            !isValid || isLoading
              ? 'bg-slate-800 text-slate-500 cursor-not-allowed border border-slate-700/50'
              : 'bg-gradient-to-r from-cyan-500 to-blue-600 hover:from-cyan-400 hover:to-blue-500 text-white shadow-cyan-500/20 active:scale-[0.98]'
          }`}
        >
          {isLoading ? (
            <>
              <RefreshCw className="w-3.5 h-3.5 animate-spin" />
              <span>Extracting Goals...</span>
            </>
          ) : (
            <>
              <Sparkles className="w-3.5 h-3.5" />
              <span>Extract Goals with AI</span>
            </>
          )}
        </button>
      </div>
    </div>
  );
};
