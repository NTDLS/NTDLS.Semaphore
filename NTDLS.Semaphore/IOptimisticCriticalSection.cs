using static NTDLS.Semaphore.OptimisticCriticalSection;

namespace NTDLS.Semaphore
{
    /// <summary>
    /// The optimistic critical section that is at the core of the optimistic semaphore.
    /// Can be instantiated externally and chared across optimistic semaphores
    /// </summary>
    public interface IOptimisticCriticalSection
    {
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
