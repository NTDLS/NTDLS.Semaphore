using NTDLS.Semaphore;

namespace PerformanceTest
{
    internal class Program
    {
        const int READ_THREAD_COUNT = 25;
        const int WRITE_THREAD_COUNT = 0;

        /// <summary>
        /// The length of the hashes that will be written to the list.
        /// </summary>
        const int HASH_SIZE = 6;

        /// <summary>
        /// The length of the hash that will be used to perform a "StartsWith" match on the hashes for read operations.
        /// </summary>
        const int PARTIAL_HASH_SIZE = 5;

        /// <summary>
        /// The maximum number of duplicates based on the StartsWith partial hash hit, once this count is reached, we remove all matches.
        /// </summary>
        const int MAX_DUPLICATES = 5;

        /// <summary>
        /// The number of hashes to add to the list before starting the threads.
        /// </summary>
        const int INITIAL_HASH_COUNT = 100000;

        private static ulong _countOfMatches = 0; //This is just to keep the compiler from optimizing our loop too much.
        private static readonly List<Thread> _threads = new();
        private static readonly OptimisticCriticalResource<List<string>> _hashes = new();

        private static string GetRandomString(int length)
            => Guid.NewGuid().ToString()[..length];

        static void Main()
        {
            _hashes.Write(o =>
            {
                for (int i = 0; i < INITIAL_HASH_COUNT; i++)
                {
                    o.Add(GetRandomString(HASH_SIZE));
                }
            });

            for (var i = 0; i < READ_THREAD_COUNT; i++)
            {
                var thread = new Thread(ReadThreadProc);
                _threads.Add(thread);
                thread.Start();
            }

            for (var i = 0; i < WRITE_THREAD_COUNT; i++)
            {
                var thread = new Thread(WriteThreadProc);
                _threads.Add(thread);
                thread.Start();
            }

            while (true)
            {
                Console.WriteLine($"{_countOfMatches:n0}");
                Thread.Sleep(1000);
            }
        }

        private static void ReadThreadProc()
        {
            while (true)
            {
                var fullHash = GetRandomString(HASH_SIZE);

                int countOfMatches = _hashes.Read(o =>
                {
                    var partialHash = fullHash[..PARTIAL_HASH_SIZE];

                    return o.Count(h => h.StartsWith(partialHash));
                });

                _countOfMatches += (ulong)countOfMatches;
            }
        }

        private static void WriteThreadProc()
        {
            while (true)
            {
                var fullHash = GetRandomString(HASH_SIZE);

                _hashes.Write(o =>
                {
                    var partialHash = fullHash[..PARTIAL_HASH_SIZE];

                    if (o.Count(h => h.StartsWith(partialHash)) > MAX_DUPLICATES)
                    {
                        o.RemoveAll(h => h.StartsWith(partialHash));
                    }
                    o.Add(fullHash);
                });
            }
        }
    }
}
