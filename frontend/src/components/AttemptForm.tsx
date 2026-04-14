'use client';

import Link from 'next/link';
import { useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from './AuthContext';
import api from '@/lib/api';

export interface AttemptResult {
  attemptId: string;
  challengeId: string;
  userId: string;
  userAnswer: string;
  isCorrect: boolean;
  correctAnswer?: string;
  elapsedSeconds: number;
  challengeTitle?: string;
  occurredAt?: string;
}

interface AttemptFormProps {
  challengeId: string;
  timeLimitSecs: number;
  onSuccess?: (result: AttemptResult) => void;
}

export default function AttemptForm({ challengeId, timeLimitSecs, onSuccess }: AttemptFormProps) {
  const [userAnswer, setUserAnswer] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<AttemptResult | null>(null);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const startedAtMs = useRef<number>(Date.now());
  const router = useRouter();
  const { clearAuth } = useAuth();

  useEffect(() => {
    if (result || loading) {
      return;
    }

    const interval = window.setInterval(() => {
      setElapsedSeconds(Math.max(0, Math.floor((Date.now() - startedAtMs.current) / 1000)));
    }, 1000);

    return () => window.clearInterval(interval);
  }, [loading, result]);

  const remainingSeconds = Math.max(timeLimitSecs - elapsedSeconds, 0);
  const remainingRatio = timeLimitSecs > 0 ? remainingSeconds / timeLimitSecs : 0;
  const isWarning = remainingRatio <= 0.5 && remainingRatio > 0.2;
  const isCritical = remainingRatio <= 0.2;

  const timerTone = isCritical
    ? 'border-red-200 bg-red-50 text-red-700'
    : isWarning
      ? 'border-amber-200 bg-amber-50 text-amber-700'
      : 'border-blue-200 bg-blue-50 text-blue-700';

  const progressTone = isCritical
    ? 'bg-red-500'
    : isWarning
      ? 'bg-amber-500'
      : 'bg-blue-500';

  const resetForm = () => {
    startedAtMs.current = Date.now();
    setElapsedSeconds(0);
    setUserAnswer('');
    setError('');
    setResult(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    const trimmedAnswer = userAnswer.trim();
    if (!trimmedAnswer) {
      setError('Answer is required');
      return;
    }

    setError('');
    setLoading(true);

    const currentElapsedSeconds = Math.max(0, Math.floor((Date.now() - startedAtMs.current) / 1000));
    setElapsedSeconds(currentElapsedSeconds);

    try {
      const response = await api.post(`/challenges/${challengeId}/attempt`, {
        userAnswer: trimmedAnswer,
        elapsedSeconds: currentElapsedSeconds,
      });

      const attemptResult = response.data as AttemptResult;
      setResult(attemptResult);
      onSuccess?.(attemptResult);
    } catch (err: any) {
      const status = err?.response?.status;

      if (status === 401) {
        clearAuth();
        router.push('/login');
        return;
      }

      if (status === 400) {
        setError('Invalid answer. Please review your input.');
      } else if (status === 404) {
        setError('Challenge not found.');
      } else {
        setError('Server error. Try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="bg-white rounded-lg shadow p-6">
      <h2 className="text-xl font-semibold text-gray-900 mb-2">Submit Your Answer</h2>
      <div className={`mb-4 rounded-lg border p-3 text-sm ${timerTone}`} aria-live="polite">
        <div className="flex items-center justify-between gap-3">
          <span className="font-medium">Time remaining</span>
          <span>{remainingSeconds}s / {timeLimitSecs}s</span>
        </div>
        <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-white/70">
          <div className={`h-full rounded-full transition-all ${progressTone}`} style={{ width: `${Math.max(0, remainingRatio * 100)}%` }} />
        </div>
      </div>

      {error && (
        <div role="alert" className="mb-4 p-3 rounded border border-red-200 bg-red-50 text-red-700 text-sm">
          {error}
        </div>
      )}

      {result && (
        <div
          className={`mb-4 p-3 rounded border text-sm ${
            result.isCorrect
              ? 'border-green-200 bg-green-50 text-green-700'
              : 'border-amber-200 bg-amber-50 text-amber-700'
          }`}
        >
          <p className="font-medium">{result.isCorrect ? 'Correct! Great job.' : 'Incorrect. Keep training.'}</p>
          <p className="mt-1 text-xs opacity-80">
            Submitted in {result.elapsedSeconds}s.
          </p>
          {!result.isCorrect && result.correctAnswer && (
            <p className="mt-1">Correct answer: {result.correctAnswer}</p>
          )}
          <div className="mt-3 flex flex-wrap gap-2">
            <button
              type="button"
              onClick={resetForm}
              className="rounded-md border border-current px-3 py-1.5 text-xs font-medium transition hover:bg-white/50"
            >
              Try again
            </button>
            <Link
              href="/challenges"
              className="rounded-md bg-current px-3 py-1.5 text-xs font-medium text-white transition hover:opacity-90"
            >
              Back to challenges
            </Link>
          </div>
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-3">
        <div>
          <label htmlFor="userAnswer" className="block text-sm font-medium text-gray-700 mb-1">
            Your answer
          </label>
          <textarea
            id="userAnswer"
            value={userAnswer}
            onChange={(e) => setUserAnswer(e.target.value)}
            rows={5}
            disabled={loading}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="Type your solution here..."
          />
        </div>

        <button
          type="submit"
          disabled={loading}
          className="bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 text-white font-medium py-2 px-4 rounded-md transition"
        >
          {loading ? 'Submitting attempt...' : 'Submit Attempt'}
        </button>
      </form>
    </div>
  );
}
