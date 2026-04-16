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
  newEloRating?: number;
  newStreak?: number;
  newBadges?: string[];
}

interface AttemptFormProps {
  challengeId: string;
  timeLimitSecs: number;
  onSuccess?: (result: AttemptResult) => void;
}

function getTimeBadge(elapsed: number, limit: number): { label: string; className: string } {
  const ratio = limit > 0 ? elapsed / limit : 1;
  if (ratio <= 0.5) return { label: 'Fast answer', className: 'text-green-600' };
  if (ratio <= 0.8) return { label: 'In time', className: 'text-blue-600' };
  return { label: 'Cutting it close', className: 'text-amber-600' };
}

export default function AttemptForm({ challengeId, timeLimitSecs, onSuccess }: AttemptFormProps) {
  const DRAFT_KEY = `draft-attempt-${challengeId}`;

  const [userAnswer, setUserAnswer] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<AttemptResult | null>(null);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const startedAtMs = useRef<number>(Date.now());
  const formRef = useRef<HTMLFormElement>(null);
  const router = useRouter();
  const { clearAuth } = useAuth();

  // Restore draft from localStorage on mount
  useEffect(() => {
    const saved = localStorage.getItem(DRAFT_KEY);
    if (saved && saved.trim()) {
      setUserAnswer(saved);
    }
  }, [DRAFT_KEY]);

  useEffect(() => {
    if (result || loading) {
      return;
    }

    const interval = window.setInterval(() => {
      setElapsedSeconds(Math.max(0, Math.floor((Date.now() - startedAtMs.current) / 1000)));
    }, 1000);

    return () => window.clearInterval(interval);
  }, [loading, result]);

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
        if (!loading) formRef.current?.requestSubmit();
        return;
      }
      if ((e.key === 'r' || e.key === 'R') && result !== null) {
        resetForm();
      }
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
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
    localStorage.removeItem(DRAFT_KEY);
    startedAtMs.current = Date.now();
    setElapsedSeconds(0);
    setUserAnswer('');
    setError('');
    setResult(null);
  };

  const handleAnswerChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const val = e.target.value;
    setUserAnswer(val);
    if (val) {
      localStorage.setItem(DRAFT_KEY, val);
    } else {
      localStorage.removeItem(DRAFT_KEY);
    }
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
      localStorage.removeItem(DRAFT_KEY);
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

      {result && (() => {
        const badge = getTimeBadge(result.elapsedSeconds, timeLimitSecs);
        return (
          <div
            className={`mb-4 p-4 rounded border text-sm ${
              result.isCorrect
                ? 'border-green-200 bg-green-50 text-green-700'
                : 'border-amber-200 bg-amber-50 text-amber-700'
            }`}
          >
            <p className="font-semibold text-base">{result.isCorrect ? 'Correct!' : 'Not quite'}</p>
            {!result.isCorrect && result.correctAnswer && (
              <p className="mt-1">Correct answer: {result.correctAnswer}</p>
            )}
            <p className="mt-1 text-xs opacity-80">
              Submitted in {result.elapsedSeconds}s —{' '}
              <span className={badge.className}>{badge.label}</span>
            </p>
            {result.newEloRating !== undefined && (
              <p className="mt-1 text-xs opacity-80">ELO: {result.newEloRating}</p>
            )}
            {result.newStreak !== undefined && (
              <p className="mt-1 text-xs opacity-80">Streak: {result.newStreak} days</p>
            )}
            {result.newBadges && result.newBadges.length > 0 && (
              <div className="mt-1">
                {result.newBadges.map((b) => (
                  <p key={b} className="text-xs font-medium">New badge: {b}</p>
                ))}
              </div>
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
        );
      })()}

      <form ref={formRef} onSubmit={handleSubmit} className="space-y-3">
        <div>
          <label htmlFor="userAnswer" className="block text-sm font-medium text-gray-700 mb-1">
            Your answer
          </label>
          <textarea
            id="userAnswer"
            value={userAnswer}
            onChange={handleAnswerChange}
            rows={5}
            disabled={loading}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
            placeholder="Type your solution here..."
          />
          <p className="text-xs text-gray-400 mt-1">Ctrl+Enter to submit</p>
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
