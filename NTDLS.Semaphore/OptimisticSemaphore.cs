using static NTDLS.Semaphore.OptimisticCriticalSection;

namespace NTDLS.Semaphore
{
    /// <summary>
    ///Protects a variable from parallel / non-sequential thread access but controls read-only and exclusive
    ///access separately to prevent read operations from blocking other read operations.it is up to the developer
    ///to determine when each lock type is appropriate.Note: read-only locks only indicate intention, the resource
    ///will not disallow modification of the resource, but this will lead to race conditions.
    /// </summary>
    /// <typeparam name="T">The type of the resource that will be instantiated and protected.</typeparam>
    public class OptimisticSemaphore<T> where T : class, new()
    {
        private readonly T _value;
        private readonly ICriticalSection _criticalSection;

        #region Local Types.

        private class CriticalCollection
        {
            public ICriticalSection Resource { get; set; }
            public bool IsLockHeld { get; set; } = false;

            public CriticalCollection(ICriticalSection resource)
            {
                Resource = resource;
            }
        }

        #endregion

        #region Delegates.

        /// <summary>
        /// Delegate for executions that do not require a return value.
        /// </summary>
        /// <param name="obj">The variable that is being protected.</param>
        public delegate void CriticalResourceDelegateWithVoidResult(T obj);

        /// <summary>
        /// Delegate for executions that require a nullable return value.
        /// </summary>
        /// <typeparam name="R">The type of the return value.</typeparam>
        /// <param name="obj">The variable that is being protected.</param>
        /// <returns></returns>
        public delegate R? CriticalResourceDelegateWithNullableResultT<R>(T obj);

        /// <summary>
        /// Delegate for executions that require a non-nullable return value.
        /// </summary>
        /// <typeparam name="R">The type of the return value.</typeparam>
        /// <param name="obj">The variable that is being protected.</param>
        /// <returns></returns>
        public delegate R CriticalResourceDelegateWithNotNullableResultT<R>(T obj);

        #endregion

        #region Constructors.

        /// <summary>
        /// Initializes a new optimistic semaphore that envelopes a variable.
        /// </summary>
        public OptimisticSemaphore()
        {
            _criticalSection = new OptimisticCriticalSection();
            _value = new T();
        }

        /// <summary>
        /// Initializes a new optimistic semaphore that envelopes a variable with a set value. This allows you to protect a variable that has a non-empty constructor.
        /// </summary>
        /// <param name="value"></param>
        public OptimisticSemaphore(T value)
        {
            _criticalSection = new OptimisticCriticalSection();
            _value = value;
        }

        /// <summary>
        /// Envelopes a variable using a predefined critical section.
        /// If other optimistic semaphores use the same critical section, they will require the lock of the shared critical section.
        /// </summary>
        /// <param name="criticalSection"></param>
        public OptimisticSemaphore(ICriticalSection criticalSection)
        {
            _criticalSection = criticalSection;
            _value = new T();
            _criticalSection = criticalSection;
        }

        /// <summary>
        /// Envelopes a variable with a set value, using a predefined critical section. This allows you to protect a variable that has a non-empty constructor.
        /// If other optimistic semaphores use the same critical section, they will require the lock of the shared critical section.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="criticalSection"></param>
        public OptimisticSemaphore(T value, ICriticalSection criticalSection)
        {
            _value = value;
            _criticalSection = criticalSection;
        }

        #endregion

        #region Read/Write/TryRead/TryWrite overloads.

        /// <summary>
        /// Block until the lock is acquired then executes the delegate function and returns the non-nullable value from the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        /// <returns></returns>
        public R Read<R>(CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            try
            {
                _criticalSection.Acquire(LockIntention.Readonly);
                return function(_value);
            }
            finally
            {
                _criticalSection.Release(LockIntention.Readonly);
            }
        }

        /// <summary>
        /// Block until the lock is acquired then executes the delegate function and returns the non-nullable value from the delegate function.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        /// <returns></returns>
        public R Write<R>(CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            try
            {
                _criticalSection.Acquire(LockIntention.Exclusive);
                return function(_value);
            }
            finally
            {
                _criticalSection.Release(LockIntention.Exclusive);
            }
        }

        /// <summary>
        /// Block until the lock is acquired then executes the delegate function and return the nullable value from the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        /// <returns></returns>
        public R? ReadNullable<R>(CriticalResourceDelegateWithNullableResultT<R> function)
        {
            try
            {
                _criticalSection.Acquire(LockIntention.Readonly);
                return function(_value);
            }
            finally
            {
                _criticalSection.Release(LockIntention.Readonly);
            }
        }

        /// <summary>
        /// Block until the lock is acquired then executes the delegate function and return the nullable value from the delegate function.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        /// <returns></returns>
        public R? WriteNullable<R>(CriticalResourceDelegateWithNullableResultT<R> function)
        {
            try
            {
                _criticalSection.Acquire(LockIntention.Exclusive);
                return function(_value);
            }
            finally
            {
                _criticalSection.Release(LockIntention.Exclusive);
            }
        }

        /// <summary>
        /// Blocks until the lock is acquired then executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public void Read(CriticalResourceDelegateWithVoidResult function)
        {
            try
            {
                _criticalSection.Acquire(LockIntention.Readonly);
                function(_value);
            }
            finally
            {
                _criticalSection.Release(LockIntention.Readonly);
            }
        }

        /// <summary>
        /// Blocks until the lock is acquired then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public void Write(CriticalResourceDelegateWithVoidResult function)
        {
            try
            {
                _criticalSection.Acquire(LockIntention.Exclusive);
                function(_value);
            }
            finally
            {
                _criticalSection.Release(LockIntention.Exclusive);
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryRead(out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Readonly);
                if (wasLockObtained)
                {
                    function(_value);
                    return;
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }
        }


        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryWrite(out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Exclusive);
                if (wasLockObtained)
                {
                    function(_value);
                    return;
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryRead(CriticalResourceDelegateWithVoidResult function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Readonly);
                if (wasLockObtained)
                {
                    function(_value);
                    return;
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryWrite(CriticalResourceDelegateWithVoidResult function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Exclusive);
                if (wasLockObtained)
                {
                    function(_value);
                    return;
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryRead(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Readonly, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    function(_value);
                    return;
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryWrite(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    function(_value);
                    return;
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryRead(int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Readonly, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    function(_value);
                    return;
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryWrite(int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    function(_value);
                    return;
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function and returns the nullable delegate value, otherwise returns null.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R? TryRead<R>(out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Readonly);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }
            return default;
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function and returns the nullable delegate value, otherwise returns null.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R? TryWrite<R>(out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Exclusive);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }
            return default;
        }

        /// <summary>
        /// Attempts to acquire the lock for a given number of milliseconds, if successful then executes the delegate function and returns the nullable delegate value,
        /// otherwise returns null.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>

        public R? TryRead<R>(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Readonly, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }

            return default;
        }

        /// <summary>
        /// Attempts to acquire the lock for a given number of milliseconds, if successful then executes the delegate function and returns the nullable delegate value,
        /// otherwise returns null.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>

        public R? TryWrite<R>(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }

            return default;
        }

        /// <summary>
        /// Attempts to acquire the lock. If successful then executes the delegate function and returns the non-nullable delegate value.
        /// Otherwise returns the supplied default value.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R TryRead<R>(out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Readonly);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Attempts to acquire the lock. If successful then executes the delegate function and returns the non-nullable delegate value.
        /// Otherwise returns the supplied default value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R TryWrite<R>(out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Exclusive);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Attempts to acquire the lock. If successful then executes the delegate function and returns the non-nullable delegate value.
        /// Otherwise returns the supplied default value.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R TryRead<R>(R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Readonly);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Attempts to acquire the lock. If successful then executes the delegate function and returns the non-nullable delegate value.
        /// Otherwise returns the supplied default value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R TryWrite<R>(R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Exclusive);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds. If successful then executes the delegate function and returns the non-nullable delegate value.
        /// Otherwise returns the supplied default value.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R TryRead<R>(out bool wasLockObtained, R defaultValue, int timeoutMilliseconds, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Readonly, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds. If successful then executes the delegate function and returns the non-nullable delegate value.
        /// Otherwise returns the supplied default value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R TryWrite<R>(out bool wasLockObtained, R defaultValue, int timeoutMilliseconds, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds. If successful then executes the delegate function and returns the non-nullable delegate value.
        /// Otherwise returns the supplied default value.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R TryRead<R>(R defaultValue, int timeoutMilliseconds, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Readonly, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds. If successful then executes the delegate function and returns the non-nullable delegate value.
        /// Otherwise returns the supplied default value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R TryWrite<R>(R defaultValue, int timeoutMilliseconds, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }

            return defaultValue;
        }

        #endregion

        #region Use All (Write)

        /// <summary>
        /// Attmepts to acquire the lock. If successful, executes the delegate function.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryWriteAll(ICriticalSection[] resources, out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (_criticalSection.TryAcquire(LockIntention.Exclusive))
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(LockIntention.Exclusive);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Exclusive);
                            }

                            wasLockObtained = false;
                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Exclusive);
                    }
                    wasLockObtained = true;

                    return;
                }
                finally
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }

            wasLockObtained = false;
        }

        /// <summary>
        /// Attmepts to acquire the lock for the specified number of milliseconds. If successful, executes the delegate function.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryWriteAll(ICriticalSection[] resources, int timeoutMilliseconds, out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (_criticalSection.TryAcquire(LockIntention.Exclusive))
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Exclusive);
                            }

                            wasLockObtained = false;
                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Exclusive);
                    }
                    wasLockObtained = true;

                    return;
                }
                finally
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }

            wasLockObtained = false;
        }

        /// <summary>
        /// Attmepts to acquire the lock. If successful, executes the delegate function and returns the nullable value from the delegate function.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R? TryWriteAll<R>(ICriticalSection[] resources, out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (_criticalSection.TryAcquire(LockIntention.Exclusive))
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(LockIntention.Exclusive);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Exclusive);
                            }

                            wasLockObtained = false;
                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Exclusive);
                    }
                    wasLockObtained = true;

                    return result;
                }
                finally
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }

            wasLockObtained = false;
            return default;
        }

        /// <summary>
        /// Attmepts to acquire the lock. If successful, executes the delegate function and returns the non-nullable value from the delegate function.
        /// Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R TryWriteAll<R>(ICriticalSection[] resources, out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (_criticalSection.TryAcquire(LockIntention.Exclusive))
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(LockIntention.Exclusive);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Exclusive);
                            }

                            wasLockObtained = false;
                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Exclusive);
                    }
                    wasLockObtained = true;

                    return result;
                }
                finally
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }

            wasLockObtained = false;
            return defaultValue;
        }

        /// <summary>
        /// Attmepts to acquire the lock for the given number of milliseconds. If successful, executes the delegate function and
        /// returns the non-nullable value from the delegate function. Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R? TryWriteAll<R>(ICriticalSection[] resources, int timeoutMilliseconds, out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (_criticalSection.TryAcquire(LockIntention.Exclusive))
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Exclusive);
                            }

                            wasLockObtained = false;
                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Exclusive);
                    }
                    wasLockObtained = true;

                    return result;
                }
                finally
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }

            wasLockObtained = false;
            return defaultValue;
        }

        /// <summary>
        /// Attmepts to acquire the lock for the given number of milliseconds. If successful, executes the delegate function and
        /// returns the nullable value from the delegate function. Otherwise returns null.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R? TryWriteAll<R>(ICriticalSection[] resources, int timeoutMilliseconds, out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (_criticalSection.TryAcquire(LockIntention.Exclusive))
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Exclusive);
                            }

                            wasLockObtained = false;
                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Exclusive);
                    }
                    wasLockObtained = true;

                    return result;
                }
                finally
                {
                    _criticalSection.Release(LockIntention.Exclusive);
                }
            }

            wasLockObtained = false;
            return default;
        }

        /// <summary>
        /// Attmepts to acquire the lock base lock as well as all supplied locks. If successful, executes the delegate function and
        /// returns the nullable value from the delegate function. Otherwise returns null.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        /// <returns></returns>
        public R? WriteAll<R>(ICriticalSection[] resources, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            _criticalSection.Acquire(LockIntention.Exclusive);

            try
            {
                foreach (var res in resources)
                {
                    res.Acquire(LockIntention.Exclusive);
                }

                var result = function(_value);

                foreach (var res in resources)
                {
                    res.Release(LockIntention.Exclusive);
                }

                return result;
            }
            finally
            {
                _criticalSection.Release(LockIntention.Exclusive);
            }
        }

        /// <summary>
        /// Blocks until the base lock as well as all supplied locks are acquired then executes the delegate function and
        /// returns the non-nullable value from the delegate function.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        /// <returns></returns>
        public R WriteAll<R>(ICriticalSection[] resources, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            _criticalSection.Acquire(LockIntention.Exclusive);

            try
            {
                foreach (var res in resources)
                {
                    res.Acquire(LockIntention.Exclusive);
                }

                var result = function(_value);

                foreach (var res in resources)
                {
                    res.Release(LockIntention.Exclusive);
                }

                return result;
            }
            finally
            {
                _criticalSection.Release(LockIntention.Exclusive);
            }
        }

        /// <summary>
        /// Blocks until the base lock as well as all supplied locks are acquired then executes the delegate function.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public void WriteAll(ICriticalSection[] resources, CriticalResourceDelegateWithVoidResult function)
        {
            _criticalSection.Acquire(LockIntention.Exclusive);

            try
            {
                foreach (var res in resources)
                {
                    res.Acquire(LockIntention.Exclusive);
                }

                function(_value);

                foreach (var res in resources)
                {
                    res.Release(LockIntention.Exclusive);
                }
            }
            finally
            {
                _criticalSection.Release(LockIntention.Exclusive);
            }
        }


        #endregion

        #region Use All (Read)

        /// <summary>
        /// Attmepts to acquire the lock. If successful, executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryReadAll(ICriticalSection[] resources, out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (_criticalSection.TryAcquire(LockIntention.Readonly))
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(LockIntention.Readonly);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Readonly);
                            }

                            wasLockObtained = false;
                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Readonly);
                    }
                    wasLockObtained = true;

                    return;
                }
                finally
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }

            wasLockObtained = false;
        }

        /// <summary>
        /// Attmepts to acquire the lock for the specified number of milliseconds. If successful, executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryReadAll(ICriticalSection[] resources, int timeoutMilliseconds, out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (_criticalSection.TryAcquire(LockIntention.Readonly))
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(LockIntention.Readonly, timeoutMilliseconds);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Readonly);
                            }

                            wasLockObtained = false;
                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Readonly);
                    }
                    wasLockObtained = true;

                    return;
                }
                finally
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }

            wasLockObtained = false;
        }

        /// <summary>
        /// Attmepts to acquire the lock. If successful, executes the delegate function and returns the nullable value from the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R? TryReadAll<R>(ICriticalSection[] resources, out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (_criticalSection.TryAcquire(LockIntention.Readonly))
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(LockIntention.Readonly);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Readonly);
                            }

                            wasLockObtained = false;
                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Readonly);
                    }
                    wasLockObtained = true;

                    return result;
                }
                finally
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }

            wasLockObtained = false;
            return default;
        }

        /// <summary>
        /// Attmepts to acquire the lock. If successful, executes the delegate function and returns the non-nullable value from the delegate function,
        /// otherwise returns the given default value.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns></returns>
        public R TryReadAll<R>(ICriticalSection[] resources, out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (_criticalSection.TryAcquire(LockIntention.Readonly))
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(LockIntention.Readonly);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Readonly);
                            }

                            wasLockObtained = false;
                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Readonly);
                    }
                    wasLockObtained = true;

                    return result;
                }
                finally
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }

            wasLockObtained = false;
            return defaultValue;
        }

        /// <summary>
        /// Attmepts to acquire the lock for the given number of milliseconds. If successful, executes the delegate function and
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns>the non-nullable value from the delegate function. Otherwise returns the given default value.</returns>
        public R? TryReadAll<R>(ICriticalSection[] resources, int timeoutMilliseconds, out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (_criticalSection.TryAcquire(LockIntention.Readonly))
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(LockIntention.Readonly, timeoutMilliseconds);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Readonly);
                            }

                            wasLockObtained = false;
                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Readonly);
                    }
                    wasLockObtained = true;

                    return result;
                }
                finally
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }

            wasLockObtained = false;
            return defaultValue;
        }

        /// <summary>
        /// Attmepts to acquire the lock for the given number of milliseconds. If successful, executes the delegate function and
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns>The nullable value from the delegate function. Otherwise returns null.</returns>
        public R? TryReadAll<R>(ICriticalSection[] resources, int timeoutMilliseconds, out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (_criticalSection.TryAcquire(LockIntention.Readonly))
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(LockIntention.Readonly, timeoutMilliseconds);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Readonly);
                            }

                            wasLockObtained = false;
                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Readonly);
                    }
                    wasLockObtained = true;

                    return result;
                }
                finally
                {
                    _criticalSection.Release(LockIntention.Readonly);
                }
            }

            wasLockObtained = false;
            return default;
        }

        /// <summary>
        /// Attmepts to acquire the lock base lock as well as all supplied locks. If successful, executes the delegate function and
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        /// <returns>The nullable value from the delegate function. Otherwise returns null</returns>
        public R? ReadAll<R>(ICriticalSection[] resources, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            _criticalSection.Acquire(LockIntention.Readonly);

            try
            {
                foreach (var res in resources)
                {
                    res.Acquire(LockIntention.Readonly);
                }

                var result = function(_value);

                foreach (var res in resources)
                {
                    res.Release(LockIntention.Readonly);
                }

                return result;
            }
            finally
            {
                _criticalSection.Release(LockIntention.Readonly);
            }
        }

        /// <summary>
        /// Blocks until the base lock as well as all supplied locks are acquired then executes the delegate function and
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        /// <returns>The non-nullable value from the delegate function</returns>
        public R ReadAll<R>(ICriticalSection[] resources, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            _criticalSection.Acquire(LockIntention.Readonly);

            try
            {
                foreach (var res in resources)
                {
                    res.Acquire(LockIntention.Readonly);
                }

                var result = function(_value);

                foreach (var res in resources)
                {
                    res.Release(LockIntention.Readonly);
                }

                return result;
            }
            finally
            {
                _criticalSection.Release(LockIntention.Readonly);
            }
        }

        /// <summary>
        /// Blocks until the base lock as well as all supplied locks are acquired then executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public void ReadAll(ICriticalSection[] resources, CriticalResourceDelegateWithVoidResult function)
        {
            _criticalSection.Acquire(LockIntention.Readonly);

            try
            {
                foreach (var res in resources)
                {
                    res.Acquire(LockIntention.Readonly);
                }

                function(_value);

                foreach (var res in resources)
                {
                    res.Release(LockIntention.Readonly);
                }
            }
            finally
            {
                _criticalSection.Release(LockIntention.Readonly);
            }
        }


        #endregion
    }
}
