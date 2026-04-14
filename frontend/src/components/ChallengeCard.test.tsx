import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { act } from 'react';
import ChallengeCard from './ChallengeCard';

interface Challenge {
  id: string;
  title: string;
  description: string;
  category: string;
  difficulty: 'Easy' | 'Medium' | 'Hard';
}

describe('ChallengeCard', () => {
  const mockOnAttempt = jest.fn();
  const mockChallenge: Challenge = {
    id: 'challenge-1',
    title: 'SQL Query Optimization',
    description: 'Learn how to optimize slow database queries',
    category: 'SQL',
    difficulty: 'Medium',
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Rendering', () => {
    it('should render challenge card with all content', () => {
      render(<ChallengeCard challenge={mockChallenge} onAttempt={mockOnAttempt} />);
      expect(screen.getByText('SQL Query Optimization')).toBeInTheDocument();
      expect(screen.getByText('Learn how to optimize slow database queries')).toBeInTheDocument();
      expect(screen.getByText('SQL')).toBeInTheDocument();
      expect(screen.getByText('Medium')).toBeInTheDocument();
    });

    it('should display title as heading', () => {
      render(<ChallengeCard challenge={mockChallenge} onAttempt={mockOnAttempt} />);
      const title = screen.getByText('SQL Query Optimization');
      expect(title.tagName).toBe('H3');
    });

    it('should display description text', () => {
      render(<ChallengeCard challenge={mockChallenge} onAttempt={mockOnAttempt} />);
      expect(screen.getByText(/Learn how to optimize/i)).toBeInTheDocument();
    });

    it('should display category badge', () => {
      render(<ChallengeCard challenge={mockChallenge} onAttempt={mockOnAttempt} />);
      expect(screen.getByText('SQL')).toBeInTheDocument();
    });

    it('should have attempt button', () => {
      render(<ChallengeCard challenge={mockChallenge} onAttempt={mockOnAttempt} />);
      expect(screen.getByRole('button', { name: /attempt/i })).toBeInTheDocument();
    });
  });

  describe('Styling by Difficulty - Easy', () => {
    it('should have green styling for Easy difficulty', () => {
      const easyChallenenge: Challenge = { ...mockChallenge, difficulty: 'Easy' };
      const { container } = render(<ChallengeCard challenge={easyChallenenge} onAttempt={mockOnAttempt} />);
      const badge = container.querySelector('.bg-green-100');
      expect(badge).toBeInTheDocument();
    });

    it('should have green text for Easy difficulty', () => {
      const easyChallenenge: Challenge = { ...mockChallenge, difficulty: 'Easy' };
      const { container } = render(<ChallengeCard challenge={easyChallenenge} onAttempt={mockOnAttempt} />);
      const badge = container.querySelector('.text-green-800');
      expect(badge).toBeInTheDocument();
    });
  });

  describe('Styling by Difficulty - Medium', () => {
    it('should have yellow styling for Medium difficulty', () => {
      const mediumChallenge: Challenge = { ...mockChallenge, difficulty: 'Medium' };
      const { container } = render(<ChallengeCard challenge={mediumChallenge} onAttempt={mockOnAttempt} />);
      const badge = container.querySelector('.bg-yellow-100');
      expect(badge).toBeInTheDocument();
    });

    it('should have yellow text for Medium difficulty', () => {
      const mediumChallenge: Challenge = { ...mockChallenge, difficulty: 'Medium' };
      const { container } = render(<ChallengeCard challenge={mediumChallenge} onAttempt={mockOnAttempt} />);
      const badge = container.querySelector('.text-yellow-800');
      expect(badge).toBeInTheDocument();
    });
  });

  describe('Styling by Difficulty - Hard', () => {
    it('should have red styling for Hard difficulty', () => {
      const hardChallenge: Challenge = { ...mockChallenge, difficulty: 'Hard' };
      const { container } = render(<ChallengeCard challenge={hardChallenge} onAttempt={mockOnAttempt} />);
      const badge = container.querySelector('.bg-red-100');
      expect(badge).toBeInTheDocument();
    });

    it('should have red text for Hard difficulty', () => {
      const hardChallenge: Challenge = { ...mockChallenge, difficulty: 'Hard' };
      const { container } = render(<ChallengeCard challenge={hardChallenge} onAttempt={mockOnAttempt} />);
      const badge = container.querySelector('.text-red-800');
      expect(badge).toBeInTheDocument();
    });
  });

  describe('Interaction', () => {
    it('should call onAttempt when button clicked', async () => {
      const user = userEvent.setup();
      render(<ChallengeCard challenge={mockChallenge} onAttempt={mockOnAttempt} />);
      
      const button = screen.getByRole('button', { name: /attempt/i });
      await user.click(button);
      
      expect(mockOnAttempt).toHaveBeenCalled();
    });

    it('should pass correct challengeId to onAttempt', async () => {
      const user = userEvent.setup();
      render(<ChallengeCard challenge={mockChallenge} onAttempt={mockOnAttempt} />);
      
      const button = screen.getByRole('button', { name: /attempt/i });
      await user.click(button);
      
      expect(mockOnAttempt).toHaveBeenCalledWith('challenge-1');
    });

    it('should be keyboard accessible', async () => {
      const user = userEvent.setup();
      render(<ChallengeCard challenge={mockChallenge} onAttempt={mockOnAttempt} />);
      
      const button = screen.getByRole('button', { name: /attempt/i });
      button.focus();
      expect(button).toHaveFocus();
      
      await user.keyboard('{Enter}');
      expect(mockOnAttempt).toHaveBeenCalled();
    });

    it('should have hover effect (shadow) on card', () => {
      const { container } = render(<ChallengeCard challenge={mockChallenge} onAttempt={mockOnAttempt} />);
      const card = container.querySelector('.shadow');
      expect(card).toBeInTheDocument();
      expect(card).toHaveClass('hover:shadow-lg', 'transition');
    });

    it('should have hover effect on button', () => {
      const { container } = render(<ChallengeCard challenge={mockChallenge} onAttempt={mockOnAttempt} />);
      const button = screen.getByRole('button');
      expect(button).toHaveClass('hover:text-blue-700');
    });
  });

  describe('Props Validation', () => {
    it('should render with valid Challenge prop', () => {
      render(<ChallengeCard challenge={mockChallenge} onAttempt={mockOnAttempt} />);
      expect(screen.getByText('SQL Query Optimization')).toBeInTheDocument();
    });

    it('should render different challenges correctly', () => {
      const differentChallenge: Challenge = {
        id: 'challenge-2',
        title: 'Docker Basics',
        description: 'Learn containerization fundamentals',
        category: 'DevOps',
        difficulty: 'Easy',
      };
      
      render(<ChallengeCard challenge={differentChallenge} onAttempt={mockOnAttempt} />);
      expect(screen.getByText('Docker Basics')).toBeInTheDocument();
      expect(screen.getByText('DevOps')).toBeInTheDocument();
    });
  });
});
