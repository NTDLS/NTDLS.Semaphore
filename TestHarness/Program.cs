using NTDLS.Semaphore;

namespace TestHarness
{
    internal class Program
    {
        static void Main(string[] args)
        {
            (new TestCriticalSection()).Execute();
            (new TestPessimisticSemaphore()).Execute();
            (new TestOptimisticSemaphore()).Execute();
        }
    }
}