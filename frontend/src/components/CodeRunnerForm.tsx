'use client';

import Link from 'next/link';
import { useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import Editor from '@monaco-editor/react';
import { useAuth } from './AuthContext';
import api from '@/lib/api';
import type { AttemptResult } from './AttemptForm';

export interface CodeTestCase {
  input: string;
  expectedOutput: string;
  description: string;
}

export interface TestResult {
  description: string;
  passed: boolean;
  actualOutput: string;
  error?: string;
}

export function runTests(code: string, testCases: CodeTestCase[]): TestResult[] {
  return testCases.map((tc) => {
    try {
      // eslint-disable-next-line no-new-func
      const fn = new Function(`${code}\nreturn solution(${tc.input});`);
      const returnValue = fn();
      const actualOutput = String(returnValue ?? '').trim();
      const passed = actualOutput.toLowerCase() === tc.expectedOutput.trim().toLowerCase();
      return { description: tc.description, passed, actualOutput };
    } catch (e: any) {
      return {
        description: tc.description,
        passed: false,
        actualOutput: '',
        error: e?.message ?? 'Unknown error',
      };
    }
  });
}

interface CodeRunnerFormProps {
  challengeId: string;
  timeLimitSecs: number;
  starterCode: string;
  testCases: CodeTestCase[];
  onSuccess?: (result: AttemptResult) => void;
  runCode?: (code: string, testCases: CodeTestCase[]) => TestResult[];
}

function getTimeBadge(elapsed: number, limit: number): { label: string; className: string } {
  const ratio = limit > 0 ? elapsed / limit : 1;
  if (ratio <= 0.5) return { label: 'Fast answer', className: 'text-green-600' };
  if (ratio <= 0.8) return { label: 'In time', className: 'text-blue-600' };
  return { label: 'Cutting it close', className: 'text-amber-600' };
}

export default function CodeRunnerForm({
  challengeId,
  timeLimitSecs,
  starterCode,
  testCases,
  onSuccess,
  runCode = runTests,
}: CodeRunnerFormProps) {
  const [code, setCode] = useState(starterCode);
  const [testResults, setTestResults] = useState<TestResult[]>([]);
  const [allPassed, setAllPassed] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<AttemptResult | null>(null);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const startedAtMs = useRef<number>(Date.now());
  const router = useRouter();
  const { clearAuth } = useAuth();

  useEffect(() => {
    if (result || loading) return;
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

  const progressTone = isCritical ? 'bg-red-500' : isWarning ? 'bg-amber-500' : 'bg-blue-500';

  const handleRunTests = () => {
    const results = runCode(code, testCases);
    setTestResults(results);
    setAllPassed(results.length > 0 && results.every((r) => r.passed));
  };

  const handleSubmit = async () => {
    if (!allPassed) return;

    setError('');
    setLoading(true);

    const currentElapsedSeconds = Math.max(0, Math.floor((Date.now() - startedAtMs.current) / 1000));
    setElapsedSeconds(currentElapsedSeconds);

    try {
      const response = await api.post(`/challenges/${challengeId}/attempt`, {
        userAnswer: 'PASS',
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

      if (status === 404) {
        setError('Challenge not found.');
      } else {
        setError('Server error. Try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    startedAtMs.current = Date.now();
    setElapsedSeconds(0);
    setCode(starterCode);
    setTestResults([]);
    setAllPassed(false);
    setError('');
    setResult(null);
  };

  return (
    <div className="bg-white rounded-lg shadow p-6 space-y-4">
      <h2 className="text-xl font-semibold text-gray-900">Write Your Solution</h2>

      <div className={`rounded-lg border p-3 text-sm ${timerTone}`} aria-live="polite">
        <div className="flex items-center justify-between gap-3">
          <span className="font-medium">Time remaining</span>
          <span>{remainingSeconds}s / {timeLimitSecs}s</span>
        </div>
        <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-white/70">
          <div
            className={`h-full rounded-full transition-all ${progressTone}`}
            style={{ width: `${Math.max(0, remainingRatio * 100)}%` }}
          />
        </div>
      </div>

      {error && (
        <div role="alert" className="p-3 rounded border border-red-200 bg-red-50 text-red-700 text-sm">
          {error}
        </div>
      )}

      {result && (() => {
        const badge = getTimeBadge(result.elapsedSeconds, timeLimitSecs);
        return (
          <div
            className={`p-4 rounded border text-sm ${
              result.isCorrect
                ? 'border-green-200 bg-green-50 text-green-700'
                : 'border-amber-200 bg-amber-50 text-amber-700'
            }`}
          >
            <p className="font-semibold text-base">{result.isCorrect ? 'Correct!' : 'Not quite'}</p>
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

      <div className="rounded-lg overflow-hidden border border-gray-700">
        <Editor
          language="javascript"
          theme="vs-dark"
          height="300px"
          value={code}
          onChange={(v) => setCode(v ?? '')}
        />
      </div>

      <div className="space-y-2">
        <h3 className="text-sm font-medium text-gray-700">Test Cases</h3>
        {testCases.map((tc, i) => {
          const res = testResults[i];
          return (
            <div key={i} className="p-3 rounded border border-gray-200 text-sm">
              <p className="font-medium text-gray-800">{tc.description}</p>
              {!res && <p className="text-gray-400 text-xs mt-1">⏳ Not run yet</p>}
              {res && res.error && (
                <p className="text-red-600 text-xs mt-1">💥 {res.error}</p>
              )}
              {res && !res.error && res.passed && (
                <p className="text-green-600 text-xs mt-1">✅ Passed</p>
              )}
              {res && !res.error && !res.passed && (
                <p className="text-red-600 text-xs mt-1">
                  ❌ Got: {res.actualOutput} — Expected: {tc.expectedOutput}
                </p>
              )}
            </div>
          );
        })}
      </div>

      <div className="flex gap-3">
        <button
          type="button"
          onClick={handleRunTests}
          className="bg-gray-700 hover:bg-gray-800 text-white font-medium py-2 px-4 rounded-md transition"
        >
          Run Tests
        </button>
        <button
          type="button"
          onClick={handleSubmit}
          disabled={!allPassed || loading || result !== null}
          className="bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 text-white font-medium py-2 px-4 rounded-md transition"
        >
          {loading ? 'Submitting...' : allPassed ? 'Submit Attempt' : 'Run tests first'}
        </button>
      </div>
    </div>
  );
}
