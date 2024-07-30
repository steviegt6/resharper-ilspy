using System;
using System.IO;

namespace Tomat.ReSharperPatcher.Platform;

/// <summary>
///     Common platform utilities.
/// </summary>
public static class CommonPlatform
{
    public static IPlatform Platform => platform ?? throw new InvalidOperationException("Attempted to access platform prior to initialization");
    
    private static IPlatform? platform;

    internal static void InitializePlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            platform = new WindowsPlatform();
        }
        else if (OperatingSystem.IsMacOS())
        {
            throw new PlatformNotSupportedException("macOS isn't supported; PR a platform implementation");
        }
        else if (OperatingSystem.IsLinux())
        {
            throw new PlatformNotSupportedException("Linux isn't supported; PR a platform implementation");
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        try
        {
            Directory.CreateDirectory(platform.GetRspDataDirectory());
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Cannot initialize platform, failed to verify data directory '{platform.GetRspDataDirectory()}'", e);
        }
        
        Console.WriteLine("Using platform implementation: " + platform.GetType().Name);
        Console.WriteLine("Using data directory: " + platform.GetRspDataDirectory());
    }
}