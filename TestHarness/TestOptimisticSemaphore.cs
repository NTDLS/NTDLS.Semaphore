using NTDLS.Semaphore;

namespace TestHarness
{
    internal class TestOptimisticSemaphore
    {
        private readonly OptimisticSemaphore<List<string>> _listOfObjects = new();
        private readonly List<Thread> _threads = new();

        const int _threadsToCreate = 10;
        const int _objectsPerIteration = 1000;

        public void Execute()
        {
            Console.WriteLine("[PessimisticSemaphore] {");
            DateTime startTime = DateTime.UtcNow;

            //Create test threads:
            for (int i = 0; i < _threadsToCreate; i++)
                _threads.Add(new Thread(ThreadProc));

            _threads.ForEach((t) => t.Start()); //Start all the threads.
            _threads.ForEach((t) => t.Join()); //Wait on all threads to exit.

            Console.WriteLine($"\tObjects: {_listOfObjects.Read(o => o.Count):n0}");
            Console.WriteLine($"\tDuration: {(DateTime.UtcNow - startTime).TotalMilliseconds:n0}");
            Console.WriteLine("}");
        }

        private void ThreadProc()
        {
            _listOfObjects.Read((o) =>
            {
                foreach (var item in o)
                {
                    if (item.StartsWith(Guid.NewGuid().ToString().Substring(0, 2)))
                    {
                        //Just doing random work to make the iterator take more time.
                    }
                }
            });

            _listOfObjects.Write((o) =>
            {
                //Removing items will break the above iterator in other threads.
                o.RemoveAll(o => o.StartsWith(Guid.NewGuid().ToString().Substring(0, 2)));
            });

            _listOfObjects.Write((o) =>
            {
                //Adding items will also break the above iterator in other threads.
                for (int i = 0; i < _objectsPerIteration; i++)
                {
                    o.Add(Guid.NewGuid().ToString().Substring(0, 4));
                }
            });
        }
    }
}
