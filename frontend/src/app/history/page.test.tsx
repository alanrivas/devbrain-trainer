import { render, screen, waitFor } from '@testing-library/react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/components/AuthContext';
import api from '@/lib/api';
import HistoryPage from './page';

jest.mock('next/navigation');
jest.mock('@/components/AuthContext');
jest.mock('@/lib/api');

describe('HistoryPage', () => {
  const mockPush = jest.fn();
  const mockClearAuth = jest.fn();

  const mockAttempts = [
    {
      attemptId: 'attempt-1',
      challengeId: 'challenge-1',
      challengeTitle: 'SQL Challenge',
      isCorrect: true,
      elapsedSecs: 42,
      occurredAt: '2026-04-15T10:00:00Z',
    },
    {
      attemptId: 'attempt-2',
      challengeId: 'challenge-2',
      challengeTitle: 'Docker Challenge',
      isCorrect: false,
      elapsedSecs: 18,
      occurredAt: '2026-04-15T09:00:00Z',
    },
  ];

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
    (useAuth as jest.Mock).mockReturnValue({ token: 'jwt-token', clearAuth: mockClearAuth });
  });

  it('should redirect to login when user has no token', async () => {
    (useAuth as jest.Mock).mockReturnValue({ token: null, clearAuth: mockClearAuth });

    render(<HistoryPage />);

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  it('should show loading state while fetching history', () => {
    (api.get as jest.Mock).mockReturnValue(new Promise(() => undefined));

    render(<HistoryPage />);

    expect(screen.getByText(/loading history/i)).toBeInTheDocument();
  });

  it('should render attempt titles and links when history loads', async () => {
    (api.get as jest.Mock).mockResolvedValue({ data: mockAttempts });

    render(<HistoryPage />);

    await waitFor(() => {
      expect(screen.getByRole('link', { name: 'SQL Challenge' })).toHaveAttribute('href', '/challenges/challenge-1');
      expect(screen.getByRole('link', { name: 'Docker Challenge' })).toHaveAttribute('href', '/challenges/challenge-2');
    });
  });

  it('should render correct and incorrect labels', async () => {
    (api.get as jest.Mock).mockResolvedValue({ data: mockAttempts });

    render(<HistoryPage />);

    await waitFor(() => {
      expect(screen.getByText('Correct')).toBeInTheDocument();
      expect(screen.getByText('Incorrect')).toBeInTheDocument();
    });
  });

  it('should render elapsed seconds for each attempt', async () => {
    (api.get as jest.Mock).mockResolvedValue({ data: mockAttempts });

    render(<HistoryPage />);

    await waitFor(() => {
      expect(screen.getByText('42s')).toBeInTheDocument();
      expect(screen.getByText('18s')).toBeInTheDocument();
    });
  });

  it('should show empty state when history has no attempts', async () => {
    (api.get as jest.Mock).mockResolvedValue({ data: [] });

    render(<HistoryPage />);

    await waitFor(() => {
      expect(screen.getByText(/no attempts yet/i)).toBeInTheDocument();
    });
  });

  it('should clear auth and redirect on 401', async () => {
    (api.get as jest.Mock).mockRejectedValue({ response: { status: 401 } });

    render(<HistoryPage />);

    await waitFor(() => {
      expect(mockClearAuth).toHaveBeenCalled();
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  it('should show an alert on generic fetch failure', async () => {
    (api.get as jest.Mock).mockRejectedValue({ response: { status: 500 } });

    render(<HistoryPage />);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/failed to load/i);
    });
  });
});