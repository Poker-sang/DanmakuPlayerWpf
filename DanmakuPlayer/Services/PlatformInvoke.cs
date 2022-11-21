using System.Runtime.InteropServices;

namespace DanmakuPlayer.Services;

public partial class PlatformInvoke
{
    [LibraryImport("user32.dll", SetLastError = false)]
    private static partial nint GetDesktopWindow();

    public static nint DesktopWindowHandle => GetDesktopWindow();
}
