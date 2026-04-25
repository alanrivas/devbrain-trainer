'use client';

import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { useAuth } from '@/components/AuthContext';
import AttemptForm, { AttemptResult } from '@/components/AttemptForm';
import MultipleChoiceForm from '@/components/MultipleChoiceForm';
import CodeRunnerForm from '@/components/CodeRunnerForm';
import type { CodeTestCase } from '@/components/CodeRunnerForm';
import api from '@/lib/api';

interface ChallengeDetail {
  id: string;
  title: string;
  description: string;
  category: string;
  difficulty: 'Easy' | 'Medium' | 'Hard';
  timeLimitSecs: number;
  type: 'OpenText' | 'MultipleChoice' | 'CodeRunner';
  options: string[];
  starterCode: string;
  testCases: CodeTestCase[];
}

export default function ChallengeDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const { token, clearAuth } = useAuth();
  const [challenge, setChallenge] = useState<ChallengeDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [notFound, setNotFound] = useState(false);
  const [lastAttempt, setLastAttempt] = useState<AttemptResult | null>(null);
  const [prevId, setPrevId] = useState<string | null>(null);
  const [nextId, setNextId] = useState<string | null>(null);

  useEffect(() => {
    if (!token) {
      router.push('/login');
      return;
    }

    const challengeId = params?.id;
    if (!challengeId) {
      setError('Invalid challenge id.');
      setLoading(false);
      return;
    }

    const fetchChallenge = async () => {
      try {
        const response = await api.get<ChallengeDetail>(`/challenges/${challengeId}`);
        setChallenge(response.data);
      } catch (err: any) {
        const status = err?.response?.status;
        if (status === 401) {
          clearAuth();
          router.push('/login');
          return;
        }
        if (status === 404) {
          setNotFound(true);
        } else {
          setError('Failed to load challenge.');
        }
      } finally {
        setLoading(false);
      }
    };

    fetchChallenge();
  }, [token, params, router, clearAuth]);

  // Keyboard navigation: ArrowLeft/ArrowRight for prev/next, Escape for back
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      const tag = (document.activeElement as HTMLElement)?.tagName;
      if (tag === 'TEXTAREA' || tag === 'INPUT') return;

      if (e.key === 'ArrowLeft' && prevId) router.push(`/challenges/${prevId}`);
      if (e.key === 'ArrowRight' && nextId) router.push(`/challenges/${nextId}`);
      if (e.key === 'Escape') router.push('/challenges');
    };
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [prevId, nextId, router]);

  // Compute prev/next from sessionStorage challenge list
  useEffect(() => {
    if (!challenge) return;

    try {
      const stored = sessionStorage.getItem('challenge-list-ids');
      if (!stored) return;

      const ids: string[] = JSON.parse(stored);
      const idx = ids.indexOf(challenge.id);
      if (idx === -1) return;

      setPrevId(idx > 0 ? ids[idx - 1] : null);
      setNextId(idx < ids.length - 1 ? ids[idx + 1] : null);
    } catch {
      // ignore parse errors
    }
  }, [challenge]);

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <p className="text-gray-600">Loading challenge...</p>
      </div>
    );
  }

  if (notFound) {
    return (
      <main className="min-h-screen bg-gray-50 py-12 px-4">
        <div className="max-w-3xl mx-auto bg-white rounded-lg shadow p-8">
          <h1 className="text-2xl font-bold text-gray-900">Challenge not found</h1>
          <p className="text-gray-600 mt-2">The requested challenge does not exist.</p>
          <Link href="/challenges" className="inline-block mt-6 text-blue-600 hover:text-blue-700 font-medium">
            Back to challenges
          </Link>
        </div>
      </main>
    );
  }

  if (error || !challenge) {
    return (
      <main className="min-h-screen bg-gray-50 py-12 px-4">
        <div className="max-w-3xl mx-auto bg-white rounded-lg shadow p-8">
          <div role="alert" className="p-3 rounded border border-red-200 bg-red-50 text-red-700 text-sm">
            {error || 'Failed to load challenge.'}
          </div>
          <Link href="/challenges" className="inline-block mt-6 text-blue-600 hover:text-blue-700 font-medium">
            Back to challenges
          </Link>
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-gray-50 py-8 px-4">
      <div className="max-w-4xl mx-auto space-y-6">
        <div className="flex items-center justify-between">
          <Link href="/challenges" className="text-sm text-blue-600 hover:text-blue-700 font-medium">
            ← Back to challenges
          </Link>

          {(prevId || nextId) && (
            <nav className="flex items-center gap-3 text-sm">
              {prevId && (
                <Link
                  href={`/challenges/${prevId}`}
                  className="text-blue-600 hover:text-blue-700 font-medium"
                >
                  ← Previous challenge
                </Link>
              )}
              {nextId && (
                <Link
                  href={`/challenges/${nextId}`}
                  className="text-blue-600 hover:text-blue-700 font-medium"
                >
                  Next challenge →
                </Link>
              )}
            </nav>
          )}
        </div>

        <section className="bg-white rounded-lg shadow p-6">
          <div className="flex flex-wrap items-center gap-2 mb-4">
            <span className="text-xs bg-gray-100 text-gray-700 px-2 py-1 rounded">{challenge.category}</span>
            <span className="text-xs bg-blue-100 text-blue-700 px-2 py-1 rounded">{challenge.difficulty}</span>
            <span className="text-xs bg-indigo-100 text-indigo-700 px-2 py-1 rounded">
              Time limit: {challenge.timeLimitSecs}s
            </span>
          </div>
          <h1 className="text-3xl font-bold text-gray-900">{challenge.title}</h1>
          <p className="mt-4 text-gray-700 whitespace-pre-wrap">{challenge.description}</p>
        </section>

        {challenge.type === 'MultipleChoice' ? (
          <MultipleChoiceForm
            challengeId={challenge.id}
            timeLimitSecs={challenge.timeLimitSecs}
            options={challenge.options}
            onSuccess={(attempt) => setLastAttempt(attempt)}
          />
        ) : challenge.type === 'CodeRunner' ? (
          <CodeRunnerForm
            challengeId={challenge.id}
            timeLimitSecs={challenge.timeLimitSecs}
            starterCode={challenge.starterCode}
            testCases={challenge.testCases}
            onSuccess={(attempt) => setLastAttempt(attempt)}
          />
        ) : (
          <AttemptForm
            challengeId={challenge.id}
            timeLimitSecs={challenge.timeLimitSecs}
            onSuccess={(attempt) => setLastAttempt(attempt)}
          />
        )}

        {lastAttempt && (
          <section className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold text-gray-900">Last Attempt</h2>
            <p className="mt-2 text-sm text-gray-700">
              Result: {lastAttempt.isCorrect ? 'Correct' : 'Incorrect'} • Elapsed: {lastAttempt.elapsedSeconds}s
            </p>
            <p className="mt-1 text-sm text-gray-600">
              Use the actions in the attempt card to retry this challenge or return to the list.
            </p>
          </section>
        )}
      </div>
    </main>
  );
}
