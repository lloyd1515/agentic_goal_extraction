import { useEffect } from 'react';
import { CheckCircle2, XCircle, RotateCcw, X, Info } from 'lucide-react';

export interface ToastItem {
  id: string;
  type: 'success' | 'error' | 'info';
  message: string;
  undoAction?: () => void;
  undoLabel?: string;
  duration?: number;
}

interface ToastProps {
  toasts: ToastItem[];
  onDismiss: (id: string) => void;
}

export const Toast: React.FC<ToastProps> = ({ toasts, onDismiss }) => {
  if (toasts.length === 0) return null;

  return (
    <div
      aria-live="polite"
      className="fixed bottom-6 right-6 z-50 flex flex-col gap-2.5 max-w-md w-full px-4 sm:px-0 pointer-events-none"
    >
      {toasts.map((toast) => (
        <ToastCard key={toast.id} toast={toast} onDismiss={() => onDismiss(toast.id)} />
      ))}
    </div>
  );
};

const ToastCard: React.FC<{ toast: ToastItem; onDismiss: () => void }> = ({
  toast,
  onDismiss,
}) => {
  useEffect(() => {
    const duration = toast.duration ?? (toast.undoAction ? 8000 : 5000);
    const timer = setTimeout(() => {
      onDismiss();
    }, duration);

    return () => clearTimeout(timer);
  }, [toast, onDismiss]);

  const getStyles = () => {
    switch (toast.type) {
      case 'success':
        return {
          cardBg: 'bg-[#121c18] border-emerald-800/70 text-emerald-100',
          icon: <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0" />,
        };
      case 'error':
        return {
          cardBg: 'bg-[#221316] border-rose-800/70 text-rose-100',
          icon: <XCircle className="w-5 h-5 text-rose-400 shrink-0" />,
        };
      case 'info':
      default:
        return {
          cardBg: 'bg-[#141b28] border-cyan-800/70 text-cyan-100',
          icon: <Info className="w-5 h-5 text-cyan-400 shrink-0" />,
        };
    }
  };

  const { cardBg, icon } = getStyles();

  return (
    <div
      className={`pointer-events-auto border rounded-xl p-3.5 shadow-2xl flex items-start justify-between gap-3 backdrop-blur-md transition-all animate-in slide-in-from-bottom-3 duration-200 ${cardBg}`}
    >
      <div className="flex items-start gap-2.5 flex-1 min-w-0">
        {icon}
        <div className="flex-1 min-w-0 pt-0.5">
          <p className="text-xs font-medium leading-snug break-words text-slate-100">
            {toast.message}
          </p>

          {toast.undoAction && (
            <button
              type="button"
              onClick={() => {
                toast.undoAction?.();
                onDismiss();
              }}
              className="mt-2 text-xs font-semibold px-2.5 py-1 rounded bg-amber-500/20 hover:bg-amber-500/30 text-amber-300 border border-amber-500/40 hover:border-amber-400 flex items-center gap-1 transition"
            >
              <RotateCcw className="w-3 h-3" />
              {toast.undoLabel || 'Undo'}
            </button>
          )}
        </div>
      </div>

      <button
        type="button"
        onClick={onDismiss}
        className="text-slate-400 hover:text-white p-1 rounded-lg hover:bg-white/10 transition shrink-0"
        aria-label="Dismiss Notification"
      >
        <X className="w-3.5 h-3.5" />
      </button>
    </div>
  );
};
