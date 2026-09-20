import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { GoalCard } from '../components/GoalCard';
import { EditableGoal } from '../types/goal';

describe('GoalCard Component', () => {
  const sampleGoal: EditableGoal = {
    tempId: 'temp-1',
    localId: 'local-1',
    title: 'Migrate Database Schemas',
    description: 'Perform complete database schema migration to PostgreSQL 16.',
    category: 'TECHNICAL',
    priority: 'HIGH',
    metric: 'Zero downtime during migration',
    timeframe: 'End of Q3',
    provenance: 'AI_ORIGINAL',
    isEdited: false,
    validationErrors: {},
  };

  describe('Provenance Badge rendering', () => {
    it('renders "AI Suggested" badge for AI_ORIGINAL provenance', () => {
      render(
        <GoalCard
          goal={{ ...sampleGoal, provenance: 'AI_ORIGINAL' }}
          index={0}
          onChange={vi.fn()}
          onRemove={vi.fn()}
        />
      );

      expect(screen.getByText('AI Suggested')).toBeInTheDocument();
      expect(screen.getByText('#1')).toBeInTheDocument();
    });

    it('renders "AI Suggested (Edited)" badge for AI_MODIFIED provenance', () => {
      render(
        <GoalCard
          goal={{ ...sampleGoal, provenance: 'AI_MODIFIED' }}
          index={1}
          onChange={vi.fn()}
          onRemove={vi.fn()}
        />
      );

      expect(screen.getByText('AI Suggested (Edited)')).toBeInTheDocument();
      expect(screen.getByText('#2')).toBeInTheDocument();
    });

    it('renders "Manually Added" badge for MANUAL provenance', () => {
      render(
        <GoalCard
          goal={{ ...sampleGoal, provenance: 'MANUAL' }}
          index={2}
          onChange={vi.fn()}
          onRemove={vi.fn()}
        />
      );

      expect(screen.getByText('Manually Added')).toBeInTheDocument();
      expect(screen.getByText('#3')).toBeInTheDocument();
    });
  });

  describe('Editing and Provenance Transition', () => {
    it('transitions AI_ORIGINAL to AI_MODIFIED when user edits title', () => {
      const handleChange = vi.fn();
      render(
        <GoalCard
          goal={{ ...sampleGoal, provenance: 'AI_ORIGINAL' }}
          index={0}
          onChange={handleChange}
          onRemove={vi.fn()}
        />
      );

      const titleInput = screen.getByPlaceholderText('e.g. Migrate legacy auth to OAuth 2.1');
      fireEvent.change(titleInput, { target: { value: 'Updated Title Exceeding 5 Chars' } });

      expect(handleChange).toHaveBeenCalledTimes(1);
      expect(handleChange).toHaveBeenCalledWith(
        'local-1',
        expect.objectContaining({
          title: 'Updated Title Exceeding 5 Chars',
          isEdited: true,
          provenance: 'AI_MODIFIED',
        })
      );
    });

    it('transitions AI_ORIGINAL to AI_MODIFIED when user edits description', () => {
      const handleChange = vi.fn();
      render(
        <GoalCard
          goal={{ ...sampleGoal, provenance: 'AI_ORIGINAL' }}
          index={0}
          onChange={handleChange}
          onRemove={vi.fn()}
        />
      );

      const descInput = screen.getByPlaceholderText(
        'Detailed description of objective, context, and expected outcome...'
      );
      fireEvent.change(descInput, {
        target: { value: 'New updated description that is long enough to pass validation' },
      });

      expect(handleChange).toHaveBeenCalledTimes(1);
      expect(handleChange).toHaveBeenCalledWith(
        'local-1',
        expect.objectContaining({
          description: 'New updated description that is long enough to pass validation',
          isEdited: true,
          provenance: 'AI_MODIFIED',
        })
      );
    });

    it('keeps MANUAL provenance when editing a manually added goal', () => {
      const handleChange = vi.fn();
      render(
        <GoalCard
          goal={{ ...sampleGoal, provenance: 'MANUAL' }}
          index={0}
          onChange={handleChange}
          onRemove={vi.fn()}
        />
      );

      const titleInput = screen.getByPlaceholderText('e.g. Migrate legacy auth to OAuth 2.1');
      fireEvent.change(titleInput, { target: { value: 'Manual Goal Title Edited' } });

      expect(handleChange).toHaveBeenCalledWith(
        'local-1',
        expect.objectContaining({
          provenance: 'MANUAL',
          isEdited: true,
        })
      );
    });
  });

  describe('Inline Validation Feedback', () => {
    it('computes validationErrors when title is changed to under 5 characters', () => {
      const handleChange = vi.fn();
      render(
        <GoalCard
          goal={sampleGoal}
          index={0}
          onChange={handleChange}
          onRemove={vi.fn()}
        />
      );

      const titleInput = screen.getByPlaceholderText('e.g. Migrate legacy auth to OAuth 2.1');
      fireEvent.change(titleInput, { target: { value: 'Tiny' } });

      expect(handleChange).toHaveBeenCalledWith(
        'local-1',
        expect.objectContaining({
          title: 'Tiny',
          validationErrors: expect.objectContaining({
            title: 'Title must be between 5 and 120 characters.',
          }),
        })
      );
    });

    it('displays error messages when goal contains validationErrors in props', () => {
      const invalidGoal: EditableGoal = {
        ...sampleGoal,
        validationErrors: {
          title: 'Title must be between 5 and 120 characters.',
          description: 'Description must be between 10 and 1000 characters.',
        },
      };

      render(
        <GoalCard
          goal={invalidGoal}
          index={0}
          onChange={vi.fn()}
          onRemove={vi.fn()}
        />
      );

      expect(screen.getByText('Title must be between 5 and 120 characters.')).toBeInTheDocument();
      expect(screen.getByText('Description must be between 10 and 1000 characters.')).toBeInTheDocument();
    });
  });

  describe('Card Removal Action', () => {
    it('calls onRemove when the trash icon button is clicked', () => {
      const handleRemove = vi.fn();
      render(
        <GoalCard
          goal={sampleGoal}
          index={0}
          onChange={vi.fn()}
          onRemove={handleRemove}
        />
      );

      const removeBtn = screen.getByRole('button', { name: /remove goal/i });
      fireEvent.click(removeBtn);

      expect(handleRemove).toHaveBeenCalledTimes(1);
      expect(handleRemove).toHaveBeenCalledWith('local-1');
    });
  });

  describe('Keyboard Navigation Revert on Escape (AC 5.2)', () => {
    it('reverts title to initial/previous value when Escape is pressed', () => {
      const handleChange = vi.fn();
      render(
        <GoalCard
          goal={sampleGoal}
          index={0}
          onChange={handleChange}
          onRemove={vi.fn()}
        />
      );

      const titleInput = screen.getByPlaceholderText('e.g. Migrate legacy auth to OAuth 2.1');
      fireEvent.focus(titleInput);
      fireEvent.change(titleInput, { target: { value: 'New Invalid' } });

      fireEvent.keyDown(titleInput, { key: 'Escape' });

      expect(handleChange).toHaveBeenCalledWith(
        'local-1',
        expect.objectContaining({
          title: 'Migrate Database Schemas',
          validationErrors: expect.not.objectContaining({ title: expect.anything() }),
        })
      );
    });

    it('reverts description to previous value when Escape is pressed', () => {
      const handleChange = vi.fn();
      render(
        <GoalCard
          goal={sampleGoal}
          index={0}
          onChange={handleChange}
          onRemove={vi.fn()}
        />
      );

      const descInput = screen.getByPlaceholderText(
        'Detailed description of objective, context, and expected outcome...'
      );
      fireEvent.focus(descInput);
      fireEvent.change(descInput, { target: { value: 'Short' } });

      fireEvent.keyDown(descInput, { key: 'Escape' });

      expect(handleChange).toHaveBeenCalledWith(
        'local-1',
        expect.objectContaining({
          description: 'Perform complete database schema migration to PostgreSQL 16.',
        })
      );
    });
  });
});
