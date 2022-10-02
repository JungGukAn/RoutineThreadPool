using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Jung.Utils
{
    public class RoutineThreadPool
    {
        public static RoutineThreadPool Shared => _shared.Value;

        private static readonly Lazy<RoutineThreadPool> _shared = new Lazy<RoutineThreadPool>(() =>
        {
            return new RoutineThreadPool();
        }, true);

        private static ILoggerFactory _consoleLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddSimpleConsole();
        });

        public int DefaultWaitMilliseconds
        {
            get => _defaultWaitMilliseconds;

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("DefaultWaitMilliseconds must be larger than zero.");
                }

                _defaultWaitMilliseconds = value;
            }
        }

        public int ThreadCount => _workerThreads.Count;

        private int _defaultWaitMilliseconds = 33;

        private readonly ILoggerFactory _loggerFactory;
        private readonly CancellationTokenSource _cts;

        private List<RoutineWorkerThread> _workerThreads = new List<RoutineWorkerThread>();

        public RoutineThreadPool() : this(_consoleLoggerFactory, Environment.ProcessorCount)
        {
            
        }

        public RoutineThreadPool(int threadPoolCount) : this(_consoleLoggerFactory, threadPoolCount)
        {

        }

        public RoutineThreadPool(ILoggerFactory loggerFactory) : this(loggerFactory, Environment.ProcessorCount)
        {

        }

        public RoutineThreadPool(ILoggerFactory loggerFactory, int threadPoolCount)
        {
            if (threadPoolCount <= 0)
            {
                throw new ArgumentException("threadPoolCount must be larger than zero.");
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException("loggerFactory is null.");
            }

            _loggerFactory = loggerFactory;
            _cts = new CancellationTokenSource();

            CreateWorkerThreads(threadPoolCount);
        }

        private void CreateWorkerThreads(int threadPoolCount)
        {
            for (int i = 0; i < threadPoolCount; i++)
            {
                var workerThread = new RoutineWorkerThread(this, _loggerFactory.CreateLogger($"{nameof(RoutineWorkerThread)}{i}"), _cts.Token);
                _workerThreads.Add(workerThread);
            }
        }

        public bool Start(IEnumerable<TimeSpan> updateRoutine)
        {
            return Start(0, _workerThreads.Count - 1, updateRoutine, CancellationToken.None);
        }

        public bool Start(int threadIndex, IEnumerable<TimeSpan> updateRoutine)
        {
            return Start(threadIndex, threadIndex, updateRoutine, CancellationToken.None);
        }

        public bool Start(int threadMinIndex, int threadMaxIndex, IEnumerable<TimeSpan> updateRoutine)
        {
            return Start(threadMinIndex, threadMaxIndex, updateRoutine, CancellationToken.None);
        }

        public bool Start(IEnumerable<TimeSpan> updateRoutine, CancellationToken cancellationToken)
        {
            return Start(0, _workerThreads.Count - 1, updateRoutine, cancellationToken);
        }

        public bool Start(int threadIndex, IEnumerable<TimeSpan> updateRoutine, CancellationToken cancellationToken)
        {
            return Start(threadIndex, threadIndex, updateRoutine, cancellationToken);
        }

        public bool Start(int threadMinIndex, int threadMaxIndex, IEnumerable<TimeSpan> updateRoutine, CancellationToken cancellationToken)
        {
            return Start(threadMinIndex, threadMaxIndex, updateRoutine.GetEnumerator(), cancellationToken);
        }

        public bool Start(IEnumerator<TimeSpan> updateRoutine)
        {
            return Start(0, _workerThreads.Count - 1, updateRoutine, CancellationToken.None);
        }

        public bool Start(int threadIndex, IEnumerator<TimeSpan> updateRoutine)
        {
            return Start(threadIndex, threadIndex, updateRoutine, CancellationToken.None);
        }

        public bool Start(int threadMinIndex, int threadMaxIndex, IEnumerator<TimeSpan> updateRoutine)
        {
            return Start(threadMinIndex, threadMaxIndex, updateRoutine, CancellationToken.None);
        }

        public bool Start(IEnumerator<TimeSpan> updateRoutine, CancellationToken cancellationToken)
        {
            return Start(0, _workerThreads.Count - 1, updateRoutine, cancellationToken);
        }

        public bool Start(int threadIndex, IEnumerator<TimeSpan> updateRoutine, CancellationToken cancellationToken)
        {
            return Start(threadIndex, threadIndex, updateRoutine, cancellationToken);
        }

        public bool Start(int threadMinIndex, int threadMaxIndex, IEnumerator<TimeSpan> updateRoutine, CancellationToken cancellationToken)
        {
            if (IsValidThreadIndex(threadMinIndex, threadMinIndex) == false)
            {
                return false;
            }

            var range = Enumerable.Range(threadMinIndex, threadMaxIndex - threadMinIndex + 1);
            var routine = new RoutineArea(updateRoutine, cancellationToken);

            foreach (int threadIndex in range)
            {
                _workerThreads[threadIndex].Add(routine);
            }

            return true;
        }

        private bool IsValidThreadIndex(int threadMinIndex, int threadMaxIndex)
        {
            if (threadMinIndex < 0)
            {
                return false;
            }

            if (threadMaxIndex >= _workerThreads.Count)
            {
                return false;
            }

            if (threadMinIndex > threadMaxIndex)
            {
                return false;
            }

            return true;
        }
    }
}
