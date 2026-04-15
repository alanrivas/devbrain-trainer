import { render, screen, waitFor } from '@testing-library/react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/components/AuthContext';
import api from '@/lib/api';
import StatsPage from './page';

jest.mock('next/navigation');
jest.mock('@/components/AuthContext');
jest.mock('@/lib/api');

describe('StatsPage', () => {
  const mockPush = jest.fn();
  const mockClearAuth = jest.fn();

  const mockStats = {
    userId: 'user-1',
    displayName: 'Alan Dev',
    totalAttempts: 42,
    correctAttempts: 30,
    accuracyRate: 71.4,
    currentStreak: 5,
    eloRating: 1200,
    lastAttemptAt: '2026-04-15T10:00:00Z',
  };

  const mockBadges = [
    { type: 'FirstAttempt', earnedAt: '2026-04-10T09:00:00Z' },
    { type: 'TenAttempts', earnedAt: '2026-04-12T11:00:00Z' },
  ];

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
    (useAuth as jest.Mock).mockReturnValue({ token: 'jwt-token', clearAuth: mockClearAuth });
  });

  it('should redirect to login when user has no token', async () => {
    (useAuth as jest.Mock).mockReturnValue({ token: null, clearAuth: mockClearAuth });

    render(<StatsPage />);

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  it('should show loading state while fetching stats', () => {
    (api.get as jest.Mock).mockReturnValue(new Promise(() => undefined));

    render(<StatsPage />);

    expect(screen.getByText(/loading stats/i)).toBeInTheDocument();
  });

  it('should render displayName when stats are loaded', async () => {
    (api.get as jest.Mock)
      .mockResolvedValueOnce({ data: mockStats })
      .mockResolvedValueOnce({ data: mockBadges });

    render(<StatsPage />);

    await waitFor(() => {
      expect(screen.getByText('Alan Dev')).toBeInTheDocument();
    });
  });

  it('should render totalAttempts and correctAttempts', async () => {
    (api.get as jest.Mock)
      .mockResolvedValueOnce({ data: mockStats })
      .mockResolvedValueOnce({ data: mockBadges });

    render(<StatsPage />);

    await waitFor(() => {
      expect(screen.getByText('42')).toBeInTheDocument();
      expect(screen.getByText('30')).toBeInTheDocument();
    });
  });

  it('should render accuracyRate formatted as percentage with one decimal', async () => {
    (api.get as jest.Mock)
      .mockResolvedValueOnce({ data: mockStats })
      .mockResolvedValueOnce({ data: mockBadges });

    render(<StatsPage />);

    await waitFor(() => {
      expect(screen.getByText('71.4%')).toBeInTheDocument();
    });
  });

  it('should show "Never" when lastAttemptAt is null', async () => {
    (api.get as jest.Mock)
      .mockResolvedValueOnce({ data: { ...mockStats, lastAttemptAt: null } })
      .mockResolvedValueOnce({ data: [] });

    render(<StatsPage />);

    await waitFor(() => {
      expect(screen.getByText('Never')).toBeInTheDocument();
    });
  });

  it('should render badge types when user has badges', async () => {
    (api.get as jest.Mock)
      .mockResolvedValueOnce({ data: mockStats })
      .mockResolvedValueOnce({ data: mockBadges });

    render(<StatsPage />);

    await waitFor(() => {
      expect(screen.getByText('FirstAttempt')).toBeInTheDocument();
      expect(screen.getByText('TenAttempts')).toBeInTheDocument();
    });
  });

  it('should show "No badges earned yet" when user has no badges', async () => {
    (api.get as jest.Mock)
      .mockResolvedValueOnce({ data: mockStats })
      .mockResolvedValueOnce({ data: [] });

    render(<StatsPage />);

    await waitFor(() => {
      expect(screen.getByText(/no badges earned yet/i)).toBeInTheDocument();
    });
  });

  it('should show error alert on fetch failure', async () => {
    (api.get as jest.Mock).mockRejectedValue({ response: { status: 500 } });

    render(<StatsPage />);

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/failed to load/i);
    });
  });

  it('should clear auth and redirect on 401', async () => {
    (api.get as jest.Mock).mockRejectedValue({ response: { status: 401 } });

    render(<StatsPage />);

    await waitFor(() => {
      expect(mockClearAuth).toHaveBeenCalled();
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });
});
