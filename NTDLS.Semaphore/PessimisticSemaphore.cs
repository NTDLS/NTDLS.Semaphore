namespace NTDLS.Semaphore
{
    /// <summary>
    /// Protects an area of code from parallel / non-sequential thread access.
    /// </summary>
    public class PessimisticSemaphore : ICriticalSection
    {
        /// <summary>
        /// Identifies the current thread that owns the lock. This is only tracked if enabled by a call
        /// to ThreadOwnershipTracking.EnableThreadOwnershipTracking(). Once enabled, the tracking is
        /// attributed to all critical sections for the life of the application - so its definitely best
        /// only enabled in debugging.
        /// </summary>
        public Thread? CurrentOwnerThread { get; private set; }
        private int _reentrantLevel = 0;

        #region Delegates.

        /// <summary>
        /// Delegate for executions that do not require a return value.
        /// </summary>
        public delegate void CriticalSectionDelegateWithVoidResult();

        /// <summary>
        /// Delegate for executions that require a nullable return value.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        public delegate T? CriticalSectionDelegateWithNullableResultT<T>();

        /// <summary>
        /// Delegate for executions that require a non-nullable return value.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        public delegate T CriticalSectionDelegateWithNotNullableResultT<T>();

        #endregion

        #region Use/TryUse overloads.

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes and returns the given delegate result. Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public T TryUse<T>(T defaultValue, CriticalSectionDelegateWithNotNullableResultT<T> function)
        {
            if (TryAcquire())
            {
                try
                {
                    return function();
                }
                finally
                {
                    Release();
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes and returns the given delegate result. Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="timeout"></param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public T TryUse<T>(int timeout, T defaultValue, CriticalSectionDelegateWithNotNullableResultT<T> function)
        {
            if (TryAcquire(timeout))
            {
                try
                {
                    return function();
                }
                finally
                {
                    Release();
                }
            }
            return defaultValue;

        }

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes and returns the given delegate result. Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public T TryUse<T>(T defaultValue, out bool wasLockObtained, CriticalSectionDelegateWithNotNullableResultT<T> function)
        {
            wasLockObtained = TryAcquire();
            if (wasLockObtained)
            {
                try
                {
                    return function();
                }
                finally
                {
                    Release();
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes and returns the given delegate result. Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeout"></param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public T TryUse<T>(T defaultValue, out bool wasLockObtained, int timeout, CriticalSectionDelegateWithNotNullableResultT<T> function)
        {
            wasLockObtained = TryAcquire(timeout);
            if (wasLockObtained)
            {
                try
                {
                    return function();
                }
                finally
                {
                    Release();
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes and returns the given delegate result. Otherwise returns null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public T? TryUseNullable<T>(CriticalSectionDelegateWithNullableResultT<T> function)
        {
            if (TryAcquire())
            {
                try
                {
                    return function();
                }
                finally
                {
                    Release();
                }
            }
            return default;
        }

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes and returns the given delegate result. Otherwise returns null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="timeout"></param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public T? TryUseNullable<T>(int timeout, CriticalSectionDelegateWithNullableResultT<T> function)
        {
            if (TryAcquire(timeout))
            {
                try
                {
                    return function();
                }
                finally
                {
                    Release();
                }
            }
            return default;

        }

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes and returns the given delegate result. Otherwise returns null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public T? TryUseNullable<T>(out bool wasLockObtained, CriticalSectionDelegateWithNullableResultT<T> function)
        {
            wasLockObtained = TryAcquire();
            if (wasLockObtained)
            {
                try
                {
                    return function();
                }
                finally
                {
                    Release();
                }
            }
            return default;
        }

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes and returns the given delegate result. Otherwise returns null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeout"></param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public T? TryUseNullable<T>(out bool wasLockObtained, int timeout, CriticalSectionDelegateWithNullableResultT<T> function)
        {
            wasLockObtained = TryAcquire(timeout);
            if (wasLockObtained)
            {
                try
                {
                    return function();
                }
                finally
                {
                    Release();
                }
            }
            return default;
        }

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes the given delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryUse(CriticalSectionDelegateWithVoidResult function)
        {
            if (TryAcquire())
            {
                try
                {
                    function();
                }
                finally
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes the given delegate function.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryUse(int timeout, CriticalSectionDelegateWithVoidResult function)
        {
            if (TryAcquire(timeout))
            {
                try
                {
                    function();
                }
                finally
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes the given delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryUse(out bool wasLockObtained, CriticalSectionDelegateWithVoidResult function)
        {
            wasLockObtained = TryAcquire();
            if (wasLockObtained)
            {
                try
                {
                    function();
                }
                finally
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes the given delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeout"></param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryUse(out bool wasLockObtained, int timeout, CriticalSectionDelegateWithVoidResult function)
        {
            wasLockObtained = TryAcquire(timeout);
            if (wasLockObtained)
            {
                try
                {
                    function();
                }
                finally
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Blocks until the critical section is acquired then executes the given delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public void Use(CriticalSectionDelegateWithVoidResult function)
        {
            Acquire();
            try
            {
                function();
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Blocks until the critical section is acquired then executes the given delegate function. Returns the given delegate result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public T Use<T>(CriticalSectionDelegateWithNotNullableResultT<T> function)
        {
            Acquire();
            try
            {
                return function();
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Blocks until the critical section is acquired then executes the given delegate function. Returns the given delegate result.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public T? UseNullable<T>(CriticalSectionDelegateWithNullableResultT<T> function)
        {
            Acquire();
            try
            {
                return function();
            }
            finally
            {
                Release();
            }
        }

        #endregion

        #region Internal interface functionality.

        /// <summary>
        /// Internal use only. Blocks until the lock is acquired.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        void ICriticalSection.Acquire(OptimisticSemaphore.LockIntention intention)
            => Acquire();

        /// <summary>
        /// Internal use only. Attempts to acquire the lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        bool ICriticalSection.TryAcquire(OptimisticSemaphore.LockIntention intention)
            => TryAcquire();

        /// <summary>
        /// Internal use only. Attempts to acquire the lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        bool ICriticalSection.TryAcquire(OptimisticSemaphore.LockIntention intention, int timeoutMilliseconds)
            => TryAcquire(timeoutMilliseconds);

        /// <summary>
        /// Internal use only. Releases the previously acquired lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        void ICriticalSection.Release(OptimisticSemaphore.LockIntention intention)
            => Release();

        /// <summary>
        /// Internal use only. Attempts to acquire the lock for a given number of milliseconds.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        bool ICriticalSection.TryAcquire(int timeoutMilliseconds)
            => TryAcquire(timeoutMilliseconds);

        /// <summary>
        /// Internal use only. Attempts to acquire the lock.
        /// </summary>
        bool ICriticalSection.TryAcquire()
            => TryAcquire();

        /// <summary>
        /// Internal use only. Blocks until the lock is acquired.
        /// </summary>
        void ICriticalSection.Acquire()
            => Acquire();

        /// <summary>
        /// Internal use only. Releases the previously acquired lock.
        /// </summary>
        void ICriticalSection.Release()
            => Release();

        /// <summary>
        /// Internal use only. Attempts to acquire the lock for a given number of milliseconds.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        private bool TryAcquire(int timeoutMilliseconds)
        {
            if (Monitor.TryEnter(this, timeoutMilliseconds))
            {
                _reentrantLevel++;
                return true;
            }
            return false;
        }


        /// <summary>
        /// Internal use only. Attempts to acquire the lock.
        /// </summary>
        private bool TryAcquire()
        {
            if (Monitor.TryEnter(this))
            {
                _reentrantLevel++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Internal use only. Blocks until the lock is acquired.
        /// </summary>
        private void Acquire()
        {
            Monitor.Enter(this);
            _reentrantLevel++;
        }

        /// <summary>
        /// Internal use only. Releases the previously acquired lock.
        /// </summary>
        private void Release()
        {
            _reentrantLevel--;

            if (_reentrantLevel < 0)
            {
                throw new InvalidOperationException("Cannot release an unowned reentrant lock.");
            }

            Monitor.Exit(this);
        }

        #endregion
    }
}
