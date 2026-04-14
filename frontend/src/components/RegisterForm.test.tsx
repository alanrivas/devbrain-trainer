import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { act } from 'react';
import { useRouter } from 'next/navigation';
import RegisterForm from './RegisterForm';
import { useAuth } from './AuthContext';
import api from '@/lib/api';

jest.mock('next/navigation');
jest.mock('./AuthContext');
jest.mock('@/lib/api');

describe('RegisterForm', () => {
  const mockPush = jest.fn();
  const mockSetAuth = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue({ push: mockPush });
    (useAuth as jest.Mock).mockReturnValue({ setAuth: mockSetAuth });
  });

  it('renders form with all inputs', () => {
    render(<RegisterForm />);
    expect(screen.getByLabelText(/display name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^password/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/confirm password/i)).toBeInTheDocument();
  });

  it('accepts user input', async () => {
    const user = userEvent.setup();
    render(<RegisterForm />);
    const input = screen.getByLabelText(/^email/i) as HTMLInputElement;
    await user.type(input, 'test@example.com');
    expect(input.value).toBe('test@example.com');
  });

  it('calls API on submission', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { token: 'test', user: { id: '1' } },
    });

    render(<RegisterForm />);
    await user.type(screen.getByLabelText(/display name/i), 'John');
    await user.type(screen.getByLabelText(/^email/i), 'john@test.com');
    await user.type(screen.getByLabelText(/^password/i), 'Password123');
    await user.type(screen.getByLabelText(/confirm password/i), 'Password123');

    await act(async () => {
      fireEvent.click(screen.getByRole('button'));
    });

    await waitFor(() => expect(api.post).toHaveBeenCalled());
  });

  it('redirects after success', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { token: 'test', user: { id: '1' } },
    });

    render(<RegisterForm />);
    await user.type(screen.getByLabelText(/display name/i), 'John');
    await user.type(screen.getByLabelText(/^email/i), 'john@test.com');
    await user.type(screen.getByLabelText(/^password/i), 'Password123');
    await user.type(screen.getByLabelText(/confirm password/i), 'Password123');

    await act(async () => {
      fireEvent.click(screen.getByRole('button'));
    });

    await waitFor(() => expect(mockPush).toHaveBeenCalledWith('/challenges'));
  });

  it('shows error on failure', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue(new Error('Failed'));

    render(<RegisterForm />);
    await user.type(screen.getByLabelText(/display name/i), 'John');
    await user.type(screen.getByLabelText(/^email/i), 'john@test.com');
    await user.type(screen.getByLabelText(/^password/i), 'Password123');
    await user.type(screen.getByLabelText(/confirm password/i), 'Password123');

    await act(async () => {
      fireEvent.click(screen.getByRole('button'));
    });

    await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
  });

  it('clears form on success', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { token: 'test', user: { id: '1' } },
    });

    render(<RegisterForm />);
    const displayInput = screen.getByLabelText(/display name/i) as HTMLInputElement;
    const emailInput = screen.getByLabelText(/^email/i) as HTMLInputElement;
    const passwordInput = screen.getByLabelText(/^password/i) as HTMLInputElement;
    const confirmInput = screen.getByLabelText(/confirm password/i) as HTMLInputElement;

    await user.type(displayInput, 'John');
    await user.type(emailInput, 'john@test.com');
    await user.type(passwordInput, 'Password123');
    await user.type(confirmInput, 'Password123');

    await act(async () => {
      fireEvent.click(screen.getByRole('button'));
    });

    await waitFor(() => {
      expect(displayInput.value).toBe('');
      expect(emailInput.value).toBe('');
      expect(passwordInput.value).toBe('');
      expect(confirmInput.value).toBe('');
    });
  });

  it('renders submit button', () => {
    render(<RegisterForm />);
    expect(screen.getByRole('button', { name: /create account/i })).toBeInTheDocument();
  });

  it('renders link to sign in', () => {
    render(<RegisterForm />);
    expect(screen.getByRole('link', { name: /sign in/i })).toBeInTheDocument();
  });

  it('renders password hint text', () => {
    render(<RegisterForm />);
    expect(screen.getByText(/min 8 chars/i)).toBeInTheDocument();
  });

  it('uses password input type', () => {
    render(<RegisterForm />);
    const passwordInput = screen.getByLabelText(/^password/i) as HTMLInputElement;
    expect(passwordInput.type).toBe('password');
  });

  it('uses email input type', () => {
    render(<RegisterForm />);
    const emailInput = screen.getByLabelText(/^email/i) as HTMLInputElement;
    expect(emailInput.type).toBe('email');
  });

  it('disables fields during submission', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockImplementation(
      () => new Promise(resolve => setTimeout(() => resolve({
        data: { token: 'test', user: { id: '1' } },
      }), 200))
    );

    render(<RegisterForm />);
    await user.type(screen.getByLabelText(/display name/i), 'John');
    await user.type(screen.getByLabelText(/^email/i), 'john@test.com');
    await user.type(screen.getByLabelText(/^password/i), 'Password123');
    await user.type(screen.getByLabelText(/confirm password/i), 'Password123');

    const button = screen.getByRole('button') as HTMLButtonElement;
    await act(async () => {
      fireEvent.click(button);
    });

    expect(button.disabled).toBe(true);
  });

  it('calls setAuth on success', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockResolvedValue({
      data: { token: 'test-token', user: { id: '1', email: 'john@test.com' } },
    });

    render(<RegisterForm />);
    await user.type(screen.getByLabelText(/display name/i), 'John');
    await user.type(screen.getByLabelText(/^email/i), 'john@test.com');
    await user.type(screen.getByLabelText(/^password/i), 'Password123');
    await user.type(screen.getByLabelText(/confirm password/i), 'Password123');

    await act(async () => {
      fireEvent.click(screen.getByRole('button'));
    });

    await waitFor(() => expect(mockSetAuth).toHaveBeenCalledWith('test-token', { id: '1', email: 'john@test.com' }));
  });

  it('preserves displayName and email on error', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue(new Error('Failed'));

    render(<RegisterForm />);
    const displayInput = screen.getByLabelText(/display name/i) as HTMLInputElement;
    const emailInput = screen.getByLabelText(/^email/i) as HTMLInputElement;

    await user.type(displayInput, 'John');
    await user.type(emailInput, 'john@test.com');
    await user.type(screen.getByLabelText(/^password/i), 'Password123');
    await user.type(screen.getByLabelText(/confirm password/i), 'Password123');

    await act(async () => {
      fireEvent.click(screen.getByRole('button'));
    });

    await waitFor(() => {
      expect(displayInput.value).toBe('John');
      expect(emailInput.value).toBe('john@test.com');
    });
  });

  it('clears passwords on error', async () => {
    const user = userEvent.setup();
    (api.post as jest.Mock).mockRejectedValue(new Error('Failed'));

    render(<RegisterForm />);
    const passwordInput = screen.getByLabelText(/^password/i) as HTMLInputElement;
    const confirmInput = screen.getByLabelText(/confirm password/i) as HTMLInputElement;

    await user.type(screen.getByLabelText(/display name/i), 'John');
    await user.type(screen.getByLabelText(/^email/i), 'john@test.com');
    await user.type(passwordInput, 'Password123');
    await user.type(confirmInput, 'Password123');

    await act(async () => {
      fireEvent.click(screen.getByRole('button'));
    });

    await waitFor(() => {
      expect(passwordInput.value).toBe('');
      expect(confirmInput.value).toBe('');
    });
  });

  it('has labels with htmlFor attributes', () => {
    render(<RegisterForm />);
    // Verify all inputs are accessible via their labels
    expect(screen.getByLabelText(/display name/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^password/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/confirm password/i)).toBeInTheDocument();
  });

  it('allows keyboard focus on inputs', () => {
    render(<RegisterForm />);
    const emailInput = screen.getByLabelText(/^email/i) as HTMLInputElement;
    emailInput.focus();
    expect(emailInput).toHaveFocus();
  });

  it('requires all fields', () => {
    const { container } = render(<RegisterForm />);
    const inputs = container.querySelectorAll('input[required]');
    expect(inputs.length).toBeGreaterThan(0);
  });

  it('has form with proper spacing', () => {
    const { container } = render(<RegisterForm />);
    const form = container.querySelector('form');
    expect(form).toHaveClass('space-y-4');
  });

  it('renders in a white card', () => {
    const { container } = render(<RegisterForm />);
    const card = container.querySelector('.bg-white');
    expect(card).toBeInTheDocument();
    expect(card).toHaveClass('rounded-lg', 'shadow-md');
  });
});