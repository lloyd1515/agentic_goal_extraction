import { describe, it, expect } from 'vitest';
import { validateGoal, ProposedGoal } from '../types/goal';

describe('validateGoal', () => {
  const baseValidGoal: Partial<ProposedGoal> = {
    title: 'Valid Goal Title Here',
    description: 'This is a valid goal description with plenty of characters.',
    metric: '95% code coverage',
    timeframe: 'Q4 2026',
    category: 'TECHNICAL',
    priority: 'HIGH',
    provenance: 'AI_ORIGINAL',
  };

  describe('Title validation', () => {
    it('passes when title length is between 5 and 120 characters', () => {
      // Minimum boundary: 5 characters
      const exact5 = { ...baseValidGoal, title: '12345' };
      expect(validateGoal(exact5).title).toBeUndefined();

      // Maximum boundary: 120 characters
      const exact120 = { ...baseValidGoal, title: 'A'.repeat(120) };
      expect(validateGoal(exact120).title).toBeUndefined();

      // Typical title
      const typical = { ...baseValidGoal, title: 'Migrate DB to PostgreSQL' };
      expect(validateGoal(typical).title).toBeUndefined();
    });

    it('fails when title is empty or only whitespace', () => {
      expect(validateGoal({ ...baseValidGoal, title: '' }).title).toBe('Title cannot be empty.');
      expect(validateGoal({ ...baseValidGoal, title: '   ' }).title).toBe('Title cannot be empty.');
      expect(validateGoal({ ...baseValidGoal, title: undefined }).title).toBe('Title cannot be empty.');
    });

    it('fails when title is shorter than 5 characters (< 5 chars)', () => {
      // 4 characters
      const fourChars = { ...baseValidGoal, title: 'ABCD' };
      const errors = validateGoal(fourChars);
      expect(errors.title).toBe('Title must be between 5 and 120 characters.');

      // 1 character
      const oneChar = { ...baseValidGoal, title: 'A' };
      expect(validateGoal(oneChar).title).toBe('Title must be between 5 and 120 characters.');

      // 4 characters after trimming
      const paddedFour = { ...baseValidGoal, title: '  ABCD  ' };
      expect(validateGoal(paddedFour).title).toBe('Title must be between 5 and 120 characters.');
    });

    it('fails when title is longer than 120 characters (> 120 chars)', () => {
      const char121 = { ...baseValidGoal, title: 'A'.repeat(121) };
      const errors = validateGoal(char121);
      expect(errors.title).toBe('Title must be between 5 and 120 characters.');

      const char200 = { ...baseValidGoal, title: 'A'.repeat(200) };
      expect(validateGoal(char200).title).toBe('Title must be between 5 and 120 characters.');
    });
  });

  describe('Description validation', () => {
    it('passes when description length is between 10 and 1000 characters', () => {
      // Minimum boundary: 10 characters
      const exact10 = { ...baseValidGoal, description: '1234567890' };
      expect(validateGoal(exact10).description).toBeUndefined();

      // Maximum boundary: 1000 characters
      const exact1000 = { ...baseValidGoal, description: 'D'.repeat(1000) };
      expect(validateGoal(exact1000).description).toBeUndefined();

      // Typical description
      const typical = {
        ...baseValidGoal,
        description: 'Complete the redesign of the user authentication workflow.',
      };
      expect(validateGoal(typical).description).toBeUndefined();
    });

    it('fails when description is empty or only whitespace', () => {
      expect(validateGoal({ ...baseValidGoal, description: '' }).description).toBe(
        'Description cannot be empty.'
      );
      expect(validateGoal({ ...baseValidGoal, description: '    \n  ' }).description).toBe(
        'Description cannot be empty.'
      );
      expect(validateGoal({ ...baseValidGoal, description: undefined }).description).toBe(
        'Description cannot be empty.'
      );
    });

    it('fails when description is shorter than 10 characters (< 10 chars)', () => {
      // 9 characters
      const nineChars = { ...baseValidGoal, description: '123456789' };
      const errors = validateGoal(nineChars);
      expect(errors.description).toBe('Description must be between 10 and 1000 characters.');

      // 9 characters after trimming
      const paddedNine = { ...baseValidGoal, description: '   123456789   ' };
      expect(validateGoal(paddedNine).description).toBe('Description must be between 10 and 1000 characters.');
    });

    it('fails when description is longer than 1000 characters (> 1000 chars)', () => {
      const char1001 = { ...baseValidGoal, description: 'D'.repeat(1001) };
      const errors = validateGoal(char1001);
      expect(errors.description).toBe('Description must be between 10 and 1000 characters.');
    });
  });

  describe('Metric and Timeframe optional constraints', () => {
    it('passes when metric and timeframe are empty or within limit', () => {
      const noOptionals = { ...baseValidGoal, metric: '', timeframe: '' };
      expect(Object.keys(validateGoal(noOptionals))).toHaveLength(0);

      const metric500 = { ...baseValidGoal, metric: 'M'.repeat(500) };
      expect(validateGoal(metric500).metric).toBeUndefined();

      const tf100 = { ...baseValidGoal, timeframe: 'T'.repeat(100) };
      expect(validateGoal(tf100).timeframe).toBeUndefined();
    });

    it('fails when metric exceeds 500 characters', () => {
      const metric501 = { ...baseValidGoal, metric: 'M'.repeat(501) };
      expect(validateGoal(metric501).metric).toBe('Metric cannot exceed 500 characters.');
    });

    it('fails when timeframe exceeds 100 characters', () => {
      const tf101 = { ...baseValidGoal, timeframe: 'T'.repeat(101) };
      expect(validateGoal(tf101).timeframe).toBe('Timeframe cannot exceed 100 characters.');
    });
  });

  describe('Composite goal validation', () => {
    it('returns empty error map when all fields are completely valid', () => {
      const errors = validateGoal(baseValidGoal);
      expect(Object.keys(errors)).toHaveLength(0);
    });

    it('returns multiple errors when both title and description are invalid', () => {
      const errors = validateGoal({ title: 'Hi', description: 'Tiny' });
      expect(errors.title).toBe('Title must be between 5 and 120 characters.');
      expect(errors.description).toBe('Description must be between 10 and 1000 characters.');
    });
  });
});
