'use client';

import Link from 'next/link';
import { useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import {
  DndContext,
  closestCenter,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
} from '@dnd-kit/core';
import {
  arrayMove,
  SortableContext,
  sortableKeyboardCoordinates,
  useSortable,
  verticalListSortingStrategy,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/sortable';
import { useAuth } from './AuthContext';
import api from '@/lib/api';
import type { AttemptResult } from './AttemptForm';

interface OrderingFormProps {
  challengeId: string;
  timeLimitSecs: number;
  items: string[];
  onSuccess?: (result: AttemptResult) => void;
}

function shuffle<T>(arr: T[]): T[] {
  const copy = [...arr];
  for (let i = copy.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [copy[i], copy[j]] = [copy[j], copy[i]];
  }
  return copy;
}

function getTimeBadge(elapsed: number, limit: number): { label: string; className: string } {
  const ratio = limit > 0 ? elapsed / limit : 1;
  if (ratio <= 0.5) return { label: 'Fast answer', className: 'text-green-600' };
  if (ratio <= 0.8) return { label: 'In time', className: 'text-blue-600' };
  return { label: 'Cutting it close', className: 'text-amber-600' };
}

function SortableItem({ id, disabled }: { id: string; disabled: boolean }) {
  const { attributes, listeners, setNodeRef, transform, transition, isDragging } = useSortable({ id, disabled });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
    opacity: isDragging ? 0.5 : 1,
  };

  return (
    <div
      ref={setNodeRef}
      style={style}
      aria-disabled={disabled}
      className={`flex items-center gap-3 p-3 rounded border border-gray-200 bg-white select-none ${
        disabled ? 'opacity-60 cursor-not-allowed' : 'cursor-grab active:cursor-grabbing hover:bg-gray-50'
      }`}
      {...attributes}
      {...listeners}
    >
      <span className="text-gray-400 text-lg" aria-hidden="true">⠿</span>
      <span className="text-sm text-gray-800">{id}</span>
    </div>
  );
}

export default function OrderingForm({ challengeId, timeLimitSecs, items, onSuccess }: OrderingFormProps) {
  const [orderedItems, setOrderedItems] = useState<string[]>(() => shuffle(items));
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<AttemptResult | null>(null);
  const [elapsedSeconds, setElapsedSeconds] = useState(0);
  const startedAtMs = useRef<number>(Date.now());
  const router = useRouter();
  const { clearAuth } = useAuth();

  const sensors = useSensors(
    useSensor(PointerSensor),
    useSensor(KeyboardSensor, { coordinateGetter: sortableKeyboardCoordinates })
  );

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

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (over && active.id !== over.id) {
      setOrderedItems((current) => {
        const oldIndex = current.indexOf(String(active.id));
        const newIndex = current.indexOf(String(over.id));
        return arrayMove(current, oldIndex, newIndex);
      });
    }
  };

  const handleSubmit = async () => {
    setError('');
    setLoading(true);

    const currentElapsedSeconds = Math.max(0, Math.floor((Date.now() - startedAtMs.current) / 1000));
    setElapsedSeconds(currentElapsedSeconds);

    try {
      const response = await api.post(`/challenges/${challengeId}/attempt`, {
        userAnswer: orderedItems.join('|'),
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
    setOrderedItems(shuffle(items));
    setError('');
    setResult(null);
  };

  return (
    <div className="bg-white rounded-lg shadow p-6 space-y-4">
      <h2 className="text-xl font-semibold text-gray-900">Order Your Answer</h2>

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
            {!result.isCorrect && result.correctAnswer && (
              <p className="mt-1">Correct order: {result.correctAnswer}</p>
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

      <p className="text-sm text-gray-600">Drag items into the correct order</p>
      <p className="text-xs text-gray-500">Use arrow keys to reorder when focused</p>

      <DndContext sensors={sensors} collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
        <SortableContext items={orderedItems} strategy={verticalListSortingStrategy}>
          <div className="space-y-2">
            {orderedItems.map((item) => (
              <SortableItem key={item} id={item} disabled={loading || result !== null} />
            ))}
          </div>
        </SortableContext>
      </DndContext>

      <button
        type="button"
        onClick={handleSubmit}
        disabled={loading || result !== null}
        className="bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 text-white font-medium py-2 px-4 rounded-md transition"
      >
        {loading ? 'Submitting...' : 'Submit Order'}
      </button>
    </div>
  );
}
