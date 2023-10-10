namespace NTDLS.Semaphore
{
    /// <summary>
    /// Protects a variable from parallel / non-sequential thread access.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CriticalResource<T> : ICriticalResource where T : class, new()
    {
        #region Local Classes.

        private class CriticalCollection
        {
            public ICriticalResource Resource { get; set; }
            public bool IsLockHeld { get; set; } = false;

            public CriticalCollection(ICriticalResource resource)
            {
                Resource = resource;
            }
        }

        #endregion

        private readonly ICriticalResource _criticalSection;
        private readonly T _value;

        public delegate void CriticalResourceDelegateWithVoidResult(T obj);
        public delegate R? CriticalResourceDelegateWithNullableResultT<R>(T obj);
        public delegate R CriticalResourceDelegateWithNotNullableResultT<R>(T obj);

        /// <summary>
        /// Identifies the current thread that owns the lock.
        /// </summary>
        public Thread? OwnerThread { get; private set; }

        /// <summary>
        /// Initializes a new critical section that envelopes a variable.
        /// </summary>
        public CriticalResource()
        {
            _value = new T();
            _criticalSection = new CriticalSection();
        }

        /// <summary>
        /// Initializes a new critical section that envelopes a variable with a set value. This allows you to protect a variable that has a non-empty constructor.
        /// </summary>
        /// <param name="value"></param>
        public CriticalResource(T value)
        {
            _value = value;
            _criticalSection = new CriticalSection();
        }

        /// <summary>
        /// Envelopes a variable with a set value, using a predefined critical section.
        /// If other CriticalResources use the same criticalSection, they will require the exclusive lock of the shared criticalSection.
        /// </summary>
        /// <param name="criticalSection"></param>
        public CriticalResource(ICriticalResource criticalSection)
        {
            _value = new T();
            _criticalSection = criticalSection;
        }

        /// <summary>
        /// Envelopes a variable with a set value, using a predefined critical section. This allows you to protect a variable that has a non-empty constructor.
        /// If other CriticalResources use the same criticalSection, they will require the exclusive lock of the shared criticalSection.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="criticalSection"></param>
        public CriticalResource(T value, ICriticalResource criticalSection)
        {
            _value = value;
            _criticalSection = criticalSection;
        }

        /// <summary>
        /// Block until the lock is acquired then executes the delegate function and returns the non-nullable value from the delegate function.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="function"></param>
        /// <returns></returns>
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
        /// <param name="function"></param>
        /// <returns></returns>
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
        /// Attmepts to acquire the lock. If successful, executes the delegate function.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="wasLockObtained"></param>
        /// <param name="function"></param>
        public void TryUseAll(ICriticalResource[] resources, out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
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
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release();
                            }

                            wasLockObtained = false;
                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
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
        /// Attmepts to acquire the lock for the specified number of milliseconds. If successful, executes the delegate function.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="wasLockObtained"></param>
        /// <param name="function"></param>
        public void TryUseAll(ICriticalResource[] resources, int timeoutMilliseconds, out bool wasLockObtained, CriticalResourceDelegateWithVoidResult function)
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
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release();
                            }

                            wasLockObtained = false;
                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
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
        /// Attmepts to acquire the lock. If successful, executes the delegate function and returns the nullable value from the delegate function.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources"></param>
        /// <param name="wasLockObtained"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public R? TryUseAllNullable<R>(ICriticalResource[] resources, out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
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
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release();
                            }

                            wasLockObtained = false;
                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
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
        /// Attmepts to acquire the lock. If successful, executes the delegate function and returns the non-nullable value from the delegate function.
        /// Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources"></param>
        /// <param name="wasLockObtained"></param>
        /// <param name="defaultValue"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public R TryUseAll<R>(ICriticalResource[] resources, out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
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
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release();
                            }

                            wasLockObtained = false;
                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
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
        /// Attmepts to acquire the lock for the given number of milliseconds. If successful, executes the delegate function and
        /// returns the non-nullable value from the delegate function. Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="wasLockObtained"></param>
        /// <param name="defaultValue"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public R? TryUseAllNullable<R>(ICriticalResource[] resources, int timeoutMilliseconds, out bool wasLockObtained, R defaultValue, CriticalResourceDelegateWithNullableResultT<R> function)
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
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release();
                            }

                            wasLockObtained = false;
                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
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
        /// Attmepts to acquire the lock for the given number of milliseconds. If successful, executes the delegate function and
        /// returns the nullable value from the delegate function. Otherwise returns null.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="wasLockObtained"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public R? TryUseAllNullable<R>(ICriticalResource[] resources, int timeoutMilliseconds, out bool wasLockObtained, CriticalResourceDelegateWithNullableResultT<R> function)
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
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var lockObject in collection.Where(o => o.IsLockHeld))
                            {
                                lockObject.Resource.Release();
                            }

                            wasLockObtained = false;
                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in collection.Where(o => o.IsLockHeld))
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
        /// Attmepts to acquire the lock base lock as well as all supplied locks. If successful, executes the delegate function and
        /// returns the nullable value from the delegate function. Otherwise returns null.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public R? UseAllNullable<R>(ICriticalResource[] resources, CriticalResourceDelegateWithNullableResultT<R> function)
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
        /// <param name="resources"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public R UseAll<R>(ICriticalResource[] resources, CriticalResourceDelegateWithNotNullableResultT<R> function)
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
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="function"></param>
        public void UseAll(ICriticalResource[] resources, CriticalResourceDelegateWithVoidResult function)
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
        /// <param name="function"></param>
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
        /// <param name="wasLockObtained"></param>
        /// <param name="function"></param>
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
        /// <param name="wasLockObtained"></param>
        /// <param name="function"></param>
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
        /// <param name="wasLockObtained"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="function"></param>
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
        /// <param name="wasLockObtained"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="function"></param>
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
        /// <param name="wasLockObtained"></param>
        /// <param name="function"></param>
        /// <returns></returns>
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
        /// <param name="wasLockObtained"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="function"></param>
        /// <returns></returns>

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
        /// <param name="wasLockObtained"></param>
        /// <param name="defaultValue"></param>
        /// <param name="function"></param>
        /// <returns></returns>
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
        /// <param name="wasLockObtained"></param>
        /// <param name="defaultValue"></param>
        /// <param name="function"></param>
        /// <returns></returns>
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
        /// <param name="wasLockObtained"></param>
        /// <param name="defaultValue"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="function"></param>
        /// <returns></returns>
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
        /// <param name="wasLockObtained"></param>
        /// <param name="defaultValue"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="function"></param>
        /// <returns></returns>
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

        #region Internal interface functionality.

        /// <summary>
        /// Internal use only. Attempts to acquire the lock for a given number of milliseconds.
        /// </summary>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public bool TryAcquire(int timeoutMilliseconds) => _criticalSection.TryAcquire(timeoutMilliseconds);

        /// <summary>
        /// Internal use only. Attempts to acquire the lock.
        /// </summary>
        /// <returns></returns>
        public bool TryAcquire() => _criticalSection.TryAcquire();

        /// <summary>
        /// Internal use only. Blocks until the lock is acquired.
        /// </summary>
        public void Acquire() => _criticalSection.Acquire();

        /// <summary>
        /// Internal use only. Releases the previously acquired lock.
        /// </summary>
        public void Release() => _criticalSection.Release();

        #endregion
    }
}
