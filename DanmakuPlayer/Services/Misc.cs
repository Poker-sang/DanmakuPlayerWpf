using System;

namespace DanmakuPlayer.Services;

public static class Misc
{
    public static string ToTime(this double sec) =>
        ((int)sec / 60).ToString().PadLeft(2, '0') + ":" +
        ((int)sec % 60).ToString().PadLeft(2, '0');

    public static T Get<T>(this WeakReference<T> w) where T : class
    {
        if (w.TryGetTarget(out var t))
            return t;
        throw new NullReferenceException();
    }

    public static T CastThrow<T>(this object? obj) where T : notnull => (T)(obj ?? throw new InvalidCastException());
}
