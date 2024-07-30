using System;
using System.IO;

using Tomat.ReSharperPatcher.Platform;

// TODO: Eventually make this actually good.
// Platform-agnostic, abstractions for patches, smart handling for caching, etc.

namespace Tomat.ReSharperPatcher;

internal static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("rsp (r# patcher)");
        Console.WriteLine("a tool to patch r#");
        Console.WriteLine('v' + typeof(Program).Assembly.GetName().Version?.ToString());

        var platform = InitializePlatform();

        var rsHostDir = ResolveReSharperHostDir() ?? throw new DirectoryNotFoundException("Could not resolve ReSharperHost directory");
        Console.WriteLine("ReSharperHost directory: " + rsHostDir);
    }

    private static IPlatform InitializePlatform()
    {
        IPlatform platform;

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
        Console.WriteLine("Using data directory: "          + platform.GetRspDataDirectory());
        return platform;
    }

    private static string? ResolveReSharperHostDir()
    {
        // TODO: platform-dependent
        var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Rider");
        if (!Directory.Exists(dataDir))
        {
            Console.WriteLine("Cannot resolve Rider directory in data directory: " + dataDir);
            return null;
        }

        var rsHostDir = Path.Combine(dataDir, "lib", "ReSharperHost");
        if (!Directory.Exists(rsHostDir))
        {
            Console.WriteLine("Cannot resolve ReSharperHost directory in Rider directory: " + rsHostDir);
            return null;
        }

        return rsHostDir;
    }
}