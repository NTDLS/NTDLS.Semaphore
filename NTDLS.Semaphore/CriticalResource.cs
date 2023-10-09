namespace NTDLS.Semaphore
{
    /// <summary>
    /// Protects a variable from parallel / non-sequential thread access.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CriticalResource<T> : ICriticalResource where T : class, new()
    {
        #region Local Classes.

        private class LockObject
        {
            public ICriticalResource Resource { get; set; }
            public bool IsLockHeld { get; set; } = false;

            public LockObject(ICriticalResource resource)
            {
                Resource = resource;
            }
        }

        #endregion

        private readonly T _value;

        public delegate void CriticalResourceDelegateWithVoidResult(T obj);
        public delegate R? CriticalResourceDelegateWithNullableResultT<R>(T obj);
        public delegate R CriticalResourceDelegateWithNotNullableResultT<R>(T obj);

        /// <summary>
        /// Identifies the current thread that owns the lock.
        /// </summary>
        public Thread? OwnerThread { get; private set; }

        public CriticalResource()
        {
            _value = new T();
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
                Acquire(); ;
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
        /// <param name="IsLockHeld"></param>
        /// <param name="function"></param>
        public void TryUseAll(ICriticalResource[] resources, out bool IsLockHeld, CriticalResourceDelegateWithVoidResult function)
        {
            var lockObjects = new LockObject[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < lockObjects.Length; i++)
                    {
                        lockObjects[i] = new(resources[i]);
                        lockObjects[i].IsLockHeld = lockObjects[i].Resource.TryAcquire();

                        if (lockObjects[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var _lockObject in lockObjects.Where(o => o.IsLockHeld))
                            {
                                lockObjects[i].Resource.Release();
                                _lockObject.IsLockHeld = false;
                            }

                            IsLockHeld = false;

                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in lockObjects.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    IsLockHeld = true;

                    return;
                }
                finally
                {
                    Release();
                }
            }

            IsLockHeld = false;
        }

        /// <summary>
        /// Attmepts to acquire the lock for the specified number of milliseconds. If successful, executes the delegate function.
        /// </summary>
        /// <param name="resources"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="IsLockHeld"></param>
        /// <param name="function"></param>
        public void TryUseAll(ICriticalResource[] resources, int timeoutMilliseconds, out bool IsLockHeld, CriticalResourceDelegateWithVoidResult function)
        {
            var lockObjects = new LockObject[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < lockObjects.Length; i++)
                    {
                        lockObjects[i] = new(resources[i]);
                        lockObjects[i].IsLockHeld = lockObjects[i].Resource.TryAcquire(timeoutMilliseconds);

                        if (lockObjects[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var _lockObject in lockObjects.Where(o => o.IsLockHeld))
                            {
                                lockObjects[i].Resource.Release();
                                _lockObject.IsLockHeld = false;
                            }
                            IsLockHeld = false;

                            return;
                        }
                    }

                    function(_value);

                    foreach (var lockObject in lockObjects.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    IsLockHeld = true;

                    return;
                }
                finally
                {
                    Release();
                }
            }

            IsLockHeld = false;
        }

        /// <summary>
        /// Attmepts to acquire the lock. If successful, executes the delegate function and returns the nullable value from the delegate function.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources"></param>
        /// <param name="IsLockHeld"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public R? TryUseAll<R>(ICriticalResource[] resources, out bool IsLockHeld, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var lockObjects = new LockObject[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < lockObjects.Length; i++)
                    {
                        lockObjects[i] = new(resources[i]);
                        lockObjects[i].IsLockHeld = lockObjects[i].Resource.TryAcquire();

                        if (lockObjects[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var _lockObject in lockObjects.Where(o => o.IsLockHeld))
                            {
                                lockObjects[i].Resource.Release();
                                _lockObject.IsLockHeld = false;
                            }

                            IsLockHeld = false;

                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in lockObjects.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    IsLockHeld = true;

                    return result;
                }
                finally
                {
                    Release();
                }
            }

            IsLockHeld = false;

            return default;
        }

        /// <summary>
        /// Attmepts to acquire the lock. If successful, executes the delegate function and returns the non-nullable value from the delegate function.
        /// Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources"></param>
        /// <param name="IsLockHeld"></param>
        /// <param name="defaultValue"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public R TryUseAll<R>(ICriticalResource[] resources, out bool IsLockHeld, R defaultValue, CriticalResourceDelegateWithNotNullableResultT<R> function)
        {
            var lockObjects = new LockObject[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < lockObjects.Length; i++)
                    {
                        lockObjects[i] = new(resources[i]);
                        lockObjects[i].IsLockHeld = lockObjects[i].Resource.TryAcquire();

                        if (lockObjects[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var _lockObject in lockObjects.Where(o => o.IsLockHeld))
                            {
                                lockObjects[i].Resource.Release();
                                _lockObject.IsLockHeld = false;
                            }

                            IsLockHeld = false;

                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in lockObjects.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    IsLockHeld = true;

                    return result;
                }
                finally
                {
                    Release();
                }
            }

            IsLockHeld = false;

            return defaultValue;
        }

        /// <summary>
        /// Attmepts to acquire the lock for the given number of milliseconds. If successful, executes the delegate function and
        /// returns the non-nullable value from the delegate function. Otherwise returns the given default value.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="IsLockHeld"></param>
        /// <param name="defaultValue"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public R? TryUseAll<R>(ICriticalResource[] resources, int timeoutMilliseconds, out bool IsLockHeld, R defaultValue, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var lockObjects = new LockObject[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < lockObjects.Length; i++)
                    {
                        lockObjects[i] = new(resources[i]);
                        lockObjects[i].IsLockHeld = lockObjects[i].Resource.TryAcquire(timeoutMilliseconds);

                        if (lockObjects[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var _lockObject in lockObjects.Where(o => o.IsLockHeld))
                            {
                                lockObjects[i].Resource.Release();
                                _lockObject.IsLockHeld = false;
                            }
                            IsLockHeld = false;

                            return defaultValue;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in lockObjects.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    IsLockHeld = true;

                    return result;
                }
                finally
                {
                    Release();
                }
            }

            IsLockHeld = false;

            return defaultValue;
        }

        /// <summary>
        /// Attmepts to acquire the lock for the given number of milliseconds. If successful, executes the delegate function and
        /// returns the nullable value from the delegate function. Otherwise returns null.
        /// </summary>
        /// <typeparam name="R"></typeparam>
        /// <param name="resources"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="IsLockHeld"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public R? TryUseAll<R>(ICriticalResource[] resources, int timeoutMilliseconds, out bool IsLockHeld, CriticalResourceDelegateWithNullableResultT<R> function)
        {
            var lockObjects = new LockObject[resources.Length];

            if (TryAcquire())
            {
                try
                {
                    for (int i = 0; i < lockObjects.Length; i++)
                    {
                        lockObjects[i] = new(resources[i]);
                        lockObjects[i].IsLockHeld = lockObjects[i].Resource.TryAcquire(timeoutMilliseconds);

                        if (lockObjects[i].IsLockHeld == false)
                        {
                            //We didnt get one of the locks, free the ones we did get and bailout.
                            foreach (var _lockObject in lockObjects.Where(o => o.IsLockHeld))
                            {
                                lockObjects[i].Resource.Release();
                                _lockObject.IsLockHeld = false;
                            }
                            IsLockHeld = false;

                            return default;
                        }
                    }

                    var result = function(_value);

                    foreach (var lockObject in lockObjects.Where(o => o.IsLockHeld))
                    {
                        lockObject.Resource.Release();
                    }
                    IsLockHeld = true;

                    return result;
                }
                finally
                {
                    Release();
                }
            }

            IsLockHeld = false;

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
                OwnerThread = Thread.CurrentThread;
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
                OwnerThread = Thread.CurrentThread;
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
            OwnerThread = Thread.CurrentThread;
        }

        /// <summary>
        /// Internal use only. Releases the previously acquired lock.
        /// </summary>
        public void Release()
        {
            OwnerThread = null;
            Monitor.Exit(this);
        }

        #endregion
    }
}
