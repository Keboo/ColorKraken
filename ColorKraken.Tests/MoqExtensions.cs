using System.Collections.Generic;

using Moq.Language.Flow;

namespace ColorKraken.Tests;

public static class MoqExtensions
{
    public static IReturnsResult<TMock> ReturnsAsyncEnumerable<TMock, T>(
        this ISetup<TMock, IAsyncEnumerable<T>> mock, params T[] items)
        where TMock : class
    {
        return mock.Returns(AsAsyncEnumerable(items));
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    private static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(IEnumerable<T> items)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        foreach (var item in items)
        {
            yield return item;
        }
    }
}
