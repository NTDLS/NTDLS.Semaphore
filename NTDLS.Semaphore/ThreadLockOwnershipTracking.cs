namespace NTDLS.Semaphore
{
    /// <summary>
    /// Thread lock ownership tracking, used for debugging.
    /// </summary>
    public static class ThreadLockOwnershipTracking
    {
        private static bool _useThreadTracking = false;

        /// <summary>
        /// Denotes whether thread lock ownership tracking has been enabled.
        /// </summary>
        public static bool Enabled { get => _useThreadTracking; }

        /// <summary>
        /// Enabled thread lock ownership tracking for the life of the process.
        /// </summary>
        public static void Enable()
        {
            _useThreadTracking = true;
        }
    }
}
