'use client';

import Link from 'next/link';
import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { useAuth } from '@/components/AuthContext';
import api from '@/lib/api';

interface AttemptHistoryItem {
  attemptId: string;
  challengeId: string;
  challengeTitle: string;
  isCorrect: boolean;
  elapsedSecs: number;
  occurredAt: string;
}

export default function HistoryPage() {
  const router = useRouter();
  const { token, clearAuth } = useAuth();
  const [attempts, setAttempts] = useState<AttemptHistoryItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!token) {
      router.push('/login');
      return;
    }

    const fetchHistory = async () => {
      try {
        const response = await api.get<AttemptHistoryItem[]>('/users/me/attempts');
        setAttempts(response.data);
      } catch (err: any) {
        const status = err?.response?.status;
        if (status === 401) {
          clearAuth();
          router.push('/login');
          return;
        }

        setError('Failed to load history.');
      } finally {
        setLoading(false);
      }
    };

    fetchHistory();
  }, [token, router, clearAuth]);

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <p className="text-gray-600">Loading history...</p>
      </div>
    );
  }

  if (error) {
    return (
      <main className="min-h-screen bg-gray-50 py-12 px-4">
        <div className="max-w-4xl mx-auto bg-white rounded-lg shadow p-8">
          <div role="alert" className="rounded border border-red-200 bg-red-50 p-3 text-sm text-red-700">
            {error}
          </div>
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-gray-50 py-8 px-4">
      <div className="max-w-4xl mx-auto space-y-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Attempt History</h1>
          <p className="mt-2 text-gray-600">Review your previous attempts and results.</p>
        </div>

        {attempts.length === 0 ? (
          <div className="rounded-lg border border-dashed border-gray-300 bg-white p-8 text-center shadow-sm">
            <p className="text-gray-500">No attempts yet</p>
          </div>
        ) : (
          <ul className="space-y-4">
            {attempts.map((attempt) => (
              <li key={attempt.attemptId} className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                  <div className="space-y-2">
                    <Link
                      href={`/challenges/${attempt.challengeId}`}
                      className="text-lg font-semibold text-blue-600 hover:text-blue-700"
                    >
                      {attempt.challengeTitle}
                    </Link>
                    <div className="flex flex-wrap gap-3 text-sm text-gray-600">
                      <span>{attempt.isCorrect ? 'Correct' : 'Incorrect'}</span>
                      <span>{attempt.elapsedSecs}s</span>
                    </div>
                  </div>
                  <time className="text-sm text-gray-500" dateTime={attempt.occurredAt}>
                    {new Date(attempt.occurredAt).toLocaleString()}
                  </time>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </main>
  );
}