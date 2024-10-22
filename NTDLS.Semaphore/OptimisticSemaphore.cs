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

        /// <summary>
        /// Thread lock ownership tracking, used for debugging when ThreadLockOwnershipTracking.Enabled is true.
        /// </summary>
        public Dictionary<int, HeldLock>? Ownership { get; private set; }

        /// <summary>
        /// Creates a new instance of OptimisticSemaphore.
        /// </summary>
        public OptimisticSemaphore()
        {
            if (ThreadLockOwnershipTracking.Enabled)
            {
                Ownership = new();
            }
        }

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
            /// The code intends to modify the resource (such as adding/removing an item from a collection).
            /// </summary>
            Exclusive,
            /// <summary>
            /// The code intends only to read the resource (such as iterate through a a collection).
            /// </summary>
            Readonly,

            /// <summary>
            /// The code intends only to read the resource (such as iterate through a a collection), but can later be upgraded to an exclusive lock.
            /// </summary>
            UpgradableRead
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
        {
            if (intention == LockIntention.Readonly)
            {
                _readerWriterLockSlim.EnterReadLock();
            }
            else if (intention == LockIntention.UpgradableRead)
            {
                _readerWriterLockSlim.EnterUpgradeableReadLock();
            }
            else if (intention == LockIntention.Exclusive)
            {
                _readerWriterLockSlim.EnterWriteLock();
            }

            if (Ownership != null)
            {
                lock (Ownership)
                {
                    var managedThreadId = Environment.CurrentManagedThreadId;

                    if (Ownership.TryGetValue(managedThreadId, out var heldLock))
                    {
                        heldLock.Intentions.Add(intention);
                    }
                    else
                    {
                        Ownership.Add(managedThreadId, new HeldLock(intention, Environment.CurrentManagedThreadId));
                    }
                }
            }
        }

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
            bool acquired = false;

            if (intention == LockIntention.Readonly)
            {
                acquired = _readerWriterLockSlim.TryEnterReadLock(timeoutMilliseconds);
            }
            else if (intention == LockIntention.UpgradableRead)
            {
                acquired = _readerWriterLockSlim.TryEnterUpgradeableReadLock(timeoutMilliseconds);
            }
            else if (intention == LockIntention.Exclusive)
            {
                acquired = _readerWriterLockSlim.TryEnterWriteLock(timeoutMilliseconds);
            }
            else
            {
                throw new Exception("The lock intention type is not implemented");
            }

            if (acquired && Ownership != null)
            {
                lock (Ownership)
                {
                    var managedThreadId = Environment.CurrentManagedThreadId;

                    if (Ownership.TryGetValue(managedThreadId, out var heldLock))
                    {
                        heldLock.Intentions.Add(intention);
                    }
                    else
                    {
                        Ownership.Add(managedThreadId, new HeldLock(intention, Environment.CurrentManagedThreadId));
                    }
                }
            }

            return acquired;
        }

        /// <summary>
        /// Releases the lock held by the current thread.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Release(LockIntention intention)
        {
            if (intention == LockIntention.Readonly)
            {
                _readerWriterLockSlim.ExitReadLock();
            }
            else if (intention == LockIntention.UpgradableRead)
            {
                _readerWriterLockSlim.ExitUpgradeableReadLock();
            }
            else if (intention == LockIntention.Exclusive)
            {
                _readerWriterLockSlim.ExitWriteLock();
            }

            if (Ownership != null)
            {
                lock (Ownership)
                {
                    var managedThreadId = Environment.CurrentManagedThreadId;

                    if (Ownership.TryGetValue(managedThreadId, out var heldLock))
                    {
                        heldLock.Intentions.Remove(intention);

                        if (heldLock.Intentions.Count == 0)
                        {
                            Ownership.Remove(managedThreadId);
                        }
                    }
                    else
                    {
                        throw new Exception("Attempted to release non-owned lock.");
                    }
                }
            }
        }

        #endregion

        #region Exposed ReaderWriterLockSlim properties.

        /// <summary>
        /// Gets the total number of unique threads that have entered the lock in read mode.
        /// </summary>
        public int CurrentReadCount
            => _readerWriterLockSlim.CurrentReadCount;

        /// <summary>
        /// Gets a value that indicates whether the current thread has entered the lock in read mode.
        /// </summary>
        public bool IsReadLockHeld
            => _readerWriterLockSlim.IsReadLockHeld;

        /// <summary>
        /// Gets a value that indicates whether the current thread has entered the lock in upgradeable mode.
        /// </summary>
        public bool IsUpgradeableReadLockHeld
            => _readerWriterLockSlim.IsUpgradeableReadLockHeld;

        /// <summary>
        /// Gets a value that indicates whether the current thread has entered the lock in write mode.
        /// </summary>
        public bool IsWriteLockHeld
            => _readerWriterLockSlim.IsWriteLockHeld;

        /// <summary>
        /// Gets the number of times the current thread has entered the lock in read mode, as an indication of recursion.
        /// </summary>
        public int RecursiveReadCount
            => _readerWriterLockSlim.RecursiveReadCount;

        /// <summary>
        /// Gets the number of times the current thread has entered the lock in upgradeable mode, as an indication of recursion.
        /// </summary>
        public int RecursiveUpgradeCount
            => _readerWriterLockSlim.RecursiveUpgradeCount;

        /// <summary>
        /// Gets the number of times the current thread has entered the lock in write mode, as an indication of recursion.
        /// </summary>
        public int RecursiveWriteCount
            => _readerWriterLockSlim.RecursiveWriteCount;

        /// <summary>
        /// Gets the total number of threads that are waiting to enter the lock in read mode.
        /// </summary>
        public int WaitingReadCount
            => _readerWriterLockSlim.WaitingReadCount;

        /// <summary>
        /// Gets the total number of threads that are waiting to enter the lock in upgradeable mode.
        /// </summary>
        public int WaitingUpgradeCount
            => _readerWriterLockSlim.WaitingUpgradeCount;

        /// <summary>
        /// Gets the total number of threads that are waiting to enter the lock in write mode.
        /// </summary>
        public int WaitingWriteCount
            => _readerWriterLockSlim.WaitingWriteCount;

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
        /// Blocks until the lock is acquired then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public R? ReadNullable<R>(CriticalResourceDelegateWithNullableResultT<R> function)
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
        public R? WriteNullable<R>(CriticalResourceDelegateWithNullableResultT<R> function)
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

        #region UpgradableRead/TryUpgradableRead overloads.

        /// <summary>
        /// Blocks until the read-only write upgradable lock is acquired then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public void UpgradableRead(CriticalResourceDelegateWithVoidResult function)
        {
            try
            {
                Acquire(LockIntention.UpgradableRead);
                function();
            }
            finally
            {
                Release(LockIntention.UpgradableRead);
            }
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryUpgradableRead(out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.UpgradableRead);
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
        /// Attempts to acquire the read-only write upgradable lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryUpgradableRead(CriticalResourceDelegateWithVoidResult function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.UpgradableRead);
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
        /// Attempts to acquire the read-only write upgradable lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryUpgradableRead(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.UpgradableRead, timeoutMilliseconds);
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
        /// Attempts to acquire the read-only write upgradable lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryUpgradableRead(int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.UpgradableRead, timeoutMilliseconds);
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

        #endregion

        #region UpgradableRead/TryUpgradableRead overloads (with returns).

        /// <summary>
        /// Blocks until the read-only write upgradable lock is acquired then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public R UpgradableRead<R>(CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            try
            {
                Acquire(LockIntention.UpgradableRead);
                return function();
            }
            finally
            {
                Release(LockIntention.UpgradableRead);
            }
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R TryUpgradableRead<R>(out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.UpgradableRead);
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
                    Release(LockIntention.UpgradableRead);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R TryUpgradableRead<R>(R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.UpgradableRead);
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
                    Release(LockIntention.UpgradableRead);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R TryUpgradableRead<R>(out bool wasLockObtained, int timeoutMilliseconds, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.UpgradableRead, timeoutMilliseconds);
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
                    Release(LockIntention.UpgradableRead);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R TryUpgradableRead<R>(int timeoutMilliseconds, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.UpgradableRead, timeoutMilliseconds);
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
                    Release(LockIntention.UpgradableRead);
                }
            }
        }

        #endregion

        #region UpgradableRead/TryUpgradableRead overloads (nullable).

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryUpgradableReadNullable<R>(out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.UpgradableRead);
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
                    Release(LockIntention.UpgradableRead);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryUpgradableReadNullable<R>(CriticalResourceDelegateWithNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.UpgradableRead);
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
                    Release(LockIntention.UpgradableRead);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryUpgradableReadNullable<R>(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.UpgradableRead, timeoutMilliseconds);
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
                    Release(LockIntention.UpgradableRead);
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the read-only write upgradable lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryUpgradableReadNullable<R>(int timeoutMilliseconds, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(LockIntention.UpgradableRead, timeoutMilliseconds);
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
                    Release(LockIntention.UpgradableRead);
                }
            }
        }

        #endregion

        #region TryReadAll/TryWriteAll.

        /// <summary>
        /// Attempts to acquire the lock. If successful, executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryReadAll(ICriticalSection[] resources, CriticalResourceDelegateWithVoidResult function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (TryAcquire(LockIntention.Readonly))
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

                            return false;
                        }
                    }

                    function();

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Readonly);
                    }

                    return true;
                }
                finally
                {
                    Release(LockIntention.Readonly);
                }
            }

            return false;
        }

        /// <summary>
        /// Acquire the lock, executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadAll(ICriticalSection[] resources, CriticalResourceDelegateWithVoidResult function)
        {
            while (!TryReadAll(resources, function))
            {
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Attempts to acquire the lock. If successful, executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryWriteAll(ICriticalSection[] resources, CriticalResourceDelegateWithVoidResult function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (TryAcquire(LockIntention.Exclusive))
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

                            return false;
                        }
                    }

                    function();

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                    {
                        lockObject.Resource.Release(LockIntention.Exclusive);
                    }

                    return true;
                }
                finally
                {
                    Release(LockIntention.Exclusive);
                }
            }

            return false;
        }

        /// <summary>
        /// Acquire the lock, executes the delegate function.
        /// The delegate SHOULD NOT modify the passed value, otherwise corruption can occur. For modifications, call Write() or TryWrite() instead.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteAll(ICriticalSection[] resources, CriticalResourceDelegateWithVoidResult function)
        {
            while (!TryWriteAll(resources, function))
            {
                Thread.Sleep(1);
            }
        }

        #endregion
    }
}
