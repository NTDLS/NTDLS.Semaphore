namespace NTDLS.Semaphore
{
    /// <summary>
    /// Public interface that all lock types must implement.
    /// </summary>
    public interface ICriticalResource
    {
        /// <summary>
        /// Internal use only. Attempts to acquire the lock for a given number of milliseconds.
        /// </summary>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public bool TryAcquire(int timeoutMilliseconds);

        /// <summary>
        /// Internal use only. Attempts to acquire the lock.
        /// </summary>
        /// <returns></returns>
        public bool TryAcquire();

        /// <summary>
        /// Internal use only. Blocks until the lock is acquired.
        /// </summary>
        public void Acquire();

        /// <summary>
        /// Internal use only. Releases the previously acquired lock.
        /// </summary>
        public void Release();
    }
}
