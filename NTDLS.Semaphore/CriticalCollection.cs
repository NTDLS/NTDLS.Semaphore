namespace NTDLS.Semaphore
{
    internal class CriticalCollection
    {
        public ICriticalSection Resource { get; set; }
        public bool IsLockHeld { get; set; } = false;

        public CriticalCollection(ICriticalSection resource)
        {
            Resource = resource;
        }
    }
}
