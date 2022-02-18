
using Microsoft.Extensions.Configuration;

namespace ColorKraken.Configuration;

public static class TolerantSourceExtensions
{
    public static void IgnoreExceptionFromLastSource(this IConfigurationBuilder builder)
    {
        var last = builder.Sources[^1];
        builder.Sources.Remove(last);
        builder.Add(new TolerantSource(last));
    }
}
