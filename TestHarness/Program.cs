namespace TestHarness
{
    internal class Program
    {
        static void Main()
        {
            //If you need to keep track of which thread ownes each semephore and/or critical sections then
            //  you can enable "ThreadOwnershipTracking" by calling ThreadOwnershipTracking.Enable(). Once this
            //  is enabled, it is enabled for the life of the application so this is only for debugging
            //  deadlock/race-condition tracking.
            //You can evaluate the ownership by evaluating
            //  the dictonary "ThreadOwnershipTracking.LockRegistration" or and instance of
            //  "PessimisticCriticalSection" or "PessimisticSemaphore" CurrentOwnerThread.
            //
            //ThreadOwnershipTracking.Enable();


            int iterations = 100;

            double lockAllVariantsDuration = 0;
            double pessimisticCriticalDuration = 0;
            double optimisticCriticalDuration = 0;
            double pessimisticSemaphoreDuration = 0;
            double optimisticSemaphoreDuration = 0;

            for (int i = 0; i < 10; i++)
            {
                lockAllVariantsDuration += (new TestLockAllVariants()).Execute();
                pessimisticCriticalDuration += (new TestPessimisticSemaphore()).Execute();
                optimisticCriticalDuration += (new TestOptimisticSemaphore()).Execute();
                pessimisticSemaphoreDuration += (new TestPessimisticCriticalResource()).Execute();
                optimisticSemaphoreDuration += (new TestOptimisticCriticalResource()).Execute();
            }

            Console.WriteLine($"Avg Durations after {iterations:n0} iterations:");
            Console.WriteLine($"           Lock All Variants: {(lockAllVariantsDuration / iterations):n2}ms");
            Console.WriteLine($"Pessimistic Critical Section: {(pessimisticCriticalDuration / iterations):n2}ms");
            Console.WriteLine($" Optimistic Critical Section: {(optimisticCriticalDuration / iterations):n2}ms");
            Console.WriteLine($"       Pessimistic Semaphore: {(pessimisticSemaphoreDuration / iterations):n2}ms");
            Console.WriteLine($"        Optimistic Semaphore: {(optimisticSemaphoreDuration / iterations):n2}ms");
        }
    }
}
