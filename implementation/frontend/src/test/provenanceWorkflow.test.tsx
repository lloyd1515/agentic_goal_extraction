import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import App from '../App';
import { goalService } from '../services/goalService';

vi.mock('../services/goalService', () => ({
  goalService: {
    checkHealth: vi.fn(),
    fetchEmployees: vi.fn(),
    extractGoals: vi.fn(),
    saveGoalsBatch: vi.fn(),
  },
}));

describe('Provenance Transitions and Full Review Canvas Workflow', () => {
  const mockEmployees = [
    {
      id: 'emp-elena-123',
      fullName: 'Elena Rostova',
      email: 'elena.rostova@acme.corp',
      department: 'Engineering',
    },
  ];

  const mockExtractionResult = {
    transcriptHash: 'abc123hash',
    summary: 'Discussion regarding performance benchmarks and microservice refactoring.',
    goals: [
      {
        tempId: 'temp-ai-1',
        title: 'Optimize Database Query Latency',
        description: 'Profile slow queries and reduce p95 latency under 50ms across core tables.',
        category: 'PERFORMANCE' as const,
        metric: 'p95 latency < 50ms',
        timeframe: 'Q4 2026',
        priority: 'HIGH' as const,
        provenance: 'AI_ORIGINAL' as const,
      },
    ],
  };

  beforeEach(() => {
    vi.clearAllMocks();
    (goalService.checkHealth as any).mockResolvedValue(true);
    (goalService.fetchEmployees as any).mockResolvedValue(mockEmployees);
    (goalService.extractGoals as any).mockResolvedValue(mockExtractionResult);
    (goalService.saveGoalsBatch as any).mockResolvedValue({
      success: true,
      savedCount: 2,
      savedGoalIds: ['goal-db-1', 'goal-db-2'],
    });
  });

  it('verifies AI_ORIGINAL -> AI_MODIFIED transition and MANUAL creation', async () => {
    render(<App />);

    // Wait for employees to load and default Elena Rostova to be selected
    await waitFor(() => {
      expect(screen.getByRole('option', { name: /Elena Rostova/i })).toBeInTheDocument();
    });

    // 1. Load sample transcript and trigger extraction
    const sampleBtn = screen.getByRole('button', { name: /load sample 1:1 discussion/i });
    fireEvent.click(sampleBtn);

    const extractBtn = screen.getByRole('button', { name: /extract goals with ai/i });
    await waitFor(() => {
      expect(extractBtn).not.toBeDisabled();
    });
    fireEvent.click(extractBtn);

    // 2. Goal extracted - verify AI_ORIGINAL badge
    await waitFor(() => {
      expect(screen.getByText('AI Suggested')).toBeInTheDocument();
    });
    expect(screen.getByDisplayValue('Optimize Database Query Latency')).toBeInTheDocument();

    // 3. Edit title - verify provenance transitions to AI_MODIFIED
    const titleInput = screen.getByDisplayValue('Optimize Database Query Latency');
    fireEvent.change(titleInput, {
      target: { value: 'Optimize Database Query Latency (Edited By Reviewer)' },
    });

    // Badge must now be "AI Suggested (Edited)"
    await waitFor(() => {
      expect(screen.getByText('AI Suggested (Edited)')).toBeInTheDocument();
    });
    expect(screen.queryByText('AI Suggested')).not.toBeInTheDocument();

    // 4. Add a manual goal
    const addGoalBtn = screen.getByRole('button', { name: /add goal/i });
    fireEvent.click(addGoalBtn);

    // Canvas now has 2 goals: 1st is AI_MODIFIED, 2nd is MANUAL
    await waitFor(() => {
      expect(screen.getByText('Manually Added')).toBeInTheDocument();
      expect(screen.getByText('2 Goals')).toBeInTheDocument();
    });

    // Fill in the manual goal fields so it becomes valid
    const titleInputs = screen.getAllByPlaceholderText('e.g. Migrate legacy auth to OAuth 2.1');
    const manualTitleInput = titleInputs[1];
    fireEvent.change(manualTitleInput, {
      target: { value: 'Implement Observability Dashboards' },
    });

    const descInputs = screen.getAllByPlaceholderText(
      'Detailed description of objective, context, and expected outcome...'
    );
    const manualDescInput = descInputs[1];
    fireEvent.change(manualDescInput, {
      target: { value: 'Set up Grafana alerts and Prometheus scrapers for production microservices.' },
    });

    // Provenance of manual goal remains MANUAL
    expect(screen.getByText('Manually Added')).toBeInTheDocument();
    expect(screen.getByText('AI Suggested (Edited)')).toBeInTheDocument();

    // 5. Save goals batch to database
    const saveBtn = screen.getByRole('button', { name: /save goals to database/i });
    expect(saveBtn).not.toBeDisabled();
    fireEvent.click(saveBtn);

    await waitFor(() => {
      expect(goalService.saveGoalsBatch).toHaveBeenCalledTimes(1);
    });

    const savePayload = (goalService.saveGoalsBatch as any).mock.calls[0][0];
    expect(savePayload.employeeId).toBe('emp-elena-123');
    expect(savePayload.sourceTranscriptHash).toBe('abc123hash');
    expect(savePayload.goals).toHaveLength(2);
    expect(savePayload.goals[0]).toMatchObject({
      title: 'Optimize Database Query Latency (Edited By Reviewer)',
      provenance: 'AI_MODIFIED',
    });
    expect(savePayload.goals[1]).toMatchObject({
      title: 'Implement Observability Dashboards',
      provenance: 'MANUAL',
    });

    // Verify confirmation toast after persistence
    await waitFor(() => {
      expect(
        screen.getByText(/Successfully persisted 2 goals to the database!/i)
      ).toBeInTheDocument();
    });

    // Review canvas should be reset after successful persistence
    expect(screen.getByText(/No Proposed Goals in Review Canvas/i)).toBeInTheDocument();
  });

  it('supports removing a goal and restoring via Undo toast', async () => {
    render(<App />);

    await waitFor(() => {
      expect(screen.getByRole('option', { name: /Elena Rostova/i })).toBeInTheDocument();
    });

    // Load sample and extract
    fireEvent.click(screen.getByRole('button', { name: /load sample 1:1 discussion/i }));
    const extractBtn = screen.getByRole('button', { name: /extract goals with ai/i });
    await waitFor(() => {
      expect(extractBtn).not.toBeDisabled();
    });
    fireEvent.click(extractBtn);

    await waitFor(() => {
      expect(screen.getByDisplayValue('Optimize Database Query Latency')).toBeInTheDocument();
    });

    // Click remove goal button
    const removeBtn = screen.getByRole('button', { name: /remove goal/i });
    fireEvent.click(removeBtn);

    // Goal should be removed from view
    await waitFor(() => {
      expect(screen.queryByDisplayValue('Optimize Database Query Latency')).not.toBeInTheDocument();
    });

    // Undo toast should appear
    const undoBtn = screen.getByRole('button', { name: /undo/i });
    expect(undoBtn).toBeInTheDocument();

    // Click Undo
    fireEvent.click(undoBtn);

    // Goal should be restored back to review canvas
    await waitFor(() => {
      expect(screen.getByDisplayValue('Optimize Database Query Latency')).toBeInTheDocument();
      expect(screen.getByText('AI Suggested')).toBeInTheDocument();
    });
  });
});
