using NTDLS.Semaphore;

namespace TestHarness
{
    internal class TestLockAllVariants
    {
        private readonly PessimisticSemaphore<List<string>> _pessimisticSemaphore = new();
        private readonly OptimisticSemaphore<List<string>> _optimisticSemaphore = new();
        private readonly OptimisticCriticalSection _optimisticCriticalSection = new();
        private readonly PessimisticCriticalSection _pessimisticCriticalSection = new();

        private readonly List<Thread> _threads = new();

        private const int _threadsToCreate = 10;
        private const int _objectsPerIteration = 100;
        private volatile int _totalAllLockCount = 0;

        public double Execute()
        {
            Console.WriteLine("[TestLockAllVariants] {");
            DateTime startTime = DateTime.UtcNow;

            // Due to the way that the locks are obtained, ReadAll/WriteAll/UseAll can lead to lock interleaving
            //  which will cause a deadlock if called in parallel with other calls to UseAll(), ReadAll() or WriteAll().
            // For these reasons, we will test them here and not in parallel.
            _optimisticSemaphore.ReadAll(
                new ICriticalSection[] { _optimisticSemaphore, _pessimisticSemaphore, _optimisticCriticalSection, _pessimisticCriticalSection }, (o) =>
                {
                    _totalAllLockCount++;
                    //Console.WriteLine("[optimisticSemaphore.ReadAll] All locks obtained.");
                });

            _optimisticSemaphore.WriteAll(
                new ICriticalSection[] { _optimisticSemaphore, _pessimisticSemaphore, _optimisticCriticalSection, _pessimisticCriticalSection }, (o) =>
                {
                    _totalAllLockCount++;
                    //Console.WriteLine("[optimisticSemaphore.WriteAll] All locks obtained.");
                });
            _pessimisticSemaphore.UseAll(
                new ICriticalSection[] { _optimisticSemaphore, _pessimisticSemaphore, _optimisticCriticalSection, _pessimisticCriticalSection }, (o) =>
                {
                    _totalAllLockCount++;
                    //Console.WriteLine("[pessimisticSemaphore.UseAll] All locks obtained.");
                });

            //Create test threads:
            for (int i = 0; i < _threadsToCreate; i++)
                _threads.Add(new Thread(ThreadProc));

            _threads.ForEach((t) => t.Start()); //Start all the threads.
            _threads.ForEach((t) => t.Join()); //Wait on all threads to exit.

            Console.WriteLine($"\tObjects: {_totalAllLockCount:n0}");
            double duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            Console.WriteLine($"\tDuration: {duration:n0}");
            Console.WriteLine("}");

            return duration;
        }
        private void ThreadProc()
        {
            for (int i = 0; i < _objectsPerIteration; i++)
            {
                TestAllVariants();
            }
        }

        private void TestAllVariants()
        {
            _optimisticSemaphore.TryReadAll(
                new ICriticalSection[] { _optimisticSemaphore, _pessimisticSemaphore, _optimisticCriticalSection, _pessimisticCriticalSection }, (o) =>
                {
                    _totalAllLockCount++;
                    //Console.WriteLine("[optimisticSemaphore.TryReadAll] All locks obtained.");
                });

            _optimisticSemaphore.TryWriteAll(
                new ICriticalSection[] { _optimisticSemaphore, _pessimisticSemaphore, _optimisticCriticalSection, _pessimisticCriticalSection }, (o) =>
                {
                    _totalAllLockCount++;
                    //Console.WriteLine("[optimisticSemaphore.TryWriteAll] All locks obtained.");
                });

            _pessimisticSemaphore.TryUseAll(
                new ICriticalSection[] { _optimisticSemaphore, _pessimisticSemaphore, _optimisticCriticalSection, _pessimisticCriticalSection }, (o) =>
                {
                    _totalAllLockCount++;
                    //Console.WriteLine("[pessimisticSemaphore.TryUseAll] All locks obtained.");
                });
        }
    }
}
