using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

using ICSharpCode.Decompiler.Metadata;

using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.Modules;

namespace ReSharperPlugin.ILSpy;

internal sealed class AssemblyResolver : IAssemblyResolver
{
    private readonly Dictionary<string, List<IAssemblyPsiModule>> cache = [];
    private readonly StringBuilder                                logger;

    public AssemblyResolver(ISolution solution, StringBuilder logger)
    {
        this.logger = logger;

        foreach (var module in solution.PsiModules().GetAssemblyModules())
        {
            if (!cache.TryGetValue(module.Assembly.AssemblyName.Name, out var list))
            {
                list = [];
                cache.Add(module.Assembly.AssemblyName.Name, list);
            }

            list.Add(module);
        }
    }

    public Task<MetadataFile> ResolveAsync(IAssemblyReference reference)
    {
        return Task.FromResult(Resolve(reference));
    }

    public Task<MetadataFile> ResolveModuleAsync(MetadataFile mainModule, string moduleName)
    {
        return Task.FromResult(ResolveModule(mainModule, moduleName));
    }

    public MetadataFile Resolve(IAssemblyReference name)
    {
        Log("------------------");
        Log("Resolve: '{0}'", name.FullName);

        // First, find the correct list of assemblies by name.
        if (!cache.TryGetValue(name.Name, out var assemblies))
        {
            Log("Could not find by name: '{0}'", name.FullName);
            return null;
        }

        // If we have only one assembly available, just use it.  This is
        // necessary, because in most cases there is only one assembly, but
        // still might have a version different from what the decompiler asks
        // for.
        if (assemblies.Count == 1)
        {
            Log("Found single assembly: '{0}'", assemblies[0].Assembly.AssemblyName.FullName);
            if (assemblies[0].Assembly.AssemblyName.Version != name.Version)
            {
                Log("WARN: Version mismatch. Expected: '{0}', Got: '{1}'", name.Version, assemblies[0].Assembly.AssemblyName.Version);
            }

            return MakePeFile(assemblies[0]);
        }

        // There are multiple assemblies.
        Log("Found '{0}' assemblies for '{1}':", assemblies.Count, name.Name);

        // Get an exact match or highest version match from the list.
        var highestVersion = default(IAssemblyPsiModule);
        var exactMatch     = default(IAssemblyPsiModule);

        var publicKeyTokenOfName = name.PublicKeyToken ?? [];

        foreach (var assembly in assemblies)
        {
            Log(assembly.Assembly.AssemblyName.Name);

            var version        = assembly.Assembly.AssemblyName.Version;
            var publicKeyToken = assembly.Assembly.AssemblyName.GetPublicKey() ?? [];

            if (version == name.Version && publicKeyToken.SequenceEqual(publicKeyTokenOfName))
            {
                exactMatch = assembly;
                Log("Found exact match: '{0}'", assembly.Assembly.AssemblyName.FullName);
            }
            else if (highestVersion is null || highestVersion.Assembly.AssemblyName.Version < version)
            {
                highestVersion = assembly;
                Log("Found higher version match: '{0}'", assembly.Assembly.AssemblyName.FullName);
            }
        }

        var chosen = exactMatch ?? highestVersion;
        Log("Chosen version: '{0}'", chosen!.Assembly.AssemblyName.FullName);
        return MakePeFile(chosen);

        PEFile MakePeFile(IAssemblyPsiModule module)
        {
            var path = module.Assembly.Location?.AssemblyPhysicalPath?.FullPath;
            Log("Load from: '{0}'", path ?? "<ERR: not on disk!?>");

            if (File.Exists(path))
            {
                return new PEFile(path!, PEStreamOptions.PrefetchMetadata);
            }

            return null;
        }
    }

    public MetadataFile ResolveModule(MetadataFile mainModule, string moduleName)
    {
        Log("-------------");
        Log("Resolve module: '{0}' of '{1}'", moduleName, mainModule.FullName);

        // Primitive implementation to support multi-module assemblies where all
        // modules are located next to the main module.
        var baseDirectory  = Path.GetDirectoryName(mainModule.FileName)!;
        var moduleFileName = Path.Combine(baseDirectory, moduleName);
        if (!File.Exists(moduleFileName))
        {
            Log("Module not found!");
            return null;
        }

        Log("Load from: '{0}'", moduleFileName);
        return new PEFile(moduleFileName, PEStreamOptions.PrefetchMetadata);
    }

    private void Log(string format, params object[] args)
    {
        logger.AppendFormat(format + Environment.NewLine, args);
    }
}