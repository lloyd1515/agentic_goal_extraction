import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { TranscriptInput, SAMPLE_TRANSCRIPT } from '../components/TranscriptInput';

describe('TranscriptInput Component', () => {
  const defaultProps = {
    transcript: '',
    onChange: vi.fn(),
    onExtract: vi.fn(),
    isLoading: false,
    onLoadSample: vi.fn(),
  };

  it('renders textarea with aria-label="Discussion Transcript" for accessibility (AC 1.1)', () => {
    render(<TranscriptInput {...defaultProps} />);

    const textarea = screen.getByLabelText('Discussion Transcript');
    expect(textarea).toBeInTheDocument();
    expect(textarea).toHaveAttribute('aria-label', 'Discussion Transcript');
    expect(textarea.tagName.toLowerCase()).toBe('textarea');
  });

  it('disables submit button and shows 0 count when input is empty', () => {
    render(<TranscriptInput {...defaultProps} transcript="" />);

    const extractBtn = screen.getByRole('button', { name: /extract goals with ai/i });
    expect(extractBtn).toBeDisabled();
    expect(screen.getByText('0 / 32,000 characters')).toBeInTheDocument();
  });

  it('displays validation error and disables submit when input is shorter than 20 characters (min length)', () => {
    render(<TranscriptInput {...defaultProps} transcript="Short note" />);

    const extractBtn = screen.getByRole('button', { name: /extract goals with ai/i });
    expect(extractBtn).toBeDisabled();

    expect(
      screen.getByText(/Transcript must be at least 20 characters \(currently 10\)\./i)
    ).toBeInTheDocument();
    expect(screen.getByText('10 / 32,000 characters')).toBeInTheDocument();
  });

  it('displays validation error and disables submit when input exceeds 32,000 characters (max length)', () => {
    const longText = 'a'.repeat(32005);
    render(<TranscriptInput {...defaultProps} transcript={longText} />);

    const extractBtn = screen.getByRole('button', { name: /extract goals with ai/i });
    expect(extractBtn).toBeDisabled();

    expect(
      screen.getByText(/Transcript exceeds maximum limit of 32,000 characters \(currently 32,005\)\./i)
    ).toBeInTheDocument();
    expect(screen.getByText('32,005 / 32,000 characters')).toBeInTheDocument();
  });

  it('enables extract button when transcript length is within valid bounds (20 - 32,000 chars)', () => {
    const validText = 'Manager: Let us migrate our auth gateway to OAuth 2.1 by Q3.';
    const handleExtract = vi.fn();
    render(<TranscriptInput {...defaultProps} transcript={validText} onExtract={handleExtract} />);

    const extractBtn = screen.getByRole('button', { name: /extract goals with ai/i });
    expect(extractBtn).not.toBeDisabled();
    expect(screen.queryByText(/Transcript must be at least 20 characters/i)).not.toBeInTheDocument();
    expect(screen.queryByText(/Transcript exceeds maximum limit/i)).not.toBeInTheDocument();

    fireEvent.click(extractBtn);
    expect(handleExtract).toHaveBeenCalledTimes(1);
  });

  it('renders loading state: disables textarea, load sample button, and extract button with spinner', () => {
    render(
      <TranscriptInput
        {...defaultProps}
        transcript={SAMPLE_TRANSCRIPT}
        isLoading={true}
      />
    );

    const textarea = screen.getByLabelText('Discussion Transcript');
    expect(textarea).toBeDisabled();

    const loadSampleBtn = screen.getByRole('button', { name: /load sample 1:1 discussion/i });
    expect(loadSampleBtn).toBeDisabled();

    expect(screen.getByText('Extracting Goals...')).toBeInTheDocument();
    const extractingBtn = screen.getByRole('button', { name: /extracting goals\.\.\./i });
    expect(extractingBtn).toBeDisabled();
  });

  it('calls onLoadSample when clicking "Load Sample 1:1 Discussion" button', () => {
    const handleLoadSample = vi.fn();
    render(<TranscriptInput {...defaultProps} onLoadSample={handleLoadSample} />);

    const sampleBtn = screen.getByRole('button', { name: /load sample 1:1 discussion/i });
    fireEvent.click(sampleBtn);

    expect(handleLoadSample).toHaveBeenCalledTimes(1);
  });

  it('invokes onChange when user types in the textarea', () => {
    const handleChange = vi.fn();
    render(<TranscriptInput {...defaultProps} onChange={handleChange} />);

    const textarea = screen.getByLabelText('Discussion Transcript');
    fireEvent.change(textarea, { target: { value: 'Discussion notes between Marcus and Elena.' } });

    expect(handleChange).toHaveBeenCalledWith('Discussion notes between Marcus and Elena.');
  });
});
