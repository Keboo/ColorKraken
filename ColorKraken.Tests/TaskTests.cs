using System.Threading.Tasks;

using Xunit;
using System.Threading;
using System;

namespace ColorKraken.Tests
{
    public class TaskTests
    {
        [Fact]
        public async Task Foo()
        {
            int t1 = Environment.CurrentManagedThreadId;

            //await Task.Yield();
            //await Task.Delay(1);
            //IOnly meaningful if the caller is the UI thread (has a synchonization context)
            //await Task.Run(() => { }).ConfigureAwait(true);
            await Task.CompletedTask;

            int t2 = Thread.CurrentThread.ManagedThreadId;

            Assert.Equal(t1, t2);
        }
    }
}