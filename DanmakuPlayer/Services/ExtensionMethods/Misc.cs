using System;

namespace DanmakuPlayer.Services.ExtensionMethods;

public static class Misc
{
    public static T CastThrow<T>(this object? obj) where T : notnull => (T)(obj ?? throw new InvalidCastException());
}
