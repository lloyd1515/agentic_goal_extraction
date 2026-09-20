import { useState, useEffect, useCallback } from 'react';
import { Header } from './components/Header';
import { TranscriptInput, SAMPLE_TRANSCRIPT } from './components/TranscriptInput';
import { ReviewCanvas } from './components/ReviewCanvas';
import { EmptyState } from './components/EmptyState';
import { LoadingIndicator } from './components/LoadingIndicator';
import { Toast, ToastItem } from './components/Toast';
import { EditableGoal, Employee, ProposedGoal, validateGoal } from './types/goal';
import { goalService } from './services/goalService';
import { ApiError } from './services/apiClient';

export default function App() {
  // Application State
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [selectedEmployeeId, setSelectedEmployeeId] = useState<string>('');
  const [isLoadingEmployees, setIsLoadingEmployees] = useState<boolean>(true);
  const [backendHealth, setBackendHealth] = useState<'checking' | 'healthy' | 'offline'>('checking');

  const [transcript, setTranscript] = useState<string>('');
  const [transcriptHash, setTranscriptHash] = useState<string>('');
  const [summary, setSummary] = useState<string | null>(null);
  const [goals, setGoals] = useState<EditableGoal[]>([]);

  const [isExtracting, setIsExtracting] = useState<boolean>(false);
  const [isSaving, setIsSaving] = useState<boolean>(false);
  const [toasts, setToasts] = useState<ToastItem[]>([]);

  // Toast Management
  const addToast = useCallback((toast: Omit<ToastItem, 'id'> & { id?: string }) => {
    const id = toast.id || (typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : Math.random().toString());
    setToasts((prev) => [...prev, { ...toast, id }]);
  }, []);

  const dismissToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  // Fetch Health Status & Seeded Employees on mount
  useEffect(() => {
    let isMounted = true;

    // Check Backend Health
    goalService
      .checkHealth()
      .then((isHealthy) => {
        if (isMounted) {
          setBackendHealth(isHealthy ? 'healthy' : 'offline');
        }
      })
      .catch(() => {
        if (isMounted) setBackendHealth('offline');
      });

    // Fetch Employees
    goalService
      .fetchEmployees()
      .then((data) => {
        if (isMounted) {
          setEmployees(data);
          // Default to Elena Rostova if present, otherwise first employee
          const elena = data.find((e) => e.fullName.toLowerCase().includes('elena'));
          if (elena) {
            setSelectedEmployeeId(elena.id);
          } else if (data.length > 0) {
            setSelectedEmployeeId(data[0].id);
          }
          setIsLoadingEmployees(false);
        }
      })
      .catch((err) => {
        if (isMounted) {
          setIsLoadingEmployees(false);
          addToast({
            type: 'error',
            message: `Could not load employees: ${err.message}`,
          });
        }
      });

    return () => {
      isMounted = false;
    };
  }, [addToast]);

  // Load Sample Discussion Preset
  const handleLoadSample = () => {
    setTranscript(SAMPLE_TRANSCRIPT);
    // Auto-select Elena Rostova if available
    const elena = employees.find((e) => e.fullName.toLowerCase().includes('elena'));
    if (elena) {
      setSelectedEmployeeId(elena.id);
    }
    addToast({
      type: 'info',
      message: 'Loaded sample 1:1 conversation for Elena Rostova.',
    });
  };

  // AI Extraction Action
  const handleExtractGoals = async () => {
    if (!transcript.trim() || transcript.trim().length < 20) {
      addToast({
        type: 'error',
        message: 'Transcript must be at least 20 characters before extracting.',
      });
      return;
    }

    const currentEmployee = employees.find((e) => e.id === selectedEmployeeId);

    setIsExtracting(true);
    try {
      const result = await goalService.extractGoals({
        transcript: transcript.trim(),
        context: {
          employeeId: selectedEmployeeId || undefined,
          department: currentEmployee?.department || undefined,
        },
      });

      setTranscriptHash(result.transcriptHash);
      setSummary(result.summary || null);

      // Transform proposed goals to editable goals
      const mappedEditableGoals: EditableGoal[] = (result.goals || []).map((g: ProposedGoal, idx: number) => {
        const localId = `goal-${Date.now()}-${idx}`;
        const validationErrors = validateGoal(g);
        return {
          ...g,
          localId,
          isEdited: false,
          validationErrors,
        };
      });

      setGoals(mappedEditableGoals);

      addToast({
        type: 'success',
        message: `Extracted ${mappedEditableGoals.length} SMART goals successfully!`,
      });
    } catch (err: unknown) {
      const message = err instanceof ApiError ? err.message : 'An error occurred during goal extraction.';
      addToast({
        type: 'error',
        message: `Goal Extraction Failed: ${message}`,
      });
    } finally {
      setIsExtracting(false);
    }
  };

  // Update Goal Field in Review Canvas
  const handleUpdateGoal = (localId: string, updatedFields: Partial<EditableGoal>) => {
    setGoals((prev) =>
      prev.map((g) => {
        if (g.localId !== localId) return g;
        return {
          ...g,
          ...updatedFields,
        };
      })
    );
  };

  // Add a Manual Goal Card
  const handleAddGoal = () => {
    const localId = `manual-${Date.now()}`;
    const newGoal: EditableGoal = {
      tempId: localId,
      localId,
      title: '',
      description: '',
      category: 'PERFORMANCE',
      metric: '',
      timeframe: '',
      priority: 'MEDIUM',
      provenance: 'MANUAL',
      isEdited: false,
      validationErrors: {
        title: 'Title cannot be empty.',
        description: 'Description cannot be empty.',
      },
    };

    setGoals((prev) => [...prev, newGoal]);
  };

  // Remove Goal with Undo Support
  const handleRemoveGoal = (localId: string) => {
    const removedIndex = goals.findIndex((g) => g.localId === localId);
    if (removedIndex === -1) return;

    const removedGoal = goals[removedIndex];
    const goalTitle = removedGoal.title.trim() || 'Untitled Goal';

    // Remove from active list
    setGoals((prev) => prev.filter((g) => g.localId !== localId));

    // Offer Undo via Toast
    addToast({
      type: 'info',
      message: `Goal "${goalTitle}" removed.`,
      undoLabel: 'Undo',
      duration: 7000,
      undoAction: () => {
        setGoals((prev) => {
          const restored = [...prev];
          restored.splice(Math.min(removedIndex, restored.length), 0, removedGoal);
          return restored;
        });
        addToast({
          type: 'success',
          message: `Restored goal "${goalTitle}".`,
        });
      },
    });
  };

  // Save Batch of Goals to Database
  const handleSaveBatch = async () => {
    if (!selectedEmployeeId) {
      addToast({
        type: 'error',
        message: 'Please select an employee before saving goals.',
      });
      return;
    }

    if (goals.length === 0) {
      addToast({
        type: 'error',
        message: 'There are no goals to save.',
      });
      return;
    }

    // Check for any validation errors
    const hasErrors = goals.some((g) => Object.keys(g.validationErrors || {}).length > 0);
    if (hasErrors) {
      addToast({
        type: 'error',
        message: 'Cannot save: Please fix validation errors on highlighted goal cards.',
      });
      return;
    }

    setIsSaving(true);
    try {
      const payload = {
        employeeId: selectedEmployeeId,
        sourceTranscriptHash: transcriptHash || undefined,
        goals: goals.map((g) => ({
          title: g.title.trim(),
          description: g.description.trim(),
          category: g.category,
          metric: (g.metric || '').trim(),
          timeframe: (g.timeframe || '').trim(),
          priority: g.priority,
          provenance: g.provenance,
        })),
      };

      const result = await goalService.saveGoalsBatch(payload);

      addToast({
        type: 'success',
        message: `Successfully persisted ${result.savedCount} goals to the database!`,
        duration: 7000,
      });

      // Clear or reset canvas after successful persistence
      setGoals([]);
      setSummary(null);
      setTranscriptHash('');
    } catch (err: unknown) {
      const message = err instanceof ApiError ? err.message : 'Failed to save goals batch.';
      addToast({
        type: 'error',
        message: `Database Save Failed: ${message}`,
      });
    } finally {
      setIsSaving(false);
    }
  };

  const selectedEmployee = employees.find((e) => e.id === selectedEmployeeId);

  return (
    <div className="min-h-screen bg-[#0a0c10] text-slate-100 flex flex-col font-sans selection:bg-cyan-500 selection:text-black">
      {/* Header Bar */}
      <Header
        employees={employees}
        selectedEmployeeId={selectedEmployeeId}
        onSelectEmployee={setSelectedEmployeeId}
        healthStatus={backendHealth}
        isLoadingEmployees={isLoadingEmployees}
      />

      {/* Main Container */}
      <main className="flex-1 max-w-6xl w-full mx-auto p-4 sm:p-6 lg:p-8 space-y-8">
        {/* Transcript Ingestion Input Section */}
        <section aria-label="Transcript Input">
          <TranscriptInput
            transcript={transcript}
            onChange={setTranscript}
            onExtract={handleExtractGoals}
            isLoading={isExtracting}
            onLoadSample={handleLoadSample}
          />
        </section>

        {/* Dynamic Content Area: Loading / Canvas / Empty State */}
        <section aria-label="Goals Review Canvas" className="pt-2">
          {isExtracting ? (
            <LoadingIndicator message="Agent consulting database & extracting SMART goals..." />
          ) : goals.length > 0 ? (
            <ReviewCanvas
              summary={summary}
              goals={goals}
              onUpdateGoal={handleUpdateGoal}
              onRemoveGoal={handleRemoveGoal}
              onAddGoal={handleAddGoal}
              onSaveBatch={handleSaveBatch}
              isSaving={isSaving}
              selectedEmployeeName={selectedEmployee?.fullName}
            />
          ) : (
            <EmptyState onLoadSample={handleLoadSample} />
          )}
        </section>
      </main>

      {/* Footer */}
      <footer className="border-t border-slate-800/60 bg-[#0d1017] py-4 px-6 text-center text-xs text-slate-500">
        <p>
          Agentic Goal Extraction &amp; Persistence Canvas • CQRS Architecture with Zero-Write AI Isolation
        </p>
      </footer>

      {/* Toast Notifications Overlay */}
      <Toast toasts={toasts} onDismiss={dismissToast} />
    </div>
  );
}
