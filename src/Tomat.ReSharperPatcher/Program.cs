using System;
using System.IO;

// TODO: Eventually make this actually good.
// Platform-agnostic, abstractions for patches, smart handling for caching, etc.

namespace Tomat.ReSharperPatcher;

internal static class Program
{
    public static void Main()
    {
        Console.WriteLine("rsp (r# patcher)");
        Console.WriteLine("a tool to patch r#");
        Console.WriteLine('v' + typeof(Program).Assembly.GetName().Version?.ToString());

        var rsHostDir = ResolveReSharperHostDir() ?? throw new DirectoryNotFoundException("Could not resolve ReSharperHost directory");
        Console.WriteLine("ReSharperHost directory: " + rsHostDir);
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