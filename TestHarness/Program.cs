namespace TestHarness
{
    internal class Program
    {
        static void Main()
        {
            int iterations = 100;

            double pessimisticCriticalDuration = 0;
            double optimisticCriticalDuration = 0;
            double pessimisticSemaphoreDuration = 0;
            double optimisticSemaphoreDuration = 0;

            for (int i = 0; i < 10; i++)
            {
                pessimisticCriticalDuration += (new TestPessimisticCriticalSection()).Execute();
                optimisticCriticalDuration += (new TestOptimisticCriticalSection()).Execute();
                pessimisticSemaphoreDuration += (new TestPessimisticSemaphore()).Execute();
                optimisticSemaphoreDuration += (new TestOptimisticSemaphore()).Execute();
            }

            Console.WriteLine($"Avg Durations after {iterations:n0} iterations:");
            Console.WriteLine($"Pessimistic Critical Section: {(pessimisticCriticalDuration / iterations):n2}");
            Console.WriteLine($" Optimistic Critical Section: {(optimisticCriticalDuration / iterations):n2}");
            Console.WriteLine($"       Pessimistic Semaphore: {(pessimisticSemaphoreDuration / iterations):n2}");
            Console.WriteLine($"        Optimistic Semaphore: {(optimisticSemaphoreDuration / iterations):n2}");
        }
    }
}