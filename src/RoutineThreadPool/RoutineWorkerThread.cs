using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.Runtime.ExceptionServices;

namespace Jung.Utils
{
    internal class RoutineWorkerThread
    {
        private readonly CancellationToken _cancellationToken;
        private readonly ManualResetEventSlim _manualReset;
        private readonly Thread _thread;

        private readonly ILogger _logger;

        private readonly ConcurrentQueue<RoutineArea> _reservedRoutines = new ConcurrentQueue<RoutineArea>();
        private readonly HashSet<RoutineArea> _routineAreas = new HashSet<RoutineArea>();

        private readonly RoutineThreadPool _threadPool;

        internal RoutineWorkerThread(RoutineThreadPool threadPool, ILogger logger, CancellationToken cancellationToken)
        {
            _threadPool = threadPool;

            _cancellationToken = cancellationToken;
            _logger = logger;

            _manualReset = new ManualResetEventSlim();

            _thread = new Thread(Update)
            {
                IsBackground = true
            };

            _thread.Start();
        }

        internal void Add(RoutineArea routineArea)
        {
            _reservedRoutines.Enqueue(routineArea);
            _manualReset.Set();
        }

        private void MoveReservedRoutines()
        {
            while (_reservedRoutines.TryDequeue(out var routine))
            {
                _routineAreas.Add(routine);
            }
        }

        private void Update()
        {
            while (_cancellationToken.IsCancellationRequested == false)
            {
                _manualReset.Reset();

                long ticks = DateTime.UtcNow.Ticks;
                int waitMilliseconds = _threadPool.DefaultWaitMilliseconds;

                try
                {
                    MoveReservedRoutines();
                    long nextExecuteTicks = ExcuteAndGetNextTicks(ticks);

                    waitMilliseconds = GetWaitMilliseconds(ticks, nextExecuteTicks);
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, message: "RoutineWorkerThread system throw exception.");
                }

                _manualReset.Wait(waitMilliseconds, _cancellationToken);
            }
        }

        private int GetWaitMilliseconds(long ticks, long nextExecuteTicks)
        {
            if (nextExecuteTicks <= 0)
            {
                return 1;
            }

            int waitMiliseconds = (int)TimeSpan.FromTicks(nextExecuteTicks - ticks).TotalMilliseconds;
            if (waitMiliseconds <= 0)
            {
                return 1;
            }

            var defaultWaitMiliseconds = _threadPool.DefaultWaitMilliseconds;
            if (waitMiliseconds > defaultWaitMiliseconds)
            {
                return defaultWaitMiliseconds;
            }

            return waitMiliseconds;
        }

        private long ExcuteAndGetNextTicks(long ticks)
        {
            List<RoutineArea>? disposedRoutineAreas = null;

            long minNextExecuteTicks = -1;

            foreach(var routineArea in _routineAreas)
            {
                if (routineArea.IsDisposed)
                {
                    if (disposedRoutineAreas == null)
                    {
                        disposedRoutineAreas = new List<RoutineArea>();
                    }

                    disposedRoutineAreas.Add(routineArea);

                    continue;
                }

                if (ExecuteAndTryGetNextTicksWhenAcquireLock(ticks, routineArea, out long nextExecuteTicks) == false)
                {
                    continue;
                }

                if (minNextExecuteTicks < 0)
                {
                    minNextExecuteTicks = nextExecuteTicks;
                }
                else
                {
                    minNextExecuteTicks = Math.Min(minNextExecuteTicks, nextExecuteTicks);
                }
            }

            RemoveRoutineAreas(disposedRoutineAreas);
            return minNextExecuteTicks;
        }

        private bool ExecuteAndTryGetNextTicksWhenAcquireLock(long ticks, RoutineArea routineArea, out long nextExecuteTicks)
        {
            nextExecuteTicks = default;

            if (routineArea.AcquireLock() == false)
            {
                return false;
            }

            try
            {
                return ExecuteAndTryGetNextTicks(ticks, routineArea, out nextExecuteTicks);
            }
            finally
            {
                routineArea.ReleaseLock();
            }
        }

        private bool ExecuteAndTryGetNextTicks(long ticks, RoutineArea routineArea, out long nextExecuteTicks)
        {
            nextExecuteTicks = default;

            if (routineArea.IsDisposed)
            {
                return false;
            }

            if (ticks < routineArea.NextExecuteTicks)
            {
                nextExecuteTicks = routineArea.NextExecuteTicks;
                return true;
            }

            bool moveNext = Execute(routineArea.UpdateRoutine, out var waitTime);
            if (moveNext == false)
            {
                routineArea.IsDisposed = true;
                return false;
            }

            routineArea.NextExecuteTicks = ticks + waitTime.Ticks;
            nextExecuteTicks = routineArea.NextExecuteTicks;

            return true;
        }

        private bool Execute(IEnumerator<TimeSpan> updateRoutine, out TimeSpan waitTime)
        {
            waitTime = TimeSpan.Zero;

            try
            {
                bool moveNext = updateRoutine.MoveNext();
                if (moveNext)
                {
                    waitTime = updateRoutine.Current;
                }

                return moveNext;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "User routine throw exception.");
                return false;
            }
        }

        private void RemoveRoutineAreas(List<RoutineArea>? routines)
        {
            if (routines == null)
            {
                return;
            }

            foreach(var routine in routines)
            {
                _routineAreas.Remove(routine);
            }
        }
    }
}
