namespace NTDLS.Semaphore
{
    /// <summary>
    /// Protects an area of code from parallel / non-sequential thread access.
    /// </summary>
    public class CriticalSection : ICriticalResource
    {
        private static bool _enableGlobalLockRegistration = false;
        private static bool _enableThreadOwnershipTracking = false;

        /// <summary>
        /// Enables a dictonary of all threads that own locks. This can be handy when identifying deadlocks and race conditions.
        /// Once enabled, the tracking is attributed to all critical sections for the life of the applicaiton.
        /// </summary>
        public static void EnableGlobalLockRegistration() => _enableGlobalLockRegistration = true;

        /// <summary>
        /// Enables tracking of the current thread that owns the lock. This is only tracked if enabled by a call to EnableThreadOwnershipTracking().
        /// </summary>
        public static void EnableThreadOwnershipTracking() => _enableThreadOwnershipTracking = true;

        public delegate void CriticalSectionDelegateWithVoidResult();
        public delegate T? CriticalSectionDelegateWithNullableResultT<T>();
        public delegate T CriticalSectionDelegateWithNotNullableResultT<T>();

        /// <summary>
        /// A dictonary of all threads that own locks. This can be handy when identifying deadlocks and race conditions.
        /// This is only tracked if enabled by a call to EnableGlobalLockRegistration().
        /// Once enabled, the tracking is attributed to all critical sections for the life of the applicaiton.
        /// </summary>
        public readonly static Dictionary<string, ICriticalResource> GlobalLocks = new();

        /// <summary>
        /// Identifies the current thread that owns the lock. This is only tracked if enabled by a call to EnableThreadOwnershipTracking().
        /// Once enabled, the tracking is attributed to all critical sections for the life of the applicaiton.
        /// </summary>
        public Thread? OwnerThread { get; private set; }
        int _reentrantLevel = 0;

        /// <summary>
        /// Attempts to acquire the critical section, if successful executes and returns the given delegate result. Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="defaultValue"></param>
        /// <param name="function"></param>
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
        /// <param name="defaultValue"></param>
        /// <param name="function"></param>
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
        /// <param name="defaultValue"></param>
        /// <param name="wasLockObtained"></param>
        /// <param name="function"></param>
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
        /// <param name="defaultValue"></param>
        /// <param name="wasLockObtained"></param>
        /// <param name="timeout"></param>
        /// <param name="function"></param>
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
        /// <param name="function"></param>
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
        /// <param name="function"></param>
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
        /// <param name="wasLockObtained"></param>
        /// <param name="function"></param>
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
        /// <param name="wasLockObtained"></param>
        /// <param name="timeout"></param>
        /// <param name="function"></param>
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
        /// <param name="function"></param>
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
        /// <param name="function"></param>
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
        /// <param name="wasLockObtained"></param>
        /// <param name="function"></param>
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
        /// <param name="wasLockObtained"></param>
        /// <param name="timeout"></param>
        /// <param name="function"></param>
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
        /// <param name="function"></param>
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
        /// <param name="function"></param>
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
        /// <param name="function"></param>
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

        #region Internal interface functionality.

        /// <summary>
        /// Internal use only. Attempts to acquire the lock for a given number of milliseconds.
        /// </summary>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public bool TryAcquire(int timeoutMilliseconds)
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
        public bool TryAcquire()
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
        public void Acquire()
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
        public void Release()
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
