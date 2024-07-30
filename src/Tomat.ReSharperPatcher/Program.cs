using System;
using System.IO;
using System.Linq;

using AsmResolver.DotNet;

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

        File.Copy("ICSharpCode.Decompiler.dll",      Path.Combine(rsHostDir, "ICSharpCode.Decompiler.dll"),      true);
        File.Copy("Tomat.ReSharperPatcher.Impl.dll", Path.Combine(rsHostDir, "Tomat.ReSharperPatcher.Impl.dll"), true);

        var implDll = AssemblyDefinition.FromFile(Path.Combine(rsHostDir, "Tomat.ReSharperPatcher.Impl.dll"));

        var csharpDll            = AssemblyDefinition.FromFile(Path.Combine(rsHostDir, "JetBrains.ReSharper.Feature.Services.ExternalSources.CSharp.dll"));
        var mod                  = csharpDll.Modules.First();
        var assemblyExporter     = mod.TopLevelTypes.First(x => x.Name == "AssemblyExporter");
        var decompileTypeElement = assemblyExporter.Methods.First(x => x.Name == "DecompileTypeElement" && x.Signature!.ReturnType == mod.CorLibTypeFactory.Boolean);
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