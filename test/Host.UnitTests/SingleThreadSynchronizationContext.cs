namespace Host.UnitTests
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    // https://blogs.msdn.microsoft.com/pfxteam/2012/01/20/await-synchronizationcontext-and-console-apps/
    internal sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        private readonly BlockingCollection<(SendOrPostCallback, object)> queue =
           new BlockingCollection<(SendOrPostCallback, object)>();

        public static void Run(Func<Task> func)
        {
            SynchronizationContext previous = SynchronizationContext.Current;
            try
            {
                var context = new SingleThreadSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(context);

                Task task = func();
                task.ContinueWith(_ => context.Complete(), TaskScheduler.Default);

                context.RunOnCurrentThread();
                task.GetAwaiter().GetResult();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            this.queue.Add((d, state));
        }

        private void Complete()
        {
            this.queue.CompleteAdding();
        }

        private void RunOnCurrentThread()
        {
            while (this.queue.TryTake(out (SendOrPostCallback cb, object state) workItem))
            {
                workItem.cb(workItem.state);
            }
        }
    }
}
