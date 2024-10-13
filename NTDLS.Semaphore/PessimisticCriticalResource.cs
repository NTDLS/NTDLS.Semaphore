namespace NTDLS.Semaphore
{
    /// <summary>
    /// Protects a variable from parallel / non-sequential thread access by always acquiring an exclusive lock on the resource. 
    /// </summary>
    /// <typeparam name="T">The type of the resource that will be instantiated and protected.</typeparam>
    public class PessimisticCriticalResource<T> : ICriticalSection where T : class, new()
    {
        #region Local Classes.

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

        /// <summary>
        /// Identifies the current thread that owns the lock.
        /// </summary>
        public Thread? CurrentOwnerThread { get; private set; }
        private readonly ICriticalSection _criticalSection;
        private readonly T _value;

        #region Delegates.

        /// <summary>
        /// Used by the constructor to allow for advanced initialization of the enclosed value.
        /// </summary>
        public delegate T InitializationCallback();

        /// <summary>
        /// Delegate for executions that do not require a return value.
        /// </summary>
        /// <param name="obj">The variable that is being protected. It can be safely modified here.</param>
        public delegate void CriticalResourceDelegateWithVoidResult(T obj);

        /// <summary>
        /// Delegate for executions that require a nullable return value.
        /// </summary>
        /// <typeparam name="R">The type of the return value.</typeparam>
        /// <param name="obj">The variable that is being protected. It can be safely modified here.</param>
        public delegate R? CriticalResourceDelegateWithNullableResultT<R>(T obj);

        /// <summary>
        /// Delegate for executions that require a non-nullable return value.
        /// </summary>
        /// <typeparam name="R">The type of the return value.</typeparam>
        /// <param name="obj">The variable that is being protected. It can be safely modified here.</param>
        public delegate R CriticalResourceDelegateWithNotNullableResultT<R>(T obj);

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

        #endregion

        /// <summary>
        /// Initializes a new pessimistic semaphore that envelopes a variable.
        /// </summary>
        public PessimisticCriticalResource(InitializationCallback initializationCallback)
        {
            _value = initializationCallback();
            _criticalSection = new PessimisticSemaphore();
        }

        /// <summary>
        /// Envelopes a variable with a set value, using a predefined critical section. This allows you to protect a variable that has a non-empty constructor.
        /// If other pessimistic semaphores use the same critical section, they will require the exclusive lock of the shared critical section.
        /// </summary>
        public PessimisticCriticalResource(InitializationCallback initializationCallback, ICriticalSection criticalSection)
        {
            _value = initializationCallback();
            _criticalSection = criticalSection;
        }

        /// <summary>
        /// Initializes a new pessimistic semaphore that envelopes a variable.
        /// </summary>
        public PessimisticCriticalResource()
        {
            _value = new T();
            _criticalSection = new PessimisticSemaphore();
        }

        /// <summary>
        /// Initializes a new pessimistic semaphore that envelopes a variable with a set value. This allows you to protect a variable that has a non-empty constructor.
        /// </summary>
        /// <param name="value"></param>
        public PessimisticCriticalResource(T value)
        {
            _value = value;
            _criticalSection = new PessimisticSemaphore();
        }

        /// <summary>
        /// Envelopes a variable using a predefined critical section.
        /// If other pessimistic semaphores use the same critical section, they will require the exclusive lock of the shared critical section.
        /// </summary>
        public PessimisticCriticalResource(ICriticalSection criticalSection)
        {
            _value = new T();
            _criticalSection = criticalSection;
        }

        /// <summary>
        /// Envelopes a variable with a set value, using a predefined critical section. This allows you to protect a variable that has a non-empty constructor.
        /// If other pessimistic semaphores use the same critical section, they will require the exclusive lock of the shared critical section.
        /// </summary>
        public PessimisticCriticalResource(T value, ICriticalSection criticalSection)
        {
            _value = value;
            _criticalSection = criticalSection;
        }

        /// <summary>
        /// Block until the lock is acquired then executes the delegate function and returns the non-nullable value from the delegate function.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public R Use<R>(CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            try
            {
                Acquire();
                return function(_value);
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Block until the lock is acquired then executes the delegate function and return the nullable value from the delegate function.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public R? UseNullable<R>(CriticalResourceDelegateWithNullableResultT<R> function)
        {
            try
            {
                Acquire();
                return function(_value);
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Attempts to acquire the lock. If successful, executes the delegate function.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns>Returns true if the lock was obtained.</returns>
        public bool TryUseAll(ICriticalSection[] resources, CriticalResourceDelegateWithVoidResult function)
        {
            TryUseAll(resources, out bool wasLockObtained, function);
            return wasLockObtained;
        }

        /// <summary>
        /// Attempts to acquire the lock. If successful, executes the delegate function.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        /// <returns>Returns true if the lock was obtained.</returns>
        public bool TryUseAll(ICriticalSection[] resources, int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            TryUseAll(resources, timeoutMilliseconds, out bool wasLockObtained, function);
            return wasLockObtained;
        }

        /// <summary>
        /// Attempts to acquire the lock. If successful, executes the delegate function.
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryUseAll(ICriticalSection[] resources, out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire();

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release();
                            }

                            wasLockObtained = false;
                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    wasLockObtained = true;

                    return;
                }
                finally
                {
                    Release();
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
        public void TryUseAll(ICriticalSection[] resources, int timeoutMilliseconds, out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(timeoutMilliseconds);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release();
                            }

                            wasLockObtained = false;
                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    wasLockObtained = true;

                    return;
                }
                finally
                {
                    Release();
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
        public R? TryUseAllNullable<R>(ICriticalSection[] resources, out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire();

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release();
                            }

                            wasLockObtained = false;
                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    wasLockObtained = true;

                    return result;
                }
                finally
                {
                    Release();
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
        public R TryUseAll<R>(ICriticalSection[] resources, out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire();

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release();
                            }

                            wasLockObtained = false;
                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    wasLockObtained = true;

                    return result;
                }
                finally
                {
                    Release();
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
        public R? TryUseAllNullable<R>(ICriticalSection[] resources, int timeoutMilliseconds, out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(timeoutMilliseconds);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release();
                            }

                            wasLockObtained = false;
                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    wasLockObtained = true;

                    return result;
                }
                finally
                {
                    Release();
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
        public R? TryUseAllNullable<R>(ICriticalSection[] resources, int timeoutMilliseconds, out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var collection = new CriticalCollection[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < collection.Length; i++)
                    {
                        collection[i] = new(resources[i]);
                        collection[i].IsLockHeld = collection[i].Resource.TryAcquire(timeoutMilliseconds);

                        if (collection[i].IsLockHeld == false)
                        {
                            //We didn't get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                            {
                                lockObject.Resource.Release();
                            }

                            wasLockObtained = false;
                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o != null && o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    wasLockObtained = true;

                    return result;
                }
                finally
                {
                    Release();
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
        public R? UseAllNullable<R>(ICriticalSection[] resources, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            Acquire();

            try
            {
                foreach (var res in resources)
                {
                    res.Acquire();
                }

                var result = function(_value);

                foreach (var res in resources)
                {
                    res.Release();
                }

                return result;
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Blocks until the base lock as well as all supplied locks are acquired then executes the delegate function and
        /// returns the non-nullable value from the delegate function.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public R UseAll<R>(ICriticalSection[] resources, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            Acquire();

            try
            {
                foreach (var res in resources)
                {
                    res.Acquire();
                }

                var result = function(_value);

                foreach (var res in resources)
                {
                    res.Release();
                }

                return result;
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Blocks until the base lock as well as all supplied locks are acquired then executes the delegate function.
        /// Due to the way that the locks are obtained, UseAll() can lead to lock interleaving which will cause a deadlock if called in parallel with other calls to UseAll(), ReadAll() or WriteAll().
        /// </summary>
        /// <param name="resources">The array of other locks that must be obtained.</param>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public void UseAll(ICriticalSection[] resources, CriticalResourceDelegateWithVoidResult function)
        {
            Acquire();

            try
            {
                foreach (var res in resources)
                {
                    res.Acquire();
                }

                function(_value);

                foreach (var res in resources)
                {
                    res.Release();
                }
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Blocks until the lock is acquired then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute when the lock is acquired.</param>
        public void Use(CriticalResourceDelegateWithVoidResult function)
        {
            try
            {
                Acquire();
                function(_value);
            }
            finally
            {
                Release();
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryUse(out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire();
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
                    Release();
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function.
        /// </summary>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryUse(CriticalResourceDelegateWithVoidResult function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire();
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
                    Release();
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryUse(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(timeoutMilliseconds);
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
                    Release();
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock for the given number of milliseconds, if successful then executes the delegate function.
        /// </summary>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public void TryUse(int timeoutMilliseconds, CriticalResourceDelegateWithVoidResult function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(timeoutMilliseconds);
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
                    Release();
                }
            }
        }

        /// <summary>
        /// Attempts to acquire the lock, if successful then executes the delegate function and returns the nullable delegate value.
        /// Otherwise returns null.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryUseNullable<R>(out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire();
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release();
                }
            }
            return default;
        }

        /// <summary>
        /// Attempts to acquire the lock for a given number of milliseconds, if successful then executes the delegate function and returns the nullable delegate value.
        /// Otherwise returns null.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="timeoutMilliseconds">The amount of time to attempt to acquire a lock. -1 = infinite, 0 = try one time, >0 = duration.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R? TryUseNullable<R>(out bool wasLockObtained, int timeoutMilliseconds, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release();
                }
            }

            return default;
        }

        /// <summary>
        /// Attempts to acquire the lock. If successful then executes the delegate function and returns the non-nullable delegate value.
        /// Otherwise returns the supplied default value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="wasLockObtained">Output boolean that denotes whether the lock was obtained.</param>
        /// <param name="defaultValue">The value to obtain if the lock could not be acquired.</param>
        /// <param name="function">The delegate function to execute if the lock is acquired.</param>
        public R TryUse<R>(out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire();
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release();
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
        public R TryUse<R>(R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire();
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release();
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
        public R TryUse<R>(out bool wasLockObtained, R defaultValue, int timeoutMilliseconds, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release();
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
        public R TryUse<R>(R defaultValue, int timeoutMilliseconds, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            bool wasLockObtained = false;
            try
            {
                wasLockObtained = TryAcquire(timeoutMilliseconds);
                if (wasLockObtained)
                {
                    return function(_value);
                }
            }
            finally
            {
                if (wasLockObtained)
                {
                    Release();
                }
            }

            return defaultValue;
        }
    }
}
