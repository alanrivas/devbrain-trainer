import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useParams, useRouter } from 'next/navigation';
import { useAuth } from '@/components/AuthContext';
import api from '@/lib/api';
import ChallengeDetailPage from './page';

jest.mock('next/navigation');
jest.mock('@/components/AuthContext');
jest.mock('@/lib/api');
jest.mock('@/components/AttemptForm', () => {
  return function MockAttemptForm(props: {
    challengeId: string;
    timeLimitSecs: number;
    onSuccess?: (result: { isCorrect: boolean; elapsedSeconds: number }) => void;
  }) {
    return (
      <div data-testid="attempt-form" data-challenge-id={props.challengeId} data-time-limit={props.timeLimitSecs}>
        <button onClick={() => props.onSuccess?.({ isCorrect: true, elapsedSeconds: 12 })}>Trigger Success</button>
      </div>
    );
  };
});

describe('ChallengeDetailPage', () => {
  const mockPush = jest.fn();
  const mockClearAuth = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
    (useParams as jest.Mock).mockReturnValue({ id: 'challenge-1' });
    (useAuth as jest.Mock).mockReturnValue({ token: 'jwt-token', clearAuth: mockClearAuth });
  });

  it('should redirect to login when user has no token', async () => {
    (useAuth as jest.Mock).mockReturnValue({ token: null, clearAuth: mockClearAuth });

    render(<ChallengeDetailPage />);

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  it('should render challenge details when fetch succeeds', async () => {
    (api.get as jest.Mock).mockResolvedValue({
      data: {
        id: 'challenge-1',
        title: 'SQL Challenge',
        description: 'Write a query',
        category: 'Sql',
        difficulty: 'Easy',
        timeLimitSecs: 60,
      },
    });

    render(<ChallengeDetailPage />);

    await waitFor(() => {
      expect(screen.getByText('SQL Challenge')).toBeInTheDocument();
      expect(screen.getByText('Write a query')).toBeInTheDocument();
    });
  });

  it('should show loading state while fetching challenge', () => {
    (api.get as jest.Mock).mockReturnValue(new Promise(() => undefined));

    render(<ChallengeDetailPage />);

    expect(screen.getByText(/loading challenge/i)).toBeInTheDocument();
  });

  it('should show not found state on 404', async () => {
    (api.get as jest.Mock).mockRejectedValue({ response: { status: 404 } });

    render(<ChallengeDetailPage />);

    await waitFor(() => {
      expect(screen.getByText(/challenge not found/i)).toBeInTheDocument();
    });
  });

  it('should show generic error state on fetch failure', async () => {
    (api.get as jest.Mock).mockRejectedValue({ response: { status: 500 } });

    render(<ChallengeDetailPage />);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/failed to load challenge/i);
    });
  });

  it('should clear auth and redirect on 401 from challenge fetch', async () => {
    (api.get as jest.Mock).mockRejectedValue({ response: { status: 401 } });

    render(<ChallengeDetailPage />);

    await waitFor(() => {
      expect(mockClearAuth).toHaveBeenCalled();
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  it('should render AttemptForm with challenge props', async () => {
    (api.get as jest.Mock).mockResolvedValue({
      data: {
        id: 'challenge-1',
        title: 'SQL Challenge',
        description: 'Write a query',
        category: 'Sql',
        difficulty: 'Easy',
        timeLimitSecs: 45,
      },
    });

    render(<ChallengeDetailPage />);

    await waitFor(() => {
      const attemptForm = screen.getByTestId('attempt-form');
      expect(attemptForm).toHaveAttribute('data-challenge-id', 'challenge-1');
      expect(attemptForm).toHaveAttribute('data-time-limit', '45');
    });
  });

  it('should show last attempt section after successful attempt callback', async () => {
    const user = userEvent.setup();
    (api.get as jest.Mock).mockResolvedValue({
      data: {
        id: 'challenge-1',
        title: 'SQL Challenge',
        description: 'Write a query',
        category: 'Sql',
        difficulty: 'Easy',
        timeLimitSecs: 45,
      },
    });

    render(<ChallengeDetailPage />);

    await waitFor(() => {
      expect(screen.getByTestId('attempt-form')).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: /trigger success/i }));

    await waitFor(() => {
      expect(screen.getByText(/last attempt/i)).toBeInTheDocument();
      expect(screen.getByText(/result: correct/i)).toBeInTheDocument();
    });
  });
});
