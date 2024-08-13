namespace TestHarness
{
    internal class Program
    {
        static void Main()
        {
            //If you need to keep track of which thread owns each semaphore and/or critical sections then
            //  you can enable "ThreadOwnershipTracking" by calling ThreadOwnershipTracking.Enable(). Once this
            //  is enabled, it is enabled for the life of the application so this is only for debugging
            //  deadlock/race-condition tracking.
            //You can evaluate the ownership by evaluating
            //  the dictionary "ThreadOwnershipTracking.LockRegistration" or and instance of
            //  "PessimisticCriticalSection" or "PessimisticSemaphore" CurrentOwnerThread.
            //
            //ThreadOwnershipTracking.Enable();


            int iterations = 100;

            double testLockAllVariants = 0;
            double testPessimisticSemaphore = 0;
            double testOptimisticSemaphore = 0;
            double testPessimisticCriticalResource = 0;
            double testOptimisticCriticalResource = 0;

            for (int i = 0; i < 10; i++)
            {
                testLockAllVariants += (new TestLockAllVariants()).Execute();
                testPessimisticSemaphore += (new TestPessimisticSemaphore()).Execute();
                testOptimisticSemaphore += (new TestOptimisticSemaphore()).Execute();
                testPessimisticCriticalResource += (new TestPessimisticCriticalResource()).Execute();
                testOptimisticCriticalResource += (new TestOptimisticCriticalResource()).Execute();
            }

            Console.WriteLine($"Avg Durations after {iterations:n0} iterations:");
            Console.WriteLine($"               LockAllVariants: {(testLockAllVariants / iterations):n2}ms");
            Console.WriteLine($"          PessimisticSemaphore: {(testPessimisticSemaphore / iterations):n2}ms");
            Console.WriteLine($"           OptimisticSemaphore: {(testOptimisticSemaphore / iterations):n2}ms");
            Console.WriteLine($"   PessimisticCriticalResource: {(testPessimisticCriticalResource / iterations):n2}ms");
            Console.WriteLine($"    OptimisticCriticalResource: {(testOptimisticCriticalResource / iterations):n2}ms");
        }
    }
}
