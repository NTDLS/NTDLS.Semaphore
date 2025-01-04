namespace NTDLS.Semaphore
{
    /// <summary>
    /// Thread lock ownership tracking, used for debugging.
    /// Debug using PessimisticSemaphore.Ownership and OptimisticSemaphore.Ownership.
    /// </summary>
    public static class ThreadLockOwnershipTracking
    {
        private static bool _useThreadTracking = false;

        /// <summary>
        /// Denotes whether thread lock ownership tracking has been enabled.
        /// Debug using PessimisticSemaphore.Ownership and OptimisticSemaphore.Ownership.
        /// </summary>
        public static bool IsEnabled { get => _useThreadTracking; }

        /// <summary>
        /// Enabled thread lock ownership tracking for the life of the process.
        /// Debug using PessimisticSemaphore.Ownership and OptimisticSemaphore.Ownership.
        /// </summary>
        public static void Enable()
        {
            _useThreadTracking = true;
        }
    }
}
