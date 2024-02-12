using System;
using Microsoft.Extensions.Options;

namespace TollBooth.Options;

internal static class OptionsExtensions
{
    internal static void Configure<T>(this IOptionsMonitor<T> options, Action<T> configure)
    {
        configure(options.CurrentValue);
        options.OnChange(configure);
    }
}
