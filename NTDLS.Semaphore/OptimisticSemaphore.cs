using System.Runtime.CompilerServices;

namespace NTDLS.Semaphore
{
    /// <summary>
    /// The optimistic semaphore is at the core of the optimistic critical resource.
    /// Can be instantiated externally and shared across optimistic semaphores
    /// </summary>
    public class OptimisticSemaphore : ICriticalSection
    {
        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new(LockRecursionPolicy.SupportsRecursion);

        #region Delegates.

        /// <summary>
        /// Delegate for executions that do not require a return value.
        /// </summary>
        public delegate void CriticalResourceDelegateWithVoidResult();

        /// <summary>
        /// Delegate for executions that require a nullable return value.
        /// </summary>
        /// <typeparam name="R">The type of the return value.</typeparam>
        public delegate R? CriticalResourceDelegateWithNullableResultT<R>();

        /// <summary>
        /// Delegate for executions that require a non-nullable return value.
        /// </summary>
        /// <typeparam name="R">The type of the return value.</typeparam>
        public delegate R CriticalResourceDelegateWithNotNullableResultT<R>();

        #endregion

        #region Local types.

        /// <summary>
        /// The type of action that the code intends to perform after the lock is obtained.
        /// </summary>
        public enum LockIntention
        {
            /// <summary>
            /// The code intends to modify the resource (such as remove an item from a collection).
            /// </summary>
            Exclusive,
            /// <summary>
            /// The code intends only to read the resource (such as iterate through a a collection).
            /// </summary>
            Readonly
        }

        #endregion

        #region Internal lock controls.

        /// <summary>
        /// Acquires an exclusive lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICriticalSection.Acquire()
            => Acquire(LockIntention.Exclusive);

        /// <summary>
        /// Releases an exclusive lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICriticalSection.Release()
            => Release(LockIntention.Exclusive);

        /// <summary>
        /// Acquires an exclusive lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool ICriticalSection.TryAcquire()
            => TryAcquire(LockIntention.Exclusive);

        /// <summary>
        /// Releases an exclusive lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool ICriticalSection.TryAcquire(int timeoutMilliseconds)
            => TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);

        /// <summary>
        /// Acquires a lock with and returns when it is held.
        /// </summary>
        /// <param name="intention"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICriticalSection.Acquire(LockIntention intention)
            => Acquire(intention);

        /// <summary>
        /// Tries to acquire a lock one single time and then gives up.
        /// </summary>
        /// <param name="intention"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool ICriticalSection.TryAcquire(LockIntention intention)
            => TryAcquire(intention);

        /// <summary>
        /// Tries to acquire a lock for a given time and then gives up.
        /// </summary>
        /// <param name="intention"></param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool ICriticalSection.TryAcquire(LockIntention intention, int timeoutMilliseconds)
            => TryAcquire(intention, timeoutMilliseconds);

        /// <summary>
        /// Releases the lock held by the current thread.
        /// </summary>
        /// <param name="intention"></param>
        /// <exception cref="Exception"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ICriticalSection.Release(LockIntention intention)
            => Release(intention);

        /// <summary>
        /// Acquires a lock with and returns when it is held.
        /// </summary>
        /// <param name="intention"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Acquire(LockIntention intention)
            => TryAcquire(intention, -1);

        /// <summary>
        /// Tries to acquire a lock one single time and then gives up.
        /// </summary>
        /// <param name="intention"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryAcquire(LockIntention intention)
            => TryAcquire(intention, 0);

        /// <summary>
        /// Tries to acquire a lock for a given time and then gives up.
        /// </summary>
        /// <param name="intention"></param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryAcquire(LockIntention intention, int timeoutMilliseconds)
        {
            if (intention == LockIntention.Readonly)
                return _readerWriterLockSlim.TryEnterReadLock(timeoutMilliseconds);
            else if (intention == LockIntention.Exclusive)
                return _readerWriterLockSlim.TryEnterWriteLock(timeoutMilliseconds);

            throw new Exception("The lock intention type is not implemented");
        }

        /// <summary>
        /// Releases the lock held by the current thread.
        /// </summary>
        /// <param name="intention"></param>
        /// <exception cref="Exception"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Release(LockIntention intention)
        {
            if (intention == LockIntention.Readonly)
                _readerWriterLockSlim.ExitReadLock();
            else if (intention == LockIntention.Exclusive)
                _readerWriterLockSlim.ExitWriteLock();
        }

        #endregion

        #region Read/Write/TryRead/TryWrite overloads.

        /// <summary>
        /// Blocks until the lock is acquired then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public void Read(CriticalResourceDelegateWithVoidResult function)
        {
            try
            {
                Acquire(LockIntention.Readonly);
                function();
            }
            finally
            {
                Release(LockIntention.Readonly);
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
                Acquire(LockIntention.Exclusive);
                function();
            }
            finally
            {
                Release(LockIntention.Exclusive);
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryRead(out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Readonly);
                if (wasLockObtained)
                {
                    function();
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Readonly);
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
                wasLockObtained = TryAcquire(LockIntention.Exclusive);
                if (wasLockObtained)
                {
                    function();
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Exclusive);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryRead(CriticalResourceDelegateWithVoidResult function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Readonly);
                if (wasLockObtained)
                {
                    function();
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Readonly);
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
                wasLockObtained = TryAcquire(LockIntention.Exclusive);
                if (wasLockObtained)
                {
                    function();
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Exclusive);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryRead(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Readonly, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    function();
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Readonly);
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
                wasLockObtained = TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    function();
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Exclusive);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryRead(int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Readonly, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    function();
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Readonly);
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
                wasLockObtained = TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    function();
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Exclusive);
                }
            }
        }


        #endregion

        #region Read/Write/TryRead/TryWrite overloads (with returns).

        /// <summary>
        /// Blocks until the lock is acquired then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public R Read<R>(CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            try
            {
                Acquire(LockIntention.Readonly);
                return function();
            }
            finally
            {
                Release(LockIntention.Readonly);
            }
        }

        /// <summary>
        /// Blocks until the lock is acquired then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public R Write<R>(CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            try
            {
                Acquire(LockIntention.Exclusive);
                return function();
            }
            finally
            {
                Release(LockIntention.Exclusive);
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R TryRead<R>(out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Readonly);
                if (wasLockObtained)
                {
                    return function();
                }
                return defaultValue;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Readonly);
                }
            }
        }


        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R TryWrite<R>(out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Exclusive);
                if (wasLockObtained)
                {
                    return function();
                }
                return defaultValue;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Exclusive);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R TryRead<R>(R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Readonly);
                if (wasLockObtained)
                {
                    return function();
                }
                return defaultValue;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Readonly);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R TryWrite<R>(R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Exclusive);
                if (wasLockObtained)
                {
                    return function();
                }
                return defaultValue;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Exclusive);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R TryRead<R>(out bool wasLockObtained, int timeoutMilliseconds, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Readonly, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function();
                }
                return defaultValue;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Readonly);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R TryWrite<R>(out bool wasLockObtained, int timeoutMilliseconds, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function();
                }
                return defaultValue;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Exclusive);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R TryRead<R>(int timeoutMilliseconds, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Readonly, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function();
                }
                return defaultValue;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Readonly);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R TryWrite<R>(int timeoutMilliseconds, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function();
                }
                return defaultValue;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Exclusive);
                }
            }
        }


        #endregion

        #region Read/Write/TryRead/TryWrite overloads (nullable)

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryReadNullable<R>(out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Readonly);
                if (wasLockObtained)
                {
                    return function();
                }
                return default;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Readonly);
                }
            }
        }


        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryWriteNullable<R>(out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Exclusive);
                if (wasLockObtained)
                {
                    return function();
                }
                return default;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Exclusive);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryReadNullable<R>(CriticalResourceDelegateWithNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Readonly);
                if (wasLockObtained)
                {
                    return function();
                }
                return default;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Readonly);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryWriteNullable<R>(CriticalResourceDelegateWithNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Exclusive);
                if (wasLockObtained)
                {
                    return function();
                }
                return default;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Exclusive);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryReadNullable<R>(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Readonly, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function();
                }
                return default;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Readonly);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryWriteNullable<R>(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function();
                }
                return default;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Exclusive);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryReadNullable<R>(int timeoutMilliseconds, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Readonly, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function();
                }
                return default;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Readonly);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryWriteNullable<R>(int timeoutMilliseconds, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function();
                }
                return default;
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release(LockIntention.Exclusive);
                }
            }
        }

        #endregion
    }
}
