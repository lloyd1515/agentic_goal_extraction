import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { ReviewCanvas } from '../components/ReviewCanvas';
import { EditableGoal } from '../types/goal';

describe('ReviewCanvas Component', () => {
  const validGoal1: EditableGoal = {
    tempId: 'g1',
    localId: 'local-g1',
    title: 'Establish CI/CD Pipeline',
    description: 'Implement GitHub Actions workflow with zero flaky tests.',
    category: 'TECHNICAL',
    priority: 'HIGH',
    metric: '100% build pass rate',
    timeframe: 'Month 1',
    provenance: 'AI_ORIGINAL',
    isEdited: false,
    validationErrors: {},
  };

  const validGoal2: EditableGoal = {
    tempId: 'g2',
    localId: 'local-g2',
    title: 'Lead Mentorship Sessions',
    description: 'Mentor junior software engineers weekly on clean code design.',
    category: 'DEVELOPMENT',
    priority: 'MEDIUM',
    metric: 'Bi-weekly 1:1 check-ins',
    timeframe: 'Ongoing Q3',
    provenance: 'MANUAL',
    isEdited: false,
    validationErrors: {},
  };

  it('renders summary box when summary prop is provided', () => {
    render(
      <ReviewCanvas
        summary="Elena discussed containerization and performance benchmarks."
        goals={[validGoal1]}
        onUpdateGoal={vi.fn()}
        onRemoveGoal={vi.fn()}
        onAddGoal={vi.fn()}
        onSaveBatch={vi.fn()}
        isSaving={false}
      />
    );

    expect(screen.getByText(/Discussion Summary & Historical Context/i)).toBeInTheDocument();
    expect(
      screen.getByText('Elena discussed containerization and performance benchmarks.')
    ).toBeInTheDocument();
  });

  it('does not render summary box when summary is null', () => {
    render(
      <ReviewCanvas
        summary={null}
        goals={[validGoal1]}
        onUpdateGoal={vi.fn()}
        onRemoveGoal={vi.fn()}
        onAddGoal={vi.fn()}
        onSaveBatch={vi.fn()}
        isSaving={false}
      />
    );

    expect(screen.queryByText(/Discussion Summary & Historical Context/i)).not.toBeInTheDocument();
  });

  it('displays the target employee name and goal count badge', () => {
    render(
      <ReviewCanvas
        summary={null}
        goals={[validGoal1, validGoal2]}
        onUpdateGoal={vi.fn()}
        onRemoveGoal={vi.fn()}
        onAddGoal={vi.fn()}
        onSaveBatch={vi.fn()}
        isSaving={false}
        selectedEmployeeName="Elena Rostova"
      />
    );

    expect(screen.getByText('Elena Rostova')).toBeInTheDocument();
    expect(screen.getByText('2 Goals')).toBeInTheDocument();
  });

  it('calls onAddGoal when clicking "Add Goal" button', () => {
    const handleAdd = vi.fn();
    render(
      <ReviewCanvas
        summary={null}
        goals={[validGoal1]}
        onUpdateGoal={vi.fn()}
        onRemoveGoal={vi.fn()}
        onAddGoal={handleAdd}
        onSaveBatch={vi.fn()}
        isSaving={false}
      />
    );

    const addBtn = screen.getByRole('button', { name: /add goal/i });
    fireEvent.click(addBtn);

    expect(handleAdd).toHaveBeenCalledTimes(1);
  });

  it('enables save button and invokes onSaveBatch when clicked for valid goals', () => {
    const handleSave = vi.fn();
    render(
      <ReviewCanvas
        summary={null}
        goals={[validGoal1]}
        onUpdateGoal={vi.fn()}
        onRemoveGoal={vi.fn()}
        onAddGoal={vi.fn()}
        onSaveBatch={handleSave}
        isSaving={false}
      />
    );

    const saveBtn = screen.getByRole('button', { name: /save goals to database/i });
    expect(saveBtn).not.toBeDisabled();
    expect(saveBtn).toHaveAttribute('title', 'Save goals to database');

    fireEvent.click(saveBtn);
    expect(handleSave).toHaveBeenCalledTimes(1);
  });

  it('disables save button and displays validation warning when any goal has errors', () => {
    const invalidGoal: EditableGoal = {
      ...validGoal1,
      validationErrors: { title: 'Title must be between 5 and 120 characters.' },
    };

    render(
      <ReviewCanvas
        summary={null}
        goals={[invalidGoal]}
        onUpdateGoal={vi.fn()}
        onRemoveGoal={vi.fn()}
        onAddGoal={vi.fn()}
        onSaveBatch={vi.fn()}
        isSaving={false}
      />
    );

    const saveBtn = screen.getByRole('button', { name: /save goals to database/i });
    expect(saveBtn).toBeDisabled();
    expect(saveBtn).toHaveAttribute('title', 'Resolve all field errors before saving');

    expect(
      screen.getByText(/goal has validation errors\. Please resolve them to enable database persistence\./i)
    ).toBeInTheDocument();
    expect(screen.getByText('1')).toBeInTheDocument();
  });

  it('disables save button when isSaving is true', () => {
    render(
      <ReviewCanvas
        summary={null}
        goals={[validGoal1]}
        onUpdateGoal={vi.fn()}
        onRemoveGoal={vi.fn()}
        onAddGoal={vi.fn()}
        onSaveBatch={vi.fn()}
        isSaving={true}
      />
    );

    expect(screen.getByText('Saving Batch...')).toBeInTheDocument();
    const saveBtn = screen.getByRole('button', { name: /saving batch.../i });
    expect(saveBtn).toBeDisabled();
  });

  it('renders bottom save bar when there are 2 or more goals', () => {
    render(
      <ReviewCanvas
        summary={null}
        goals={[validGoal1, validGoal2]}
        onUpdateGoal={vi.fn()}
        onRemoveGoal={vi.fn()}
        onAddGoal={vi.fn()}
        onSaveBatch={vi.fn()}
        isSaving={false}
      />
    );

    expect(screen.getByRole('button', { name: /add another goal/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /save all \(2\) goals/i })).toBeInTheDocument();
  });
});
