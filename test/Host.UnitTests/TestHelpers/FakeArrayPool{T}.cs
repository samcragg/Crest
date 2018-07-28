namespace Host.UnitTests.TestHelpers
{
    using System.Buffers;

    internal class FakeArrayPool<T> : ArrayPool<T>
    {
        internal static FakeArrayPool<T> Instance { get; }
            = new FakeArrayPool<T>();

        internal int TotalAllocated { get; private set; }

        public override T[] Rent(int minimumLength)
        {
            lock (FakeArrayPool.LockObject)
            {
                this.TotalAllocated += minimumLength;
                return new T[minimumLength];
            }
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            lock (FakeArrayPool.LockObject)
            {
                this.TotalAllocated -= array.Length;
            }
        }

        internal void Reset()
        {
            this.TotalAllocated = 0;
        }
    }
}
