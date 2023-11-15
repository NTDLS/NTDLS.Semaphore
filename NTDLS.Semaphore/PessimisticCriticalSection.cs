namespace NTDLS.Semaphore
{
    /// <summary>
    /// Protects an area of code from parallel / non-sequential thread access.
    /// </summary>
    public class PessimisticCriticalSection : ICriticalSection
    {
        /// <summary>
        /// Identifies the current thread that owns the lock. This is only tracked if enabled by a call
        /// to EnableThreadOwnershipTracking(). Once enabled, the tracking is attributed to all critical
        /// sections for the life of the applicaiton - so its definitly best only enabled in debugging.
        /// </summary>
        public Thread? OwnerThread { get; private set; }
        private int _reentrantLevel = 0;

        #region Global static configuration.

        private static bool _enableGlobalLockRegistration = false;
        private static bool _enableThreadOwnershipTracking = false;

        /// <summary>
        /// Enables a dictonary of all threads that own locks. This can be handy when identifying deadlocks
        /// and race conditions. Once enabled, the tracking is attributed to all critical sections for the
        /// life of the applicaiton.  - so its definitly best only enabled in debugging.
        /// </summary>
        public static void EnableGlobalLockRegistration() => _enableGlobalLockRegistration = true;

        /// <summary>
        /// Enables tracking of the current thread that owns the lock. This is only tracked if enabled by a call to EnableThreadOwnershipTracking().
        /// </summary>
        public static void EnableThreadOwnershipTracking() => _enableThreadOwnershipTracking = true;

        #endregion

        #region Delegates.

        /// <summary>
        /// Delegate for executions that do not require a return value.
        /// </summary>
        public delegate void CriticalSectionDelegateWithVoidResult();

        /// <summary>
        /// Delegate for executions that require a nullable return value.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        /// <returns></returns>
        public delegate T? CriticalSectionDelegateWithNullableResultT<T>();

        /// <summary>
        /// Delegate for executions that require a non-nullable return value.
        /// </summary>
        /// <typeparam name="T">The type of the return value.</typeparam>
        /// <returns></returns>
        public delegate T CriticalSectionDelegateWithNotNullableResultT<T>();

        /// <summary>
        /// A dictonary of all threads that own locks. This can be handy when identifying deadlocks and race conditions.
        /// This is only tracked if enabled by a call to EnableGlobalLockRegistration().
        /// Once enabled, the tracking is attributed to all critical sections for the life of the applicaiton.
        /// </summary>
        public readonly static Dictionary<string, ICriticalSection> GlobalLocks = new();

        #endregion

        #region Use/TryUse overloads.

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes and returns the given delegate result. Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        /// <returns></returns>
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
        void ICriticalSection.Acquire(OptimisticCriticalSection.LockIntention intention)
            => Acquire();

        /// <summary>
        /// Internal use only. Attempts to acquire the lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        /// <returns></returns>
        bool ICriticalSection.TryAcquire(OptimisticCriticalSection.LockIntention intention)
            => TryAcquire();

        /// <summary>
        /// Internal use only. Attempts to acquire the lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        /// <returns></returns>
        bool ICriticalSection.TryAcquire(OptimisticCriticalSection.LockIntention intention, int timeoutMilliseconds)
            => TryAcquire(timeoutMilliseconds);

        /// <summary>
        /// Internal use only. Releases the previously acquired lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        void ICriticalSection.Release(OptimisticCriticalSection.LockIntention intention)
            => Release();

        /// <summary>
        /// Internal use only. Attempts to acquire the lock for a given number of milliseconds.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <returns></returns>
        bool ICriticalSection.TryAcquire(int timeoutMilliseconds)
            => TryAcquire(timeoutMilliseconds);

        /// <summary>
        /// Internal use only. Attempts to acquire the lock.
        /// </summary>
        /// <returns></returns>
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
        /// <returns></returns>
        private bool TryAcquire(int timeoutMilliseconds)
        {
            if (Monitor.TryEnter(this, timeoutMilliseconds))
            {
                if (_enableThreadOwnershipTracking)
                {
                    OwnerThread = Thread.CurrentThread;
                }
                _reentrantLevel++;

                if (_enableGlobalLockRegistration)
                {
                    if (_reentrantLevel == 1)
                    {
                        lock (GlobalLocks)
                        {
                            GlobalLocks.Add($"CS:{Thread.CurrentThread.ManagedThreadId}:{GetHashCode()}", this);
                        }
                    }
                }
                return true;
            }
            return false;
        }


        /// <summary>
        /// Internal use only. Attempts to acquire the lock.
        /// </summary>
        /// <returns></returns>
        private bool TryAcquire()
        {
            if (Monitor.TryEnter(this))
            {
                if (_enableThreadOwnershipTracking)
                {
                    OwnerThread = Thread.CurrentThread;
                }
                _reentrantLevel++;

                if (_enableGlobalLockRegistration)
                {
                    if (_reentrantLevel == 1)
                    {
                        lock (GlobalLocks)
                        {
                            GlobalLocks.Add($"CS:{Thread.CurrentThread.ManagedThreadId}:{GetHashCode()}", this);
                        }
                    }
                }

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
            if (_enableThreadOwnershipTracking)
            {
                OwnerThread = Thread.CurrentThread;
            }
            _reentrantLevel++;

            if (_enableGlobalLockRegistration)
            {
                if (_reentrantLevel == 1)
                {
                    lock (GlobalLocks)
                    {
                        GlobalLocks.Add($"CS:{Thread.CurrentThread.ManagedThreadId}:{GetHashCode()}", this);
                    }
                }
            }
        }

        /// <summary>
        /// Internal use only. Releases the previously acquired lock.
        /// </summary>
        private void Release()
        {
            _reentrantLevel--;

            if (_enableThreadOwnershipTracking && OwnerThread == null)
            {
                throw new InvalidOperationException("Cannot release an unowned lock.");
            }

            if (_reentrantLevel == 0)
            {
                if (_enableGlobalLockRegistration)
                {
                    lock (GlobalLocks)
                    {
                        GlobalLocks.Remove($"CS:{Thread.CurrentThread.ManagedThreadId}:{GetHashCode()}");
                    }
                }
                if (_enableThreadOwnershipTracking)
                {
                    OwnerThread = null;
                }
            }
            else if (_reentrantLevel < 0)
            {
                throw new InvalidOperationException("Cannot release an unowned reentrant lock.");
            }

            Monitor.Exit(this);
        }

        #endregion
    }
}
