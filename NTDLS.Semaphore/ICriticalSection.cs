using static NTDLS.Semaphore.OptimisticSemaphore;

namespace NTDLS.Semaphore
{
    /// <summary>
    /// Both optimistic and pessimistic critical sections must inherit from this interface.
    /// </summary>
    public interface ICriticalSection
    {
        /// <summary>
        /// Internal use only. Blocks until the lock is acquired.
        /// </summary>
        public void Acquire();

        /// <summary>
        /// Internal use only. Releases the previously acquired lock.
        /// </summary>
        public void Release();

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
        /// Acquires a lock with and returns when it is held.
        /// </summary>
        /// <param name="intention"></param>
        public void Acquire(LockIntention intention);

        /// <summary>
        /// Tries to acquire a lock one single time and then gives up.
        /// </summary>
        /// <param name="intention"></param>
        /// <returns></returns>
        public bool TryAcquire(LockIntention intention);

        /// <summary>
        /// Tries to acquire a lock for a given time and then gives up.
        /// </summary>
        /// <param name="intention"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <returns></returns>
        public bool TryAcquire(LockIntention intention, int timeoutMilliseconds);

        /// <summary>
        /// Releases the lock held by the current thread.
        /// </summary>
        /// <param name="intention"></param>
        /// <exception cref="Exception"></exception>
        public void Release(LockIntention intention);
    }
}
