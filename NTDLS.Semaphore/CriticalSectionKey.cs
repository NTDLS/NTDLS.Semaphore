namespace NTDLS.Semaphore
{
    /// <summary>
    /// Disposable key for a critical section.
    /// </summary>
    public class CriticalSectionKey : IDisposable
    {
        public CriticalSection Owner { get; private set; }

        public bool IsLockHeld { get; private set; }

        internal CriticalSectionKey(CriticalSection owner, bool isLockHeld)
        {
            Owner = owner;
            IsLockHeld = isLockHeld;
        }

        public void Dispose()
        {
            Owner.Release();
        }
    }
}
