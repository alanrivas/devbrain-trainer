import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useRouter } from 'next/navigation';
import CodeRunnerForm from './CodeRunnerForm';
import type { CodeTestCase, TestResult } from './CodeRunnerForm';
import { useAuth } from './AuthContext';
import api from '@/lib/api';

jest.mock('next/navigation');
jest.mock('./AuthContext');
jest.mock('@/lib/api');
jest.mock('@monaco-editor/react', () =>
  function MockEditor({ value, onChange }: { value: string; onChange: (v: string) => void }) {
    return (
      <textarea
        data-testid="code-editor"
        value={value}
        onChange={(e) => onChange(e.target.value)}
      />
    );
  }
);

describe('CodeRunnerForm', () => {
  const mockPush = jest.fn();
  const mockClearAuth = jest.fn();

  const testCases: CodeTestCase[] = [
    { input: '2, 3', expectedOutput: '5', description: 'Returns sum of 2 and 3' },
    { input: '0, 0', expectedOutput: '0', description: 'Zero sum' },
  ];

  const starterCode = 'function solution(a, b) {}';

  const allPassingMock = (code: string, tcs: CodeTestCase[]): TestResult[] =>
    tcs.map((tc) => ({ description: tc.description, passed: true, actualOutput: tc.expectedOutput }));

  const oneFailingMock = (code: string, tcs: CodeTestCase[]): TestResult[] =>
    tcs.map((tc, i) =>
      i === 0
        ? { description: tc.description, passed: false, actualOutput: 'wrong' }
        : { description: tc.description, passed: true, actualOutput: tc.expectedOutput }
    );

  const withErrorMock = (code: string, tcs: CodeTestCase[]): TestResult[] =>
    tcs.map((tc, i) =>
      i === 0
        ? { description: tc.description, passed: false, actualOutput: '', error: 'ReferenceError: solution is not defined' }
        : { description: tc.description, passed: true, actualOutput: tc.expectedOutput }
    );

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
    (useAuth as jest.Mock).mockReturnValue({ clearAuth: mockClearAuth });
  });

  // Rendering

  it('should render the code editor', () => {
    render(<CodeRunnerForm challengeId="c-1" timeLimitSecs={120} starterCode={starterCode} testCases={testCases} />);
    expect(screen.getByTestId('code-editor')).toBeInTheDocument();
  });

  it('should show starterCode as initial editor value', () => {
    render(<CodeRunnerForm challengeId="c-1" timeLimitSecs={120} starterCode={starterCode} testCases={testCases} />);
    expect(screen.getByTestId('code-editor')).toHaveValue(starterCode);
  });

  it('should render test case rows with pending state', () => {
    render(<CodeRunnerForm challengeId="c-1" timeLimitSecs={120} starterCode={starterCode} testCases={testCases} />);
    testCases.forEach((tc) => expect(screen.getByText(tc.description)).toBeInTheDocument());
    expect(screen.getAllByText(/not run yet/i)).toHaveLength(testCases.length);
  });

  it('should have submit button disabled on mount', () => {
    render(<CodeRunnerForm challengeId="c-1" timeLimitSecs={120} starterCode={starterCode} testCases={testCases} />);
    expect(screen.getByRole('button', { name: /run tests first/i })).toBeDisabled();
  });

  // Execution

  it('should mark all tests passed when runCode returns all passing', async () => {
    const user = userEvent.setup();
    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={allPassingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));

    expect(screen.getAllByText(/✅ Passed/i)).toHaveLength(testCases.length);
  });

  it('should show actualOutput when a test fails', async () => {
    const user = userEvent.setup();
    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={oneFailingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));

    expect(screen.getByText(/Got: wrong/i)).toBeInTheDocument();
  });

  it('should show error message when code throws exception', async () => {
    const user = userEvent.setup();
    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={withErrorMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));

    expect(screen.getByText(/ReferenceError: solution is not defined/i)).toBeInTheDocument();
  });

  it('should enable Submit button after all tests pass', async () => {
    const user = userEvent.setup();
    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={allPassingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));

    expect(screen.getByRole('button', { name: /submit attempt/i })).toBeEnabled();
  });

  it('should keep Submit disabled after a failing test', async () => {
    const user = userEvent.setup();
    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={oneFailingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));

    expect(screen.getByRole('button', { name: /run tests first/i })).toBeDisabled();
  });

  // Submit

  it('should call POST with userAnswer: "PASS" when submitting', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'PASS', isCorrect: true, elapsedSeconds: 10 },
    });

    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={allPassingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(api.post).toHaveBeenCalledWith('/challenges/c-1/attempt', {
        userAnswer: 'PASS',
        elapsedSeconds: expect.any(Number),
      });
    });
  });

  it('should disable submit button while loading', async () => {
    const user = userEvent.setup();
    let resolveRequest!: (value: unknown) => void;
    const pending = new Promise((resolve) => { resolveRequest = resolve; });
    (api.post as jest.Mock).mockReturnValue(pending);

    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={allPassingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => expect(screen.getByRole('button', { name: /submitting/i })).toBeDisabled());

    resolveRequest({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'PASS', isCorrect: true, elapsedSeconds: 10 },
    });
    await waitFor(() => expect(screen.getByText(/correct!/i)).toBeInTheDocument());
  });

  it('should show "Correct!" when result is correct', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'PASS', isCorrect: true, elapsedSeconds: 10 },
    });

    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={allPassingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => expect(screen.getByText(/correct!/i)).toBeInTheDocument());
  });

  it('should show "Not quite" when result is incorrect', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'PASS', isCorrect: false, elapsedSeconds: 10 },
    });

    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={allPassingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => expect(screen.getByText(/not quite/i)).toBeInTheDocument());
  });

  // Reset and result

  it('should reset editor to starterCode when "Try again" is clicked', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'PASS', isCorrect: true, elapsedSeconds: 5 },
    });

    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={allPassingMock}
      />
    );

    // Change code, run, submit, then try again
    fireEvent.change(screen.getByTestId('code-editor'), {
      target: { value: 'function solution(a, b) { return a + b; }' },
    });
    await user.click(screen.getByRole('button', { name: 'Run Tests' }));
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));
    await waitFor(() => expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument());

    await user.click(screen.getByRole('button', { name: /try again/i }));

    expect(screen.getByTestId('code-editor')).toHaveValue(starterCode);
  });

  it('should clear test results when "Try again" is clicked', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'PASS', isCorrect: true, elapsedSeconds: 5 },
    });

    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={allPassingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));
    await waitFor(() => expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument());

    await user.click(screen.getByRole('button', { name: /try again/i }));

    expect(screen.getAllByText(/not run yet/i)).toHaveLength(testCases.length);
  });

  it('should display ELO rating in result card when present', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'PASS', isCorrect: true, elapsedSeconds: 10, newEloRating: 1300 },
    });

    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={allPassingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => expect(screen.getByText(/elo: 1300/i)).toBeInTheDocument());
  });

  // Error handling

  it('should clear auth and redirect to login on 401', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 401 } });

    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={allPassingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(mockClearAuth).toHaveBeenCalled();
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  it('should show "Challenge not found." on 404', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 404 } });

    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={allPassingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/challenge not found\./i);
    });
  });

  it('should show "Server error. Try again." on 500', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 500 } });

    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={allPassingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/server error\. try again\./i);
    });
  });

  // Accessibility

  it('should render error messages with role="alert"', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 500 } });

    render(
      <CodeRunnerForm
        challengeId="c-1"
        timeLimitSecs={120}
        starterCode={starterCode}
        testCases={testCases}
        runCode={allPassingMock}
      />
    );

    await user.click(screen.getByRole('button', { name: 'Run Tests' }));
    await user.click(screen.getByRole('button', { name: /submit attempt/i }));

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
  });

  it('should show each test case description in the DOM', () => {
    render(<CodeRunnerForm challengeId="c-1" timeLimitSecs={120} starterCode={starterCode} testCases={testCases} />);
    testCases.forEach((tc) => expect(screen.getByText(tc.description)).toBeInTheDocument());
  });
});
