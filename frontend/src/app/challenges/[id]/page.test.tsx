import { fireEvent, render, screen, waitFor } from '@testing-library/react';
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

jest.mock('@/components/MultipleChoiceForm', () => {
  return function MockMultipleChoiceForm(props: {
    challengeId: string;
    timeLimitSecs: number;
    options: string[];
  }) {
    return (
      <div data-testid="multiple-choice-form" data-challenge-id={props.challengeId}>
        {props.options?.map((opt: string) => <span key={opt}>{opt}</span>)}
      </div>
    );
  };
});

jest.mock('@/components/CodeRunnerForm', () => ({
  __esModule: true,
  default: function MockCodeRunnerForm(props: {
    challengeId: string;
    starterCode: string;
    testCases: Array<{ description: string }>;
  }) {
    return (
      <div data-testid="code-runner-form" data-challenge-id={props.challengeId}>
        <span data-testid="starter-code">{props.starterCode}</span>
        {props.testCases?.map((tc, i) => <span key={i}>{tc.description}</span>)}
      </div>
    );
  },
}));

jest.mock('@/components/OrderingForm', () => {
  return function MockOrderingForm(props: {
    challengeId: string;
    timeLimitSecs: number;
    items: string[];
  }) {
    return (
      <div data-testid="ordering-form" data-challenge-id={props.challengeId}>
        {props.items?.map((item: string) => <span key={item}>{item}</span>)}
      </div>
    );
  };
});

describe('ChallengeDetailPage', () => {
  const mockPush = jest.fn();
  const mockClearAuth = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    sessionStorage.clear();
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

  describe('Challenge navigation', () => {
    afterEach(() => {
      sessionStorage.clear();
    });

    it('should show both previous and next links for a middle challenge', async () => {
      sessionStorage.setItem('challenge-list-ids', JSON.stringify(['ch-0', 'challenge-1', 'ch-2']));
      (api.get as jest.Mock).mockResolvedValue({
        data: { id: 'challenge-1', title: 'Mid Challenge', description: 'Desc', category: 'Sql', difficulty: 'Easy', timeLimitSecs: 60 },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => {
        expect(screen.getByRole('link', { name: /← previous challenge/i })).toBeInTheDocument();
        expect(screen.getByRole('link', { name: /next challenge →/i })).toBeInTheDocument();
      });
    });

    it('should only show next link for the first challenge in the list', async () => {
      (useParams as jest.Mock).mockReturnValue({ id: 'ch-0' });
      sessionStorage.setItem('challenge-list-ids', JSON.stringify(['ch-0', 'ch-1', 'ch-2']));
      (api.get as jest.Mock).mockResolvedValue({
        data: { id: 'ch-0', title: 'First Challenge', description: 'Desc', category: 'Sql', difficulty: 'Easy', timeLimitSecs: 60 },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => {
        expect(screen.queryByRole('link', { name: /← previous challenge/i })).not.toBeInTheDocument();
        expect(screen.getByRole('link', { name: /next challenge →/i })).toBeInTheDocument();
      });
    });

    it('should only show previous link for the last challenge in the list', async () => {
      (useParams as jest.Mock).mockReturnValue({ id: 'ch-2' });
      sessionStorage.setItem('challenge-list-ids', JSON.stringify(['ch-0', 'ch-1', 'ch-2']));
      (api.get as jest.Mock).mockResolvedValue({
        data: { id: 'ch-2', title: 'Last Challenge', description: 'Desc', category: 'Sql', difficulty: 'Easy', timeLimitSecs: 60 },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => {
        expect(screen.getByRole('link', { name: /← previous challenge/i })).toBeInTheDocument();
        expect(screen.queryByRole('link', { name: /next challenge →/i })).not.toBeInTheDocument();
      });
    });

    it('should show no navigation links when challenge is the only one in the list', async () => {
      sessionStorage.setItem('challenge-list-ids', JSON.stringify(['challenge-1']));
      (api.get as jest.Mock).mockResolvedValue({
        data: { id: 'challenge-1', title: 'Only Challenge', description: 'Desc', category: 'Sql', difficulty: 'Easy', timeLimitSecs: 60 },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => expect(screen.getByText('Only Challenge')).toBeInTheDocument());

      expect(screen.queryByRole('link', { name: /← previous challenge/i })).not.toBeInTheDocument();
      expect(screen.queryByRole('link', { name: /next challenge →/i })).not.toBeInTheDocument();
    });

    it('should show no navigation links when sessionStorage has no list', async () => {
      (api.get as jest.Mock).mockResolvedValue({
        data: { id: 'challenge-1', title: 'Direct Access', description: 'Desc', category: 'Sql', difficulty: 'Easy', timeLimitSecs: 60 },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => expect(screen.getByText('Direct Access')).toBeInTheDocument());

      expect(screen.queryByRole('link', { name: /← previous challenge/i })).not.toBeInTheDocument();
      expect(screen.queryByRole('link', { name: /next challenge →/i })).not.toBeInTheDocument();
    });

    it('should link next button to the correct challenge id', async () => {
      sessionStorage.setItem('challenge-list-ids', JSON.stringify(['ch-0', 'challenge-1', 'ch-2']));
      (api.get as jest.Mock).mockResolvedValue({
        data: { id: 'challenge-1', title: 'Mid Challenge', description: 'Desc', category: 'Sql', difficulty: 'Easy', timeLimitSecs: 60 },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => {
        const nextLink = screen.getByRole('link', { name: /next challenge →/i });
        expect(nextLink).toHaveAttribute('href', '/challenges/ch-2');
      });
    });

    it('should link previous button to the correct challenge id', async () => {
      sessionStorage.setItem('challenge-list-ids', JSON.stringify(['ch-0', 'challenge-1', 'ch-2']));
      (api.get as jest.Mock).mockResolvedValue({
        data: { id: 'challenge-1', title: 'Mid Challenge', description: 'Desc', category: 'Sql', difficulty: 'Easy', timeLimitSecs: 60 },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => {
        const prevLink = screen.getByRole('link', { name: /← previous challenge/i });
        expect(prevLink).toHaveAttribute('href', '/challenges/ch-0');
      });
    });
  });

  describe('Keyboard navigation', () => {
    const challengeData = {
      id: 'challenge-1',
      title: 'SQL Challenge',
      description: 'Write a query',
      category: 'Sql',
      difficulty: 'Easy' as const,
      timeLimitSecs: 60,
    };

    beforeEach(() => {
      sessionStorage.setItem('challenge-list-ids', JSON.stringify(['ch-prev', 'challenge-1', 'ch-next']));
      (api.get as jest.Mock).mockResolvedValue({ data: challengeData });
    });

    it('ArrowRight_GivenNextIdExists_Should_NavigateToNextChallenge', async () => {
      render(<ChallengeDetailPage />);
      // Wait for nextId to be set (nav link visible confirms the keyboard effect has re-registered)
      await waitFor(() => expect(screen.getByRole('link', { name: /next challenge/i })).toBeInTheDocument());

      fireEvent.keyDown(document, { key: 'ArrowRight' });

      expect(mockPush).toHaveBeenCalledWith('/challenges/ch-next');
    });

    it('ArrowLeft_GivenPrevIdExists_Should_NavigateToPrevChallenge', async () => {
      render(<ChallengeDetailPage />);
      // Wait for prevId to be set (nav link visible confirms the keyboard effect has re-registered)
      await waitFor(() => expect(screen.getByRole('link', { name: /← previous challenge/i })).toBeInTheDocument());

      fireEvent.keyDown(document, { key: 'ArrowLeft' });

      expect(mockPush).toHaveBeenCalledWith('/challenges/ch-prev');
    });

    it('ArrowRight_GivenNoNextId_Should_NotNavigate', async () => {
      sessionStorage.setItem('challenge-list-ids', JSON.stringify(['ch-prev', 'challenge-1']));
      render(<ChallengeDetailPage />);
      await waitFor(() => expect(screen.getByText('SQL Challenge')).toBeInTheDocument());

      fireEvent.keyDown(document, { key: 'ArrowRight' });

      expect(mockPush).not.toHaveBeenCalledWith(expect.stringContaining('/challenges/ch'));
    });

    it('ArrowLeft_GivenNoPrevId_Should_NotNavigate', async () => {
      sessionStorage.setItem('challenge-list-ids', JSON.stringify(['challenge-1', 'ch-next']));
      render(<ChallengeDetailPage />);
      await waitFor(() => expect(screen.getByText('SQL Challenge')).toBeInTheDocument());

      fireEvent.keyDown(document, { key: 'ArrowLeft' });

      expect(mockPush).not.toHaveBeenCalledWith(expect.stringContaining('/challenges/ch'));
    });

    it('Escape_Should_NavigateBackToChallengesList', async () => {
      render(<ChallengeDetailPage />);
      await waitFor(() => expect(screen.getByText('SQL Challenge')).toBeInTheDocument());

      fireEvent.keyDown(document, { key: 'Escape' });

      expect(mockPush).toHaveBeenCalledWith('/challenges');
    });
  });

  describe('Challenge type rendering', () => {
    it('should render MultipleChoiceForm for MultipleChoice challenges', async () => {
      (api.get as jest.Mock).mockResolvedValue({
        data: {
          id: 'challenge-1',
          title: 'Binary Search',
          description: 'Select the complexity',
          category: 'Algorithms',
          difficulty: 'Medium',
          timeLimitSecs: 60,
          type: 'MultipleChoice',
          options: ['O(n)', 'O(log n)', 'O(n²)', 'O(n log n)'],
        },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => {
        expect(screen.getByTestId('multiple-choice-form')).toBeInTheDocument();
        expect(screen.queryByTestId('attempt-form')).not.toBeInTheDocument();
      });
    });

    it('should render AttemptForm for OpenText challenges', async () => {
      (api.get as jest.Mock).mockResolvedValue({
        data: {
          id: 'challenge-1',
          title: 'SQL Challenge',
          description: 'Write a query',
          category: 'Sql',
          difficulty: 'Easy',
          timeLimitSecs: 60,
          type: 'OpenText',
          options: [],
        },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => {
        expect(screen.getByTestId('attempt-form')).toBeInTheDocument();
        expect(screen.queryByTestId('multiple-choice-form')).not.toBeInTheDocument();
      });
    });

    it('should pass options to MultipleChoiceForm', async () => {
      const options = ['O(n)', 'O(log n)', 'O(n²)', 'O(n log n)'];
      (api.get as jest.Mock).mockResolvedValue({
        data: {
          id: 'challenge-1',
          title: 'Binary Search',
          description: 'Select complexity',
          category: 'Algorithms',
          difficulty: 'Medium',
          timeLimitSecs: 60,
          type: 'MultipleChoice',
          options,
        },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => {
        options.forEach((opt) => expect(screen.getByText(opt)).toBeInTheDocument());
      });
    });

    it('should render CodeRunnerForm for CodeRunner challenges', async () => {
      (api.get as jest.Mock).mockResolvedValue({
        data: {
          id: 'challenge-1',
          title: 'JS: Sum Two Numbers',
          description: 'Write a function that sums two numbers.',
          category: 'CodeLogic',
          difficulty: 'Easy',
          timeLimitSecs: 120,
          type: 'CodeRunner',
          options: [],
          starterCode: 'function solution(a, b) {}',
          testCases: [{ input: '2, 3', expectedOutput: '5', description: 'Returns sum of 2 and 3' }],
        },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => {
        expect(screen.getByTestId('code-runner-form')).toBeInTheDocument();
        expect(screen.queryByTestId('attempt-form')).not.toBeInTheDocument();
        expect(screen.queryByTestId('multiple-choice-form')).not.toBeInTheDocument();
      });
    });

    it('should pass starterCode and testCases to CodeRunnerForm', async () => {
      (api.get as jest.Mock).mockResolvedValue({
        data: {
          id: 'challenge-1',
          title: 'JS: Sum Two Numbers',
          description: 'Write a function that sums two numbers.',
          category: 'CodeLogic',
          difficulty: 'Easy',
          timeLimitSecs: 120,
          type: 'CodeRunner',
          options: [],
          starterCode: 'function solution(a, b) {}',
          testCases: [{ input: '2, 3', expectedOutput: '5', description: 'Returns sum of 2 and 3' }],
        },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => {
        expect(screen.getByTestId('starter-code')).toHaveTextContent('function solution(a, b) {}');
        expect(screen.getByText('Returns sum of 2 and 3')).toBeInTheDocument();
      });
    });

    it('should render OrderingForm for Ordering challenges', async () => {
      (api.get as jest.Mock).mockResolvedValue({
        data: {
          id: 'challenge-1',
          title: 'Architecture: Hexagonal Layers',
          description: 'Order the layers from innermost to outermost.',
          category: 'Architecture',
          difficulty: 'Medium',
          timeLimitSecs: 90,
          type: 'Ordering',
          options: [],
          items: ['UI', 'Infrastructure', 'Domain', 'Application'],
        },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => {
        expect(screen.getByTestId('ordering-form')).toBeInTheDocument();
        expect(screen.queryByTestId('attempt-form')).not.toBeInTheDocument();
        expect(screen.queryByTestId('multiple-choice-form')).not.toBeInTheDocument();
      });
    });

    it('should pass items to OrderingForm', async () => {
      const items = ['UI', 'Infrastructure', 'Domain', 'Application'];
      (api.get as jest.Mock).mockResolvedValue({
        data: {
          id: 'challenge-1',
          title: 'Architecture: Hexagonal Layers',
          description: 'Order the layers.',
          category: 'Architecture',
          difficulty: 'Medium',
          timeLimitSecs: 90,
          type: 'Ordering',
          options: [],
          items,
        },
      });

      render(<ChallengeDetailPage />);

      await waitFor(() => {
        items.forEach((item) => expect(screen.getByText(item)).toBeInTheDocument());
      });
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
      expect(screen.getByText(/use the actions in the attempt card/i)).toBeInTheDocument();
    });
  });
});
