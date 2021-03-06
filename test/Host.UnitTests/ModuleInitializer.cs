﻿namespace Host.UnitTests
{
    using Crest.Host;
    using Crest.Host.IO;
    using Host.UnitTests.TestHelpers;

    public static class ModuleInitializer
    {
        public static void Initialize()
        {
            FakeLogger.InterceptLogger();
            QueryLookup.BytePool = FakeArrayPool<byte>.Instance;
            StreamIterator.BytePool = FakeArrayPool<byte>.Instance;
            StreamIterator.CharPool = FakeArrayPool<char>.Instance;
            StringBuffer.Pool = FakeArrayPool<char>.Instance;
        }
    }
}
