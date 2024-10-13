using static NTDLS.Semaphore.OptimisticSemaphore;

namespace NTDLS.Semaphore
{
    /// <summary>
    /// Used for debugging when ThreadLockOwnershipTracking.Enabled is true.
    /// </summary>
    public class HeldLock
    {
        /// <summary>
        /// The original intention of the lock. Actual lock could be different for upgradable locks.
        /// </summary>
        public List<LockIntention> Intentions { get; set; } = new();

        /// <summary>
        /// The id of the thread that owns the lock.
        /// </summary>
        public int ManagedThreadId { get; set; }

        /// <summary>
        /// Creates an instance of HeldLock.
        /// </summary>
        public HeldLock(LockIntention intention, int managedThreadId)
        {
            Intentions.Add(intention);
            ManagedThreadId = managedThreadId;
        }
    }
}
