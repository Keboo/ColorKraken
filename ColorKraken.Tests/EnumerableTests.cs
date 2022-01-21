using System.Threading.Tasks;

using Xunit;
using System.Collections.Generic;
using System;
using System.Threading;

namespace ColorKraken.Tests
{
    public class EnumerableTests
    {
        [Fact]
        public void DeferredExecution()
        {
            IEnumerable<int> numbers = GetNumbers(3);

            //foreach(int number in numbers)
            //{
            //    //Do stuff
            //}

            using IEnumerator<int> numbersEnumerator = numbers.GetEnumerator();
            while(numbersEnumerator.MoveNext())
            {
                int current = numbersEnumerator.Current;
                //Do stuff with current
            }
        }

        [Fact]
        public async Task AsyncDeferredExecution()
        {
            IAsyncEnumerable<int> numbers = GetNumbersAsync(3);

            await foreach (int number in numbers)
            {
                int threadId = Thread.CurrentThread.ManagedThreadId;
                //Do stuff
            }

            //await using IAsyncEnumerator<int> numbersEnumerator = numbers.GetAsyncEnumerator();
            //while (await numbersEnumerator.MoveNextAsync())
            //{
            //    int current = numbersEnumerator.Current;
            //    //Do stuff with current
            //}
        }

        public IEnumerable<int> GetNumbers(int count)
        {
            if (count < 0) throw new ArgumentException("TODO");

            return GetNumbersImplementation(count);

            static IEnumerable<int> GetNumbersImplementation(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    yield return i;
                }
            }
        }

        public IAsyncEnumerable<int> GetNumbersAsync(int count)
        {
            if (count < 0) throw new ArgumentException("TODO");

            return GetNumbersImplementation(count);

            static async IAsyncEnumerable<int> GetNumbersImplementation(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    await Task.Yield();
                    yield return i;
                }
            }
        }
    }
}