using System;
using System.Collections.Generic;
using System.Threading;

namespace Jung.Utils
{
	public class RoutineArea
	{
		public long NextExecuteTicks;
		public readonly IEnumerator<TimeSpan> UpdateRoutine;
		public readonly CancellationToken CancellationToken;

		private bool _isDisposed;
		public bool IsDisposed
		{
			get => CancellationToken.IsCancellationRequested || _isDisposed;
			set => _isDisposed = value;
		}

		// Have to use volatile keyword or Interlocked.Exchange when release, because one routine can be unexpectedly executed.
		// macOS Arm64 6.0.301 occured.
		private int _executeFlag;

		public RoutineArea(IEnumerator<TimeSpan> updateRoutine, CancellationToken cancellationToken)
        {
            UpdateRoutine = updateRoutine;
			CancellationToken = cancellationToken;
        }

		public bool AcquireLock()
        {
            return Interlocked.CompareExchange(ref _executeFlag, 1, 0) == 0;
        }

        public void ReleaseLock()
        {
            Interlocked.Exchange(ref _executeFlag, 0);
        }
	}
}