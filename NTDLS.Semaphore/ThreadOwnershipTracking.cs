namespace NTDLS.Semaphore
{
    /// <summary>
    /// Enables tracking of the thread ownership across all types of critical sections and semaphores.
    /// </summary>
    public static class ThreadOwnershipTracking
    {
        /// <summary>
        /// Denotes whether thread ownership tracking is enabled.
        /// </summary>
        public static bool IsEnabled { get; internal set; } = true;

        /// <summary>
        /// A dictionary of all threads that own locks. This can be handy when identifying deadlocks and race conditions.
        /// This is only tracked if enabled by a call to EnableGlobalLockRegistration().
        /// Once enabled, the tracking is attributed to all critical sections for the life of the application.
        /// </summary>
        public static Dictionary<string, ICriticalSection>? LockRegistration { get; internal set; }

        /// <summary>
        /// Enables tracking of the current thread that owns the lock and a maintains a dictionary of all threads
        /// that own locks. This can be handy when identifying deadlocks and race conditions. Once enabled, the
        /// tracking is attributed to all critical sections for the life of the application.
        /// This is should not be enabled in production/released code.
        /// </summary>
        public static void Enable()
        {
            IsEnabled = true;
            LockRegistration = new();
        }
    }
}
