import { act, fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useRouter } from 'next/navigation';
import AttemptForm from './AttemptForm';
import { useAuth } from './AuthContext';
import api from '@/lib/api';

jest.mock('next/navigation');
jest.mock('./AuthContext');
jest.mock('@/lib/api');

describe('AttemptForm', () => {
  const mockPush = jest.fn();
  const mockClearAuth = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    localStorage.clear();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
    (useAuth as jest.Mock).mockReturnValue({ clearAuth: mockClearAuth });
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  it('should render answer input and submit button', () => {
    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    expect(screen.getByLabelText(/your answer/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /submit attempt/i })).toBeInTheDocument();
  });

  it('should render time limit hint', () => {
    render(<AttemptForm challengeId="c-1" timeLimitSecs={90} />);
    expect(screen.getByText(/time remaining/i)).toBeInTheDocument();
    expect(screen.getByText(/90s \//i)).toBeInTheDocument();
  });

  it('should update the timer as time passes', () => {
    jest.useFakeTimers();
    render(<AttemptForm challengeId="c-1" timeLimitSecs={90} />);

    act(() => {
      jest.advanceTimersByTime(2000);
    });

    expect(screen.getByText((content) => content.includes('88s / 90s'))).toBeInTheDocument();
  });

  it('should switch timer styling to warning state near the limit', () => {
    jest.useFakeTimers();
    render(<AttemptForm challengeId="c-1" timeLimitSecs={10} />);

    act(() => {
      jest.advanceTimersByTime(6000);
    });

    const timerContainer = screen.getByText(/time remaining/i).closest('[aria-live="polite"]');
    expect(timerContainer).toHaveClass('border-amber-200');
  });

  it('should show validation error when answer is empty', async () => {
    const user = userEvent.setup();
    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    expect(screen.getByRole('alert')).toHaveTextContent(/answer is required/i);
    expect(api.post).not.toHaveBeenCalled();
  });

  it('should reject whitespace-only answers', async () => {
    const user = userEvent.setup();
    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await user.type(screen.getByLabelText(/your answer/i), '   ');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    expect(api.post).not.toHaveBeenCalled();
    expect(screen.getByRole('alert')).toBeInTheDocument();
  });

  it('should submit payload with challengeId, userAnswer and elapsedSeconds', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: {
        attemptId: 'a-1',
        challengeId: 'c-1',
        userId: 'u-1',
        userAnswer: 'SELECT 1',
        isCorrect: true,
        elapsedSeconds: 1,
      },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await user.type(screen.getByLabelText(/your answer/i), 'SELECT 1');
    fireEvent.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(api.post).toHaveBeenCalledWith('/challenges/c-1/attempt', {
        userAnswer: 'SELECT 1',
        elapsedSeconds: expect.any(Number),
      });
    });
  });

  it('should disable submit button while request is in progress', async () => {
    const user = userEvent.setup();
    let resolveRequest: (value: unknown) => void = () => undefined;
    const pending = new Promise((resolve) => {
      resolveRequest = resolve;
    });
    (api.post as jest.Mock).mockReturnValue(pending);

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await user.type(screen.getByLabelText(/your answer/i), 'answer');
    const submitButton = screen.getByRole('button', { name: /submit attempt/i });
    await user.click(submitButton);

  expect(screen.getByRole('button', { name: /submitting attempt/i })).toBeDisabled();

    resolveRequest({
      data: {
        attemptId: 'a-1',
        challengeId: 'c-1',
        userId: 'u-1',
        userAnswer: 'answer',
        isCorrect: true,
        elapsedSeconds: 1,
      },
    });

    await waitFor(() => expect(screen.getByRole('button', { name: /submit attempt/i })).toBeEnabled());
  });

  it('should show success feedback when attempt is correct', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: {
        attemptId: 'a-1',
        challengeId: 'c-1',
        userId: 'u-1',
        userAnswer: 'answer',
        isCorrect: true,
        elapsedSeconds: 1,
      },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await user.type(screen.getByLabelText(/your answer/i), 'answer');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByText(/correct!/i)).toBeInTheDocument();
    });
  });

  it('should show incorrect feedback and correct answer when provided', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: {
        attemptId: 'a-1',
        challengeId: 'c-1',
        userId: 'u-1',
        userAnswer: 'wrong',
        isCorrect: false,
        correctAnswer: 'right answer',
        elapsedSeconds: 2,
      },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await user.type(screen.getByLabelText(/your answer/i), 'wrong');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByText(/not quite/i)).toBeInTheDocument();
      expect(screen.getByText(/correct answer: right answer/i)).toBeInTheDocument();
    });
  });

  it('should render retry and back to challenges actions after success', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: {
        attemptId: 'a-1',
        challengeId: 'c-1',
        userId: 'u-1',
        userAnswer: 'answer',
        isCorrect: true,
        elapsedSeconds: 1,
      },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await user.type(screen.getByLabelText(/your answer/i), 'answer');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
      expect(screen.getByRole('link', { name: /back to challenges/i })).toBeInTheDocument();
    });
  });

  it('should reset the form when retry is clicked', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: {
        attemptId: 'a-1',
        challengeId: 'c-1',
        userId: 'u-1',
        userAnswer: 'answer',
        isCorrect: true,
        elapsedSeconds: 1,
      },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await user.type(screen.getByLabelText(/your answer/i), 'answer');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: /try again/i }));

    expect(screen.getByLabelText(/your answer/i)).toHaveValue('');
    expect(screen.queryByText(/correct!/i)).not.toBeInTheDocument();
  });

  it('should call onSuccess callback with result data', async () => {
    const user = userEvent.setup();
    const onSuccess = jest.fn();
    const result = {
      attemptId: 'a-1',
      challengeId: 'c-1',
      userId: 'u-1',
      userAnswer: 'answer',
      isCorrect: true,
      elapsedSeconds: 1,
    };
    (api.post as jest.Mock).mockResolvedValue({ data: result });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} onSuccess={onSuccess} />);

    await user.type(screen.getByLabelText(/your answer/i), 'answer');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(onSuccess).toHaveBeenCalledWith(result);
    });
  });

  it('should show 400 validation message', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 400 } });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await user.type(screen.getByLabelText(/your answer/i), 'answer');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/invalid answer/i);
    });
  });

  it('should clear auth and redirect to login on 401', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 401 } });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await user.type(screen.getByLabelText(/your answer/i), 'answer');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(mockClearAuth).toHaveBeenCalled();
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  it('should show 404 message when challenge does not exist', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 404 } });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await user.type(screen.getByLabelText(/your answer/i), 'answer');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/challenge not found/i);
    });
  });

  it('should show generic server error on 500 or network failure', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 500 } });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await user.type(screen.getByLabelText(/your answer/i), 'answer');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/server error\. try again/i);
    });
  });

  // Draft Persistence

  it('should save draft to localStorage as user types', async () => {
    const user = userEvent.setup();
    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await user.type(screen.getByLabelText(/your answer/i), 'my draft');

    expect(localStorage.getItem('draft-attempt-c-1')).toBe('my draft');
  });

  it('should pre-fill textarea with existing draft on mount', async () => {
    localStorage.setItem('draft-attempt-c-1', 'saved draft text');

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    await waitFor(() => {
      expect(screen.getByLabelText(/your answer/i)).toHaveValue('saved draft text');
    });
  });

  it('should leave textarea empty when no draft exists', () => {
    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    expect(screen.getByLabelText(/your answer/i)).toHaveValue('');
  });

  it('should clear draft from localStorage after successful submit', async () => {
    const user = userEvent.setup();
    localStorage.setItem('draft-attempt-c-1', 'my answer');
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'my answer', isCorrect: true, elapsedSeconds: 5 },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'my answer');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(localStorage.getItem('draft-attempt-c-1')).toBeNull();
    });
  });

  it('should clear draft from localStorage when form is reset', async () => {
    const user = userEvent.setup();
    localStorage.setItem('draft-attempt-c-1', 'old answer');
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'old answer', isCorrect: true, elapsedSeconds: 5 },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'old answer');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));
    await waitFor(() => screen.getByRole('button', { name: /try again/i }));
    await user.click(screen.getByRole('button', { name: /try again/i }));

    expect(localStorage.getItem('draft-attempt-c-1')).toBeNull();
  });

  it('should ignore whitespace-only draft on mount', () => {
    localStorage.setItem('draft-attempt-c-1', '   ');

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    expect(screen.getByLabelText(/your answer/i)).toHaveValue('');
  });

  // Performance Badge

  it('should show "Fast answer" badge when elapsed is at most 50% of time limit', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'ans', isCorrect: true, elapsedSeconds: 25 },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'ans');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByText(/fast answer/i)).toBeInTheDocument();
    });
  });

  it('should show "In time" badge when elapsed is between 50% and 80% of time limit', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'ans', isCorrect: true, elapsedSeconds: 36 },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'ans');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByText(/in time/i)).toBeInTheDocument();
    });
  });

  it('should show "Cutting it close" badge when elapsed exceeds 80% of time limit', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'ans', isCorrect: true, elapsedSeconds: 54 },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'ans');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByText(/cutting it close/i)).toBeInTheDocument();
    });
  });

  // Gamification Result Feedback

  it('should display ELO rating in result card when present', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'ans', isCorrect: true, elapsedSeconds: 10, newEloRating: 1250 },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'ans');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByText(/elo: 1250/i)).toBeInTheDocument();
    });
  });

  it('should not display ELO when newEloRating is absent', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'ans', isCorrect: true, elapsedSeconds: 10 },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'ans');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => expect(screen.getByText(/correct!/i)).toBeInTheDocument());
    expect(screen.queryByText(/elo:/i)).not.toBeInTheDocument();
  });

  it('should display streak in result card when present', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'ans', isCorrect: true, elapsedSeconds: 10, newStreak: 3 },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'ans');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByText(/streak: 3 days/i)).toBeInTheDocument();
    });
  });

  it('should not display streak when newStreak is absent', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'ans', isCorrect: true, elapsedSeconds: 10 },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'ans');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => expect(screen.getByText(/correct!/i)).toBeInTheDocument());
    expect(screen.queryByText(/streak:/i)).not.toBeInTheDocument();
  });

  it('should display a single new badge when earned', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'ans', isCorrect: true, elapsedSeconds: 10, newBadges: ['FirstAttempt'] },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'ans');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByText(/new badge: FirstAttempt/i)).toBeInTheDocument();
    });
  });

  it('should display multiple new badges when earned', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'ans', isCorrect: true, elapsedSeconds: 10, newBadges: ['FirstAttempt', 'TenAttempts'] },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'ans');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByText(/new badge: FirstAttempt/i)).toBeInTheDocument();
      expect(screen.getByText(/new badge: TenAttempts/i)).toBeInTheDocument();
    });
  });

  it('should not display badge section when newBadges is empty', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'ans', isCorrect: true, elapsedSeconds: 10, newBadges: [] },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'ans');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => expect(screen.getByText(/correct!/i)).toBeInTheDocument());
    expect(screen.queryByText(/new badge:/i)).not.toBeInTheDocument();
  });

  // Keyboard Navigation

  it('CtrlEnter_GivenTextareaHasText_Should_SubmitForm', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'my answer', isCorrect: true, elapsedSeconds: 5 },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'my answer');

    fireEvent.keyDown(document, { key: 'Enter', ctrlKey: true });

    await waitFor(() => {
      expect(api.post).toHaveBeenCalledWith('/challenges/c-1/attempt', expect.objectContaining({ userAnswer: 'my answer' }));
    });
  });

  it('CtrlEnter_GivenTextareaIsEmpty_Should_ShowValidationError', async () => {
    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);

    fireEvent.keyDown(document, { key: 'Enter', ctrlKey: true });

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/answer is required/i);
    });
    expect(api.post).not.toHaveBeenCalled();
  });

  it('R_GivenResultIsVisible_Should_ResetForm', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'answer', isCorrect: true, elapsedSeconds: 5 },
    });

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'answer');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));
    await waitFor(() => expect(screen.getByText(/correct!/i)).toBeInTheDocument());

    fireEvent.keyDown(document, { key: 'r' });

    expect(screen.getByLabelText(/your answer/i)).toHaveValue('');
    expect(screen.queryByText(/correct!/i)).not.toBeInTheDocument();
  });

  it('CtrlEnter_GivenLoadingInProgress_Should_NotSubmitAgain', async () => {
    const user = userEvent.setup();
    let resolveRequest!: (value: unknown) => void;
    const pending = new Promise((resolve) => { resolveRequest = resolve; });
    (api.post as jest.Mock).mockReturnValue(pending);

    render(<AttemptForm challengeId="c-1" timeLimitSecs={60} />);
    await user.type(screen.getByLabelText(/your answer/i), 'answer');
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => expect(screen.getByRole('button', { name: /submitting attempt/i })).toBeDisabled());

    fireEvent.keyDown(document, { key: 'Enter', ctrlKey: true });

    resolveRequest({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'answer', isCorrect: true, elapsedSeconds: 5 },
    });

    await waitFor(() => expect(screen.getByText(/correct!/i)).toBeInTheDocument());
    expect(api.post).toHaveBeenCalledTimes(1);
  });
});
