using NTDLS.Semaphore;

namespace TestHarness
{
    internal class TestPessimisticCriticalSection
    {
        private readonly PessimisticCriticalSection _genericCS = new();
        private readonly List<Thread> _threads = new();
        private readonly List<string> _listOfObjects = new();

        private const int _threadsToCreate = 10;
        private const int _objectsPerIteration = 10000;

        public double Execute()
        {
            Console.WriteLine("[CriticalSection] {");
            DateTime startTime = DateTime.UtcNow;

            //Create test threads:
            for (int i = 0; i < _threadsToCreate; i++)
                _threads.Add(new Thread(ThreadProc));

            _threads.ForEach((t) => t.Start()); //Start all the threads.
            _threads.ForEach((t) => t.Join()); //Wait on all threads to exit.

            Console.WriteLine($"\tObjects: {_listOfObjects.Count:n0}");
            double duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            Console.WriteLine($"\tDuration: {duration:n0}");
            Console.WriteLine("}");

            return duration;
        }

        private void ThreadProc()
        {
            _genericCS.Use(() =>
            {
                foreach (var item in _listOfObjects)
                {
                    if (item.StartsWith(Guid.NewGuid().ToString().Substring(0, 2)))
                    {
                        //Just doing random work to make the iterator take more time.
                    }
                }

                //Removing items will break the above iterator in other threads.
                _listOfObjects.RemoveAll(o => o.StartsWith(Guid.NewGuid().ToString().Substring(0, 2)));

                //Adding items will also break the above iterator in other threads.
                for (int i = 0; i < _objectsPerIteration; i++)
                {
                    _listOfObjects.Add(Guid.NewGuid().ToString().Substring(0, 4));
                }
            });
        }
    }
}
