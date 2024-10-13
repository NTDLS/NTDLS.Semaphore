using System.Runtime.CompilerServices;
using static NTDLS.Semaphore.OptimisticSemaphore;

namespace NTDLS.Semaphore
{
    /// <summary>
    ///Protects a variable from parallel / non-sequential thread access but controls read-only and exclusive
    /// access separately to prevent read operations from blocking other read operations. It is up to the developer
    /// to determine when each lock type is appropriate.Note: read-only locks only indicate intention, the resource
    /// will not disallow modification of the resource, but this will lead to race conditions.
    /// </summary>
    /// <typeparam name="T">The type of the resource that will be instantiated and protected.</typeparam>
    public class OptimisticCriticalResource<T> : ICriticalSection where T : class, new()
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
        /// Used by the constructor to allow for advanced initialization of the enclosed value.
        /// </summary>
        public delegate T InitializationCallback();

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
        public delegate R? CriticalResourceDelegateWithNullableResultT<R>(T obj);

        /// <summary>
        /// Delegate for executions that require a non-nullable return value.
        /// </summary>
        /// <typeparam name="R">The type of the return value.</typeparam>
        /// <param name="obj">The variable that is being protected.</param>
        public delegate R CriticalResourceDelegateWithNotNullableResultT<R>(T obj);

        #endregion

        #region Constructors.

        /// <summary>
        /// Initializes a new optimistic semaphore that envelopes a variable.
        /// </summary>
        public OptimisticCriticalResource(InitializationCallback initializationCallback)
        {
            _criticalSection = new OptimisticSemaphore();
            _value = initializationCallback();
        }

        /// <summary>
        /// Envelopes a variable with a set value, using a predefined critical section. This allows you to protect a variable that has a non-empty constructor.
        /// If other optimistic semaphores use the same critical section, they will require the lock of the shared critical section.
        /// </summary>
        public OptimisticCriticalResource(InitializationCallback initializationCallback, ICriticalSection criticalSection)
        {
            _value = initializationCallback();
            _criticalSection = criticalSection;
        }

        /// <summary>
        /// Initializes a new optimistic semaphore that envelopes a variable.
        /// </summary>
        public OptimisticCriticalResource()
        {
            _criticalSection = new OptimisticSemaphore();
            _value = new T();
        }

        /// <summary>
        /// Initializes a new optimistic semaphore that envelopes a variable with a set value. This allows you to protect a variable that has a non-empty constructor.
        /// </summary>
        /// <param name="value"></param>
        public OptimisticCriticalResource(T value)
        {
            _criticalSection = new OptimisticSemaphore();
            _value = value;
        }

        /// <summary>
        /// Envelopes a variable using a predefined critical section.
        /// If other optimistic semaphores use the same critical section, they will require the lock of the shared critical section.
        /// </summary>
        public OptimisticCriticalResource(ICriticalSection criticalSection)
        {
            _criticalSection = criticalSection;
            _value = new T();
        }

        /// <summary>
        /// Envelopes a variable with a set value, using a predefined critical section. This allows you to protect a variable that has a non-empty constructor.
        /// If other optimistic semaphores use the same critical section, they will require the lock of the shared critical section.
        /// </summary>
        public OptimisticCriticalResource(T value, ICriticalSection criticalSection)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? TryReadNullable<R>(out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? TryWriteNullable<R>(out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? TryReadNullable<R>(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithNullableResultT<R> function)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? TryWriteNullable<R>(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithNullableResultT<R> function)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        #region UpgradableRead/TryUpgradableRead overloads.

        /// <summary>
        /// Block until the lock is acquired then executes the delegate function and returns the non-nullable value from the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R UpgradableRead<R>(CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            try
            {
                _criticalSection.Acquire(LockIntention.UpgradableRead);
                return function(_value);
            }
            finally
            {
                _criticalSection.Release(LockIntention.UpgradableRead);
            }
        }

        /// <summary>
        /// Block until the lock is acquired then executes the delegate function and return the nullable value from the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? ReadUpgradableNullable<R>(CriticalResourceDelegateWithNullableResultT<R> function)
        {
            try
            {
                _criticalSection.Acquire(LockIntention.UpgradableRead);
                return function(_value);
            }
            finally
            {
                _criticalSection.Release(LockIntention.UpgradableRead);
            }
        }

        /// <summary>
        /// Blocks until the lock is acquired then executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpgradableRead(CriticalResourceDelegateWithVoidResult function)
        {
            try
            {
                _criticalSection.Acquire(LockIntention.UpgradableRead);
                function(_value);
            }
            finally
            {
                _criticalSection.Release(LockIntention.UpgradableRead);
            }
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock, if successful then executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryUpgradableRead(out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.UpgradableRead);
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
                    _criticalSection.Release(LockIntention.UpgradableRead);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock, if successful then executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryUpgradableRead(CriticalResourceDelegateWithVoidResult function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.UpgradableRead);
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
                    _criticalSection.Release(LockIntention.UpgradableRead);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock for the given number of milliseconds, if successful then executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryUpgradableRead(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.UpgradableRead, timeoutMilliseconds);
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
                    _criticalSection.Release(LockIntention.UpgradableRead);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock for the given number of milliseconds, if successful then executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryUpgradableRead(int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.UpgradableRead, timeoutMilliseconds);
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
                    _criticalSection.Release(LockIntention.UpgradableRead);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock, if successful then executes the delegate function and returns the nullable delegate value, otherwise returns null.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? TryUpgradableReadNullable<R>(out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.UpgradableRead);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.UpgradableRead);
                }
            }
            return default;
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock for a given number of milliseconds, if successful then executes the delegate function and returns the nullable delegate value,
        /// otherwise returns null.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? TryUpgradableReadNullable<R>(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.UpgradableRead, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.UpgradableRead);
                }
            }

            return default;
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock. If successful then executes the delegate function and returns the non-nullable delegate value.
        /// Otherwise returns the supplied default value.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R TryUpgradableRead<R>(out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.UpgradableRead);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.UpgradableRead);
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock. If successful then executes the delegate function and returns the non-nullable delegate value.
        /// Otherwise returns the supplied default value.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R TryUpgradableRead<R>(R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.UpgradableRead);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.UpgradableRead);
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock for the given number of milliseconds. If successful then executes the delegate function and returns the non-nullable delegate value.
        /// Otherwise returns the supplied default value.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R TryUpgradableRead<R>(out bool wasLockObtained, R defaultValue, int timeoutMilliseconds, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.UpgradableRead, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.UpgradableRead);
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock for the given number of milliseconds. If successful then executes the delegate function and returns the non-nullable delegate value.
        /// Otherwise returns the supplied default value.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R TryUpgradableRead<R>(R defaultValue, int timeoutMilliseconds, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = _criticalSection.TryAcquire(LockIntention.UpgradableRead, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    _criticalSection.Release(LockIntention.UpgradableRead);
                }
            }

            return defaultValue;
        }

        #endregion

        #region Use All (Write)

        /// <summary>
        /// Attempts to acquire the lock. If successful, executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns>Returns true if the lock was obtained</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteAll(ICriticalSection[] resources, CriticalResourceDelegateWithVoidResult function)
        {
            TryWriteAll(resources, out bool wasLockObtained, function);
            return wasLockObtained;
        }

        /// <summary>
        /// Attempts to acquire the lock. If successful, executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns>Returns true if the lock was obtained</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteAll(ICriticalSection[] resources, int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            TryWriteAll(resources, timeoutMilliseconds, out bool wasLockObtained, function);
            return wasLockObtained;
        }

        /// <summary>
        /// Attempts to acquire the lock. If successful, executes the delegate function.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Exclusive);
                            }

                            wasLockObtained = false;
                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
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
        /// Attempts to acquire the lock for the specified number of milliseconds. If successful, executes the delegate function.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Exclusive);
                            }

                            wasLockObtained = false;
                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
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
        /// Attempts to acquire the lock. If successful, executes the delegate function and returns the nullable value from the delegate function.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? TryWriteAllNullable<R>(ICriticalSection[] resources, out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
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
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Exclusive);
                            }

                            wasLockObtained = false;
                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
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
        /// Attempts to acquire the lock. If successful, executes the delegate function and returns the non-nullable value from the delegate function.
        /// Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Exclusive);
                            }

                            wasLockObtained = false;
                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
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
        /// Attempts to acquire the lock for the given number of milliseconds. If successful, executes the delegate function and
        /// returns the non-nullable value from the delegate function. Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? TryWriteAllNullable<R>(ICriticalSection[] resources, int timeoutMilliseconds, out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNullableResultT<R> function)
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
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Exclusive);
                            }

                            wasLockObtained = false;
                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
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
        /// Attempts to acquire the lock for the given number of milliseconds. If successful, executes the delegate function and
        /// returns the nullable value from the delegate function. Otherwise returns null.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? TryWriteAllNullable<R>(ICriticalSection[] resources, int timeoutMilliseconds, out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
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
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Exclusive);
                            }

                            wasLockObtained = false;
                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
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
        /// Attempts to acquire the lock base lock as well as all supplied locks. If successful, executes the delegate function and
        /// returns the nullable value from the delegate function. Otherwise returns null.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? WriteAllNullable<R>(ICriticalSection[] resources, CriticalResourceDelegateWithNullableResultT<R> function)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        /// Due to the way that the locks are obtained, WriteAll() can lead to lock interleaving which will cause a deadlock if called in parallel with other calls to UseAll(), ReadAll() or WriteAll().
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        /// Attempts to acquire the lock. If successful, executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns>Returns true if the lock was obtained</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadAll(ICriticalSection[] resources, CriticalResourceDelegateWithVoidResult function)
        {
            TryReadAll(resources, out bool wasLockObtained, function);
            return wasLockObtained;
        }

        /// <summary>
        /// Attempts to acquire the lock. If successful, executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns>Returns true if the lock was obtained</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadAll(ICriticalSection[] resources, int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            TryReadAll(resources, timeoutMilliseconds, out bool wasLockObtained, function);
            return wasLockObtained;
        }

        /// <summary>
        /// Attempts to acquire the lock. If successful, executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Readonly);
                            }

                            wasLockObtained = false;
                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
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
        /// Attempts to acquire the lock for the specified number of milliseconds. If successful, executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Readonly);
                            }

                            wasLockObtained = false;
                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
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
        /// Attempts to acquire the lock. If successful, executes the delegate function and returns the nullable value from the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? TryReadAllNullable<R>(ICriticalSection[] resources, out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
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
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Readonly);
                            }

                            wasLockObtained = false;
                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
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
        /// Attempts to acquire the lock. If successful, executes the delegate function and returns the non-nullable value from the delegate function,
        /// otherwise returns the given default value.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Readonly);
                            }

                            wasLockObtained = false;
                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
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
        /// Attempts to acquire the lock for the given number of milliseconds. If successful, executes the delegate function and
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns>the non-nullable value from the delegate function. Otherwise returns the given default value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? TryReadAllNullable<R>(ICriticalSection[] resources, int timeoutMilliseconds, out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNullableResultT<R> function)
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
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Readonly);
                            }

                            wasLockObtained = false;
                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
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
        /// Attempts to acquire the lock for the given number of milliseconds. If successful, executes the delegate function and
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns>The nullable value from the delegate function. Otherwise returns null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? TryReadAllNullable<R>(ICriticalSection[] resources, int timeoutMilliseconds, out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
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
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release(LockIntention.Readonly);
                            }

                            wasLockObtained = false;
                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
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
        /// Attempts to acquire the lock base lock as well as all supplied locks. If successful, executes the delegate function and
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        /// <returns>The nullable value from the delegate function. Otherwise returns null</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public R? ReadAllNullable<R>(ICriticalSection[] resources, CriticalResourceDelegateWithNullableResultT<R> function)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        /// Due to the way that the locks are obtained, ReadAll() can lead to lock interleaving which will cause a deadlock if called in parallel with other calls to UseAll(), ReadAll() or WriteAll().
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        #region Internal interface functionality.

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
            => _criticalSection.TryAcquire(timeoutMilliseconds);

        /// <summary>
        /// Internal use only. Attempts to acquire the lock.
        /// </summary>
        private bool TryAcquire()
            => _criticalSection.TryAcquire();

        /// <summary>
        /// Internal use only. Blocks until the lock is acquired.
        /// </summary>
        private void Acquire()
            => _criticalSection.Acquire();

        /// <summary>
        /// Internal use only. Releases the previously acquired lock.
        /// </summary>
        private void Release()
            => _criticalSection.Release();

        /// <summary>
        /// Internal use only. Blocks until the lock is acquired.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        void ICriticalSection.Acquire(LockIntention intention)
            => _criticalSection.Acquire(intention);

        /// <summary>
        /// Internal use only. Attempts to acquire the lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        bool ICriticalSection.TryAcquire(LockIntention intention)
            => _criticalSection.TryAcquire(intention);

        /// <summary>
        /// Internal use only. Attempts to acquire the lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        bool ICriticalSection.TryAcquire(LockIntention intention, int timeoutMilliseconds)
            => _criticalSection.TryAcquire(intention, timeoutMilliseconds);

        /// <summary>
        /// Internal use only. Releases the previously acquired lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        void ICriticalSection.Release(LockIntention intention)
            => _criticalSection.Release(intention);

        #endregion
    }
}
