import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useRouter } from 'next/navigation';
import OrderingForm from './OrderingForm';
import { useAuth } from './AuthContext';
import api from '@/lib/api';

jest.mock('next/navigation');
jest.mock('./AuthContext');
jest.mock('@/lib/api');

// Mock @dnd-kit — render items as plain divs with test IDs
jest.mock('@dnd-kit/core', () => ({
  DndContext: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  closestCenter: jest.fn(),
  KeyboardSensor: jest.fn(),
  PointerSensor: jest.fn(),
  useSensor: jest.fn(),
  useSensors: jest.fn(() => []),
}));

jest.mock('@dnd-kit/sortable', () => ({
  SortableContext: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  sortableKeyboardCoordinates: jest.fn(),
  useSortable: (args: { id: string; disabled?: boolean }) => ({
    attributes: { 'data-disabled': args.disabled },
    listeners: {},
    setNodeRef: jest.fn(),
    transform: null,
    transition: null,
    isDragging: false,
  }),
  verticalListSortingStrategy: jest.fn(),
  arrayMove: (arr: string[], from: number, to: number) => {
    const copy = [...arr];
    copy.splice(to, 0, copy.splice(from, 1)[0]);
    return copy;
  },
  CSS: { Transform: { toString: jest.fn(() => '') } },
}));

describe('OrderingForm', () => {
  const mockPush = jest.fn();
  const mockClearAuth = jest.fn();
  const items = ['Build', 'Test', 'Lint', 'Deploy'];

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
    (useAuth as jest.Mock).mockReturnValue({ clearAuth: mockClearAuth });
  });

  // Rendering

  it('should render one draggable item per element in items', () => {
    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    items.forEach((item) => expect(screen.getByText(item)).toBeInTheDocument());
  });

  it('should display the text of each item', () => {
    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    items.forEach((item) => expect(screen.getByText(item)).toBeVisible());
  });

  it('should show instruction "Drag items into the correct order"', () => {
    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    expect(screen.getByText(/drag items into the correct order/i)).toBeInTheDocument();
  });

  it('should show Submit Order button enabled from the start', () => {
    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    expect(screen.getByRole('button', { name: /submit order/i })).toBeEnabled();
  });

  // State and interaction

  it('should show timer on mount', () => {
    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    expect(screen.getByText(/time remaining/i)).toBeInTheDocument();
  });

  it('should show all items in the DOM initially', () => {
    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    const count = items.filter((item) => screen.queryByText(item) !== null).length;
    expect(count).toBe(items.length);
  });

  it('should show keyboard instruction text', () => {
    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    expect(screen.getByText(/use arrow keys to reorder when focused/i)).toBeInTheDocument();
  });

  // Submit

  it('should call POST with userAnswer pipe-separated in current order', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'Build|Test|Lint|Deploy', isCorrect: true, elapsedSeconds: 10 },
    });

    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    await user.click(screen.getByRole('button', { name: /submit order/i }));

    await waitFor(() => {
      expect(api.post).toHaveBeenCalledWith('/challenges/c-1/attempt', {
        userAnswer: expect.stringMatching(/^[^|]+(\|[^|]+){3}$/),
        elapsedSeconds: expect.any(Number),
      });
    });
  });

  it('should disable submit button during loading', async () => {
    const user = userEvent.setup();
    let resolveRequest!: (value: unknown) => void;
    const pending = new Promise((resolve) => { resolveRequest = resolve; });
    (api.post as jest.Mock).mockReturnValue(pending);

    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    await user.click(screen.getByRole('button', { name: /submit order/i }));

    await waitFor(() => expect(screen.getByRole('button', { name: /submitting/i })).toBeDisabled());

    resolveRequest({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'Build|Test|Lint|Deploy', isCorrect: true, elapsedSeconds: 10 },
    });
    await waitFor(() => expect(screen.getByText(/correct!/i)).toBeInTheDocument());
  });

  it('should show "Correct!" when result is correct', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'Build|Test|Lint|Deploy', isCorrect: true, elapsedSeconds: 10 },
    });

    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    await user.click(screen.getByRole('button', { name: /submit order/i }));

    await waitFor(() => expect(screen.getByText(/correct!/i)).toBeInTheDocument());
  });

  it('should show "Not quite" and correct order when incorrect', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: {
        attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1',
        userAnswer: 'Deploy|Build|Test|Lint', isCorrect: false,
        correctAnswer: 'Build|Test|Lint|Deploy', elapsedSeconds: 20,
      },
    });

    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    await user.click(screen.getByRole('button', { name: /submit order/i }));

    await waitFor(() => {
      expect(screen.getByText(/not quite/i)).toBeInTheDocument();
      expect(screen.getByText(/correct order: build\|test\|lint\|deploy/i)).toBeInTheDocument();
    });
  });

  // Reset and result

  it('should clear result when "Try again" is clicked', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'Build|Test|Lint|Deploy', isCorrect: true, elapsedSeconds: 5 },
    });

    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    await user.click(screen.getByRole('button', { name: /submit order/i }));
    await waitFor(() => expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument());

    await user.click(screen.getByRole('button', { name: /try again/i }));

    expect(screen.queryByText(/correct!/i)).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: /submit order/i })).toBeEnabled();
  });

  it('should display ELO rating when present', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'Build|Test|Lint|Deploy', isCorrect: true, elapsedSeconds: 10, newEloRating: 1200 },
    });

    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    await user.click(screen.getByRole('button', { name: /submit order/i }));

    await waitFor(() => expect(screen.getByText(/elo: 1200/i)).toBeInTheDocument());
  });

  it('should display new badges when present', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { attemptId: 'a-1', challengeId: 'c-1', userId: 'u-1', userAnswer: 'Build|Test|Lint|Deploy', isCorrect: true, elapsedSeconds: 10, newBadges: ['Architect'] },
    });

    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    await user.click(screen.getByRole('button', { name: /submit order/i }));

    await waitFor(() => expect(screen.getByText(/new badge: Architect/i)).toBeInTheDocument());
  });

  // Error handling

  it('should clear auth and redirect to login on 401', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 401 } });

    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    await user.click(screen.getByRole('button', { name: /submit order/i }));

    await waitFor(() => {
      expect(mockClearAuth).toHaveBeenCalled();
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  it('should show "Challenge not found." on 404', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 404 } });

    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    await user.click(screen.getByRole('button', { name: /submit order/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/challenge not found\./i);
    });
  });

  it('should show "Server error. Try again." on 500', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 500 } });

    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    await user.click(screen.getByRole('button', { name: /submit order/i }));

    await waitFor(() => {
      expect(screen.getByRole('alert')).toHaveTextContent(/server error\. try again\./i);
    });
  });

  // Accessibility

  it('should render error messages with role="alert"', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue({ response: { status: 500 } });

    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    await user.click(screen.getByRole('button', { name: /submit order/i }));

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
  });

  it('should show keyboard instruction text', () => {
    render(<OrderingForm challengeId="c-1" timeLimitSecs={90} items={items} />);
    expect(screen.getByText(/use arrow keys to reorder when focused/i)).toBeInTheDocument();
  });
});
