namespace NTDLS.Semaphore
{
    /// <summary>
    /// The optimistic critical section that is at the core of the optimistic semaphore.
    /// Can be instantiated externally and chared across optimistic semaphores
    /// </summary>
    public class OptimisticCriticalSection : ICriticalSection
    {
        private readonly PessimisticSemaphore<List<HeldLock>> _locks = new();
        private readonly AutoResetEvent _locksModifiedEvent = new(false);

        #region Delegates.

        /// <summary>
        /// Delegate for executions that do not require a return value.
        /// </summary>
        public delegate void CriticalResourceDelegateWithVoidResult();

        /// <summary>
        /// Delegate for executions that require a nullable return value.
        /// </summary>
        /// <typeparam name="R">The type of the return value.</typeparam>
        /// <returns></returns>
        public delegate R? CriticalResourceDelegateWithNullableResultT<R>();

        /// <summary>
        /// Delegate for executions that require a non-nullable return value.
        /// </summary>
        /// <typeparam name="R">The type of the return value.</typeparam>
        /// <returns></returns>
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

        private class HeldLock
        {
            public int ThreadId { get; set; }
            public int ReferenceCount { get; set; }
            public LockIntention LockType { get; set; }

            public HeldLock(int threadId, LockIntention lockType)
            {
                ThreadId = threadId;
                LockType = lockType;
                ReferenceCount++;
            }
        }

        #endregion

        private void RegisterLock(LockIntention intention)
        {
            if (ThreadOwnershipTracking.LockRegistration != null)
            {
                lock (ThreadOwnershipTracking.LockRegistration)
                {
                    ThreadOwnershipTracking.LockRegistration.TryAdd($"Optimistic:CS:{intention}:{Environment.CurrentManagedThreadId}:{GetHashCode()}", this);
                }
            }
        }

        private void DeregisterLock(LockIntention intention)
        {
            if (ThreadOwnershipTracking.LockRegistration != null)
            {
                lock (ThreadOwnershipTracking.LockRegistration)
                {
                    ThreadOwnershipTracking.LockRegistration.Remove($"Optimistic:CS:{intention}:{Environment.CurrentManagedThreadId}:{GetHashCode()}");
                }
            }
        }

        #region Internal lock controls.

        /// <summary>
        /// Acquires an exclusive lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        void ICriticalSection.Acquire()
            => Acquire(LockIntention.Exclusive);

        /// <summary>
        /// Releases an exclusive lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        void ICriticalSection.Release()
            => Release(LockIntention.Exclusive);

        /// <summary>
        /// Acquires an exclusive lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        bool ICriticalSection.TryAcquire()
            => TryAcquire(LockIntention.Exclusive);

        /// <summary>
        /// Releases an exclusive lock.
        /// This implemented so that a PessimisticSemaphore can be locked via a call to OptimisticSemaphore...All().
        /// </summary>
        bool ICriticalSection.TryAcquire(int timeoutMilliseconds)
            => TryAcquire(LockIntention.Exclusive, timeoutMilliseconds);

        /// <summary>
        /// Acquires a lock with and returns when it is held.
        /// </summary>
        /// <param name="intention"></param>
        void ICriticalSection.Acquire(LockIntention intention)
            => Acquire(intention);

        /// <summary>
        /// Tries to acquire a lock one single time and then gives up.
        /// </summary>
        /// <param name="intention"></param>
        /// <returns></returns>
        bool ICriticalSection.TryAcquire(LockIntention intention)
            => TryAcquire(intention);

        /// <summary>
        /// Tries to acquire a lock for a given time and then gives up.
        /// </summary>
        /// <param name="intention"></param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <returns></returns>
        bool ICriticalSection.TryAcquire(LockIntention intention, int timeoutMilliseconds)
            => TryAcquire(intention, timeoutMilliseconds);

        /// <summary>
        /// Releases the lock held by the current thread.
        /// </summary>
        /// <param name="intention"></param>
        /// <exception cref="Exception"></exception>
        void ICriticalSection.Release(LockIntention intention)
            => Release(intention);

        /// <summary>
        /// Acquires a lock with and returns when it is held.
        /// </summary>
        /// <param name="intention"></param>
        private void Acquire(LockIntention intention)
            => TryAcquire(intention, -1);

        /// <summary>
        /// Tries to acquire a lock one single time and then gives up.
        /// </summary>
        /// <param name="intention"></param>
        /// <returns></returns>
        private bool TryAcquire(LockIntention intention)
            => TryAcquire(intention, 0);

        /// <summary>
        /// Tries to acquire a lock for a given time and then gives up.
        /// </summary>
        /// <param name="intention"></param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <returns></returns>
        private bool TryAcquire(LockIntention intention, int timeoutMilliseconds)
        {
            int threadId = Environment.CurrentManagedThreadId;

            DateTime? beginAttemptTime = timeoutMilliseconds > 0 ? DateTime.UtcNow : null;

            do
            {
                bool isLockHeld = _locks.Use((o) =>
                {
                    var locksHeldByThisThread = o.Where(l => l.ThreadId == threadId).ToList();

                    //Check to see if this thread already has a lock of the requested type.
                    var existingExactLockByThisThread = locksHeldByThisThread.SingleOrDefault(l => l.LockType == intention);
                    if (existingExactLockByThisThread != null)
                    {
                        //The thread already has the specified lock type, increment the reference count and return.
                        existingExactLockByThisThread.ReferenceCount++;
                        return true;
                    }

                    //This thread needs to acquire a new read-only lock.
                    if (intention == LockIntention.Readonly)
                    {
                        //Check to see if this thread already holds a more restrictive lock.
                        if (locksHeldByThisThread.Any(l => l.LockType == LockIntention.Exclusive))
                        {
                            //The current thread already has an exclusive lock, so we automatically grant a read-lock.
                            o.Add(new HeldLock(threadId, intention));
                            RegisterLock(intention);
                            _locksModifiedEvent.Set(); //Let any waiting lock acquisitions know they can try again.
                            return true;
                        }

                        //Check to make sure there are no existing exclusive locks.
                        if (o.Any(l => l.LockType == LockIntention.Exclusive) == false)
                        {
                            //This thread is seeking a read-only lock and there are no exclusive locks. Grant the read-lock.
                            o.Add(new HeldLock(threadId, intention));
                            RegisterLock(intention);
                            _locksModifiedEvent.Set(); //Let any waiting lock acquisitions know they can try again.
                            return true;
                        }
                    }

                    //This thread needs to acquire a new exclusive lock.
                    if (intention == LockIntention.Exclusive)
                    {
                        //Check to see if there are any existig locks (other than the ones held by the current thread).
                        //Read locks held by this thread DO NOT block new exclusive locks by the same thread.
                        if (o.Where(l => l.ThreadId != threadId).Any() == false)
                        {
                            //This thread is seeking a exclusive lock and there are no incompativle read-only locks. Grant the exclusive-lock.
                            o.Add(new HeldLock(threadId, intention));
                            RegisterLock(intention);
                            _locksModifiedEvent.Set(); //Let any waiting lock acquisitions know they can try again.
                            return true;
                        }
                    }

                    return false; //We were unable to get a non-conflicting lock.
                });

                if (isLockHeld)
                {
                    //If we were able to acquire a lock, return true.
                    return true;
                }

                _locksModifiedEvent.WaitOne(1);

                if (timeoutMilliseconds == 0)
                {
                    //timeoutMilliseconds == 0 means try only once.
                    break;
                }

            }
            while (beginAttemptTime == null || (DateTime.UtcNow - (DateTime)beginAttemptTime).TotalMilliseconds < timeoutMilliseconds);

            //Return false because we timed out while attempting to acquire the lock.
            return false;
        }

        /// <summary>
        /// Releases the lock held by the current thread.
        /// </summary>
        /// <param name="intention"></param>
        /// <exception cref="Exception"></exception>
        private void Release(LockIntention intention)
        {
            int threadId = Environment.CurrentManagedThreadId;

            _locks.Use((o) =>
            {
                var heldLock = o.Where(l => l.ThreadId == threadId && l.LockType == intention).SingleOrDefault();

                if (heldLock == null)
                {
                    throw new Exception("The thread lock reference was not found in the collection.");
                }

                heldLock.ReferenceCount--;

                if (heldLock.ReferenceCount == 0)
                {
                    //We have derefrenced all of this threads locks of the intended type. Remove the lock from the collection.
                    o.Remove(heldLock);
                    DeregisterLock(intention);
                    _locksModifiedEvent.Set(); //Let any waiting lock acquisitions know they can try again.
                }
                else if (heldLock.ReferenceCount < 0)
                {
                    throw new Exception("The thread lock reference count fell below zero.");
                }
            });
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
                return default(R);
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
                return default(R);
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
                return default(R);
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
                return default(R);
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
                return default(R);
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
                return default(R);
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
                return default(R);
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
                return default(R);
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
