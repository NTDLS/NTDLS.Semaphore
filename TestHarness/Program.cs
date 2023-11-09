namespace TestHarness
{
    internal class Program
    {
        static void Main()
        {
            int iterations = 100;

            double criticalSectionDuration = 0;
            double pessimisticSemaphoreDuration = 0;
            double optimisticSemaphoreDuration = 0;

            for (int i = 0; i < 10; i++)
            {
                criticalSectionDuration += (new TestCriticalSection()).Execute();
                pessimisticSemaphoreDuration += (new TestPessimisticSemaphore()).Execute();
                optimisticSemaphoreDuration += (new TestOptimisticSemaphore()).Execute();
            }

            Console.WriteLine($"Avg Durations after {iterations:n0} iterations:");
            Console.WriteLine($"     CriticalSection: {(criticalSectionDuration / iterations):n2}");
            Console.WriteLine($"PessimisticSemaphore: {(pessimisticSemaphoreDuration / iterations):n2}");
            Console.WriteLine($" OptimisticSemaphore: {(optimisticSemaphoreDuration / iterations):n2}");
        }
    }
}