namespace NTDLS.Semaphore
{
    /// <summary>
    /// Protects an area of code from parallel / non-sequential thread access.
    /// </summary>
    public class CriticalSection : ICriticalResource
    {
        public delegate void CriticalSectionDelegateWithParamAndNotNullableResultT<T>(T obj);
        public delegate void CriticalSectionDelegateWithVoidResult();
        public delegate T CriticalSectionDelegateRWithNotNullableResultT<T>();

        /// <summary>
        /// Identified the current thread that owns the lock.
        /// </summary>
        public Thread? OwnerThread { get; private set; }

        private CriticalSectionKey? _currentHeldKey;

        public bool TryAcquire(int timeout)
        {
            bool isLockHeld = Monitor.TryEnter(this, timeout);
            if (isLockHeld)
            {
                OwnerThread = Thread.CurrentThread;
            }
            return isLockHeld;
        }

        public bool TryAcquire()
        {
            bool isLockHeld = Monitor.TryEnter(this);
            if (isLockHeld)
            {
                OwnerThread = Thread.CurrentThread;
            }
            return isLockHeld;
        }

        public void Acquire()
        {
            Monitor.Enter(this);
            OwnerThread = Thread.CurrentThread;
        }

        public void Release()
        {
            Monitor.Exit(this);
            OwnerThread = Thread.CurrentThread;
        }

        /// <summary>
        /// Enters a critical section and returns a IDisposable object for which its disposal will release the lock.
        /// </summary>
        /// <returns></returns>
        public CriticalSectionKey Lock()
        {
            Acquire();
            _currentHeldKey = new CriticalSectionKey(this, true);
            return _currentHeldKey;
        }

        /// <summary>
        /// Attempts to enter a critical section for a given amount of time and returns a IDisposable object for which its disposal will release the lock.
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="wasLockAcquired"></param>
        /// <returns></returns>
        public CriticalSectionKey TryLock(int timeout, out bool wasLockAcquired)
        {
            wasLockAcquired = TryAcquire(timeout);
            _currentHeldKey = new CriticalSectionKey(this, wasLockAcquired);
            return _currentHeldKey;
        }

        /// <summary>
        /// Attempts to enter a critical section and returns a IDisposable object for which its disposal will release the lock.
        /// </summary>
        /// <param name="wasLockAcquired"></param>
        /// <returns></returns>
        public CriticalSectionKey TryLock(out bool wasLockAcquired)
        {
            wasLockAcquired = TryAcquire();
            _currentHeldKey = new CriticalSectionKey(this, wasLockAcquired);
            return _currentHeldKey;
        }

        public void Use(CriticalSectionDelegateWithVoidResult function)
        {
            using (Lock())
            {
                function();
            }
        }

        public T Use<T>(CriticalSectionDelegateRWithNotNullableResultT<T> function)
        {
            using (Lock())
            {
                return function();
            }
        }
    }
}
