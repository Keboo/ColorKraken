
using Microsoft.Extensions.Configuration;

namespace ColorKraken.Configuration;

public static class TollerantSourceExtensions
{
    public static void IgnoreExceptionFromLastSource(this IConfigurationBuilder builder)
    {
        var last = builder.Sources[builder.Sources.Count - 1];
        builder.Sources.RemoveAt(builder.Sources.Count - 1);
        builder.Add(new TollerantSource(last));
    }
}
