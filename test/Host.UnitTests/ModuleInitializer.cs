namespace Host.UnitTests
{
    using Crest.Host.Serialization;

    public static class ModuleInitializer
    {
        public static void Initialize()
        {
            FakeLogger.InterceptLogger();
            StreamIterator.BytePool = FakeArrayPool<byte>.Instance;
            StreamIterator.CharPool = FakeArrayPool<char>.Instance;
            StringBuffer.Pool = FakeArrayPool<char>.Instance;
        }
    }
}
