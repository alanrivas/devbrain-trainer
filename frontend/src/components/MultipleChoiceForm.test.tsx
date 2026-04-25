import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useRouter } from 'next/navigation';
import MultipleChoiceForm from './MultipleChoiceForm';
import { useAuth } from './AuthContext';
import api from '@/lib/api';

jest.mock('next/navigation');
jest.mock('./AuthContext');
jest.mock('@/lib/api');

describe('MultipleChoiceForm', () => {
  const mockPush = jest.fn();
  const mockClearAuth = jest.fn();
  const options = ['O(n)', 'O(n²)', 'O(log n)', 'O(n log n)'];

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
    (useAuth as jest.Mock).mockReturnValue({ clearAuth: mockClearAuth });
  });

  afterEach(() => {
    jest.useRealTimers();
  });

  // Rendering

  it('should render one radio button per provided option', () => {
    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    expect(screen.getAllByRole('radio')).toHaveLength(options.length);
  });

  it('should display the label text for each option', () => {
    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    options.forEach((opt) => expect(screen.getByText(opt)).toBeInTheDocument());
  });

  it('should have submit button disabled on mount when no option is selected', () => {
    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    expect(screen.getByRole('button', { name: /submit answer/i })).toBeDisabled();
  });

  it('should show the timer on mount', () => {
    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    expect(screen.getByText(/time remaining/i)).toBeInTheDocument();
  });

  it('should show "Enter to submit" keyboard hint', () => {
    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    expect(screen.getByText(/enter to submit/i)).toBeInTheDocument();
  });

  // Selection

  it('should mark the clicked option as selected', async () => {
    const user = userEvent.setup();
    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);

    await user.click(screen.getByRole('radio', { name: options[0] }));

    expect(screen.getByRole('radio', { name: options[0] })).toBeChecked();
  });

  it('should enable submit button after selecting an option', async () => {
    const user = userEvent.setup();
    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);

    await user.click(screen.getByRole('radio', { name: options[0] }));

    expect(screen.getByRole('button', { name: /submit answer/i })).toBeEnabled();
  });

  it('should deselect the first option when a second is selected', async () => {
    const user = userEvent.setup();
    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);

    await user.click(screen.getByRole('radio', { name: options[0] }));
    await user.click(screen.getByRole('radio', { name: options[1] }));

    expect(screen.getByRole('radio', { name: options[0] })).not.toBeChecked();
    expect(screen.getByRole('radio', { name: options[1] })).toBeChecked();
  });

  // Submit

  it('should show error and not call API when form is submitted without a selection', () => {
    const { container } = render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);

    fireEvent.submit(container.querySelector('form')!);

    expect(screen.getByRole('alert')).toHaveTextContent(/please select an option/i);
    expect(api.post).not.toHaveBeenCalled();
  });

  it('should call POST with userAnswer equal to the selected option text', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: options[2], isCorrect: true, elapsedSeconds: 5 },
    });

    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);

    await user.click(screen.getByRole('radio', { name: options[2] }));
    await user.click(screen.getByRole('button', { name: /submit answer/i }));

    await waitFor(() => {
      expect(api.post).toHaveBeenCalledWith('/challenges/c-1/attempt', {
        userAnswer: options[2],
        elapsedSeconds: expect.any(Number),
      });
    });
  });

  it('should disable radios and submit button while loading', async () => {
    const user = userEvent.setup();
    let resolveRequest!: (value: unknown) => void;
    const pending = new Promise((resolve) => { resolveRequest = resolve; });
    (api.post as jest.Mock).mockReturnValue(pending);

    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);

    await user.click(screen.getByRole('radio', { name: options[0] }));
    await user.click(screen.getByRole('button', { name: /submit answer/i }));

    await waitFor(() => expect(screen.getByRole('button', { name: /submitting/i })).toBeDisabled());
    screen.getAllByRole('radio').forEach((radio) => expect(radio).toBeDisabled());

    resolveRequest({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: options[0], isCorrect: true, elapsedSeconds: 5 },
    });
    await waitFor(() => expect(screen.getByText(/correct!/i)).toBeInTheDocument());
  });

  it('should show "Correct!" when result is correct', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: options[2], isCorrect: true, elapsedSeconds: 10 },
    });

    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    await user.click(screen.getByRole('radio', { name: options[2] }));
    await user.click(screen.getByRole('button', { name: /submit answer/i }));

    await waitFor(() => expect(screen.getByText(/correct!/i)).toBeInTheDocument());
  });

  it('should show "Not quite" and correct answer when result is incorrect', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: {
        attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1',
        userAnswer: options[0], isCorrect: false, correctAnswer: options[2], elapsedSeconds: 20,
      },
    });

    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    await user.click(screen.getByRole('radio', { name: options[0] }));
    await user.click(screen.getByRole('button', { name: /submit answer/i }));

    await waitFor(() => {
      expect(screen.getByText(/not quite/i)).toBeInTheDocument();
      expect(screen.getByText((content) => content.toLowerCase().includes(`correct answer: ${options[2].toLowerCase()}`))).toBeInTheDocument();
    });
  });

  // Result and reset

  it('should display ELO rating in result card when present', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: options[0], isCorrect: true, elapsedSeconds: 10, newEloRating: 1250 },
    });

    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    await user.click(screen.getByRole('radio', { name: options[0] }));
    await user.click(screen.getByRole('button', { name: /submit answer/i }));

    await waitFor(() => expect(screen.getByText(/elo: 1250/i)).toBeInTheDocument());
  });

  it('should display streak in result card when present', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: options[0], isCorrect: true, elapsedSeconds: 10, newStreak: 3 },
    });

    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    await user.click(screen.getByRole('radio', { name: options[0] }));
    await user.click(screen.getByRole('button', { name: /submit answer/i }));

    await waitFor(() => expect(screen.getByText(/streak: 3 days/i)).toBeInTheDocument());
  });

  it('should display new badge in result card when present', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: options[0], isCorrect: true, elapsedSeconds: 10, newBadges: ['FirstAttempt'] },
    });

    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    await user.click(screen.getByRole('radio', { name: options[0] }));
    await user.click(screen.getByRole('button', { name: /submit answer/i }));

    await waitFor(() => expect(screen.getByText(/new badge: FirstAttempt/i)).toBeInTheDocument());
  });

  it('should reset selection and clear result when "Try again" is clicked', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: options[0], isCorrect: true, elapsedSeconds: 5 },
    });

    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    await user.click(screen.getByRole('radio', { name: options[0] }));
    await user.click(screen.getByRole('button', { name: /submit answer/i }));
    await waitFor(() => expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument());

    await user.click(screen.getByRole('button', { name: /try again/i }));

    screen.getAllByRole('radio').forEach((radio) => expect(radio).not.toBeChecked());
    expect(screen.queryByText(/correct!/i)).not.toBeInTheDocument();
  });

  // Error handling

  it('should show 400 validation message', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 400 } });

    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    await user.click(screen.getByRole('radio', { name: options[0] }));
    await user.click(screen.getByRole('button', { name: /submit answer/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/invalid answer\. please review your input\./i);
    });
  });

  it('should clear auth and redirect to login on 401', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 401 } });

    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    await user.click(screen.getByRole('radio', { name: options[0] }));
    await user.click(screen.getByRole('button', { name: /submit answer/i }));

    await waitFor(() => {
      expect(mockClearAuth).toHaveBeenCalled();
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  it('should show "Challenge not found." on 404', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 404 } });

    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    await user.click(screen.getByRole('radio', { name: options[0] }));
    await user.click(screen.getByRole('button', { name: /submit answer/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/challenge not found\./i);
    });
  });

  it('should show "Server error. Try again." on 500', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 500 } });

    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    await user.click(screen.getByRole('radio', { name: options[0] }));
    await user.click(screen.getByRole('button', { name: /submit answer/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/server error\. try again\./i);
    });
  });

  // Accessibility

  it('should render options inside a <fieldset>', () => {
    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    expect(screen.getByRole('group')).toBeInTheDocument();
  });

  it('should have a <legend> describing the option group', () => {
    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    // eslint-disable-next-line testing-library/no-node-access
    expect(document.querySelector('legend')).toBeInTheDocument();
    expect(screen.getByText(/choose your answer/i)).toBeInTheDocument();
  });

  it('should render error messages with role="alert"', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 500 } });

    render(<MultipleChoiceForm challengeId="c-1" timeLimitSecs={60} options={options} />);
    await user.click(screen.getByRole('radio', { name: options[0] }));
    await user.click(screen.getByRole('button', { name: /submit answer/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toBeInTheDocument();
    });
  });
});
