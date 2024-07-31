using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Progress;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Debug;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ExternalSources;
using JetBrains.ReSharper.Feature.Services.ExternalSources.CSharp.AssemblyExport;
using JetBrains.ReSharper.Feature.Services.ExternalSources.CSharp.MetadataTranslator;
using JetBrains.ReSharper.Feature.Services.ExternalSources.MetadataTranslator;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Modules;

using Mono.Cecil.Cil;

using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace ReSharperPlugin.Tomat.ReSharper.ILSpy;

[ZoneDefinition]
// [ZoneDefinitionConfigurableFeature("Title", "Description", IsInProductSection: false)]
public interface IIlSpyZone : IZone, IRequire<ILanguageCSharpZone>, IRequire<ExternalSourcesZone>;

[ShellComponent]
public class MyComponent
{
    private static readonly Dictionary<string, Assembly> asm_map = [];

    private static             bool   initialized;
    [CanBeNull] private static ILHook decompileTypeElement;
    [CanBeNull] private static Hook   translateByDecompiler;
    [CanBeNull] private static Hook   translateTopLevelTypeElementByDecompiler;
    [CanBeNull] private static Hook   writeHeader;

    static MyComponent()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var asmName      = new AssemblyName(args.Name);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var dir          = Path.Combine(localAppData, "resharper-patcher");
            var path         = Path.Combine(dir,          asmName.Name + ".dll");

            if (asm_map.TryGetValue(asmName.Name, out var asm))
            {
                return asm;
            }

            if (!File.Exists(path))
            {
                return null;
            }

            return asm_map[asmName.Name] = Assembly.LoadFrom(path);
        };

        Initialize();
    }

    private static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        var meth = typeof(AssemblyExporter).GetMethods(BindingFlags.Instance | BindingFlags.NonPublic).First(x => x.Name == "DecompileTypeElement" && x.ReturnType == typeof(bool));
        decompileTypeElement = new ILHook(
            meth,
            il =>
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.Before, x => x.MatchCallvirt<IMetadataTranslator>("TranslateTopLevelTypeElement"));
                c.Remove();
                c.Emit(OpCodes.Call, typeof(MyComponent).GetMethod(nameof(TranslateTopLevelTypeElement), BindingFlags.Static | BindingFlags.NonPublic)!);
            }
        );

        meth                  = typeof(CSharpMetadataTranslator).GetMethod("TranslateByDecompiler", BindingFlags.Instance | BindingFlags.NonPublic)!;
        translateByDecompiler = new Hook(meth, typeof(MyComponent).GetMethod(nameof(TranslateByDecompiler), BindingFlags.Static | BindingFlags.NonPublic)!);

        meth                                     = typeof(CSharpMetadataTranslator).GetMethod("TranslateTopLevelTypeElementByDecompiler", BindingFlags.Instance | BindingFlags.NonPublic)!;
        translateTopLevelTypeElementByDecompiler = new Hook(meth, typeof(MyComponent).GetMethod(nameof(TranslateTopLevelTypeElementByDecompiler), BindingFlags.Static | BindingFlags.NonPublic)!);

        meth        = typeof(CSharpMetadataTranslator).GetMethod("WriteHeader", BindingFlags.Instance | BindingFlags.NonPublic)!;
        writeHeader = new Hook(meth, typeof(MyComponent).GetMethod(nameof(WriteHeader), BindingFlags.Static | BindingFlags.NonPublic)!);
    }

    [CanBeNull]
    private static string TranslateTopLevelTypeElement(
        IMetadataTranslator                             @this,
        [NotNull]   ITypeElement                        element,
        [NotNull]   IAssemblyPsiModule                  context,
        [NotNull]   MetadataTranslatorOptions           options,
        [CanBeNull] IMetadataLocalVariablesNameProvider metadataLocalVariablesNameProvider,
        out         bool                                containsUnsafeCode,
        [NotNull]   IProgressIndicator                  indicator,
        [CanBeNull] DebugData                           debugData     = null,
        int                                             documentIndex = 0
    )
    {
        return CSharpDecompilationService.PerformDecompilation(element.GetClrName().FullName, context, out containsUnsafeCode);
    }

    private static string TranslateByDecompiler(
        CSharpMetadataTranslator                        @this,
        [NotNull]   ITypeElement                        typeElement,
        [NotNull]   IAssemblyPsiModule                  assemblyPsiModule,
        [NotNull]   MetadataTranslatorOptions           options,
        [CanBeNull] IMetadataLocalVariablesNameProvider metadataLocalVariablesNameProvider,
        out         bool                                containsUnsafeCode,
        [NotNull]   IProgressIndicator                  indicator,
        [CanBeNull] DebugData                           debugData,
        int                                             documentIndex,
        int                                             startLinesCount
    )
    {
        return CSharpDecompilationService.PerformDecompilation(typeElement.GetClrName().FullName, assemblyPsiModule, out containsUnsafeCode);
    }

    private static string TranslateTopLevelTypeElementByDecompiler(
        CSharpMetadataTranslator              @this,
        [NotNull] IMetadataTypeInfo           typeInfo,
        Lifetime                              lifetime,
        [CanBeNull] IAssemblyPsiModule        assemblyPsiModule,
        [NotNull]   MetadataTranslatorOptions options,
        IMetadataLocalVariablesNameProvider   metadataLocalVariablesNameProvider,
        out         bool                      containsUnsafeCode,
        [NotNull]   IProgressIndicator        indicator,
        [CanBeNull] DebugData                 debugData,
        int                                   documentIndex,
        int                                   startLinesCount
    )
    {
        return CSharpDecompilationService.PerformDecompilation(typeInfo.FullyQualifiedName, assemblyPsiModule, out containsUnsafeCode);
    }

    private static (StringBuilder sb, int startLinesCount) WriteHeader(
        CSharpMetadataTranslator                        @this,
        [CanBeNull] ITypeElement                        element,
        [CanBeNull] IAssemblyPsiModule                  assemblyPsiModule,
        [CanBeNull] IMetadataTypeInfo                   typeInfo,
        MetadataTranslatorOptions                       options,
        [CanBeNull] IMetadataLocalVariablesNameProvider metadataLocalVariablesNameProvider
    )
    {
        const int start_lines_count = 7;

        var sb = new StringBuilder();

        sb.AppendLine($"#region Assembly {assemblyPsiModule?.Assembly.AssemblyName.FullName ?? "<ERR: unknown psi module>"}");
        sb.AppendLine($"// {GetAssemblyPath(assemblyPsiModule)                              ?? "<ERR: unknown assembly location>"}");
        sb.AppendLine($"// Decompiled with ICSharpCode.Decompiler {typeof(ICSharpCode.Decompiler.DecompilerException).Assembly.GetName().Version}");
        sb.AppendLine("// Patched by Tomat: https://github.com/steviegt6/resharper-patcher");
        sb.AppendLine("#endregion");
        sb.AppendLine(); // For formatting.

        return (sb, start_lines_count);
    }

    [CanBeNull] private static string GetAssemblyPath(IAssemblyPsiModule module)
    {
        return module?.Assembly.Location?.AssemblyPhysicalPath?.FullPath ?? module?.Assembly.Location?.ContainerPhysicalPath?.FullPath;
    }
}

internal sealed class AssemblyResolver : ICSharpCode.Decompiler.Metadata.IAssemblyResolver
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

    /*public PEFile TryResolve(string path, PEStreamOptions streamOptions)
    {
        if
    }*/

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

            /*var result = TryResolve(path, PEStreamOptions.PrefetchMetadata);
            if (result is not null)
            {
                return result;
            }*/

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
        var baseDirectory  = Path.GetDirectoryName(mainModule.FileName);
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

internal static class CSharpDecompilationService
{
    [CanBeNull]
    public static string PerformDecompilation(string fullName, IAssemblyPsiModule module, out bool containsUnsafeCode)
    {
        var logger   = new StringBuilder();
        var resolver = new AssemblyResolver(module.GetSolution(), logger);

        // Load the assembly.
        var file = default(PEFile);
        if (module.Assembly.Location?.AssemblyPhysicalPath?.FullPath is { } assemblyLocation)
        {
            file = new PEFile(assemblyLocation, PEStreamOptions.PrefetchEntireImage);
        }
        else
        {
            containsUnsafeCode = false;
            return null;
        }

        using (file)
        {
            // Initialize a decompiler with default settings.
            var decompiler = new CSharpDecompiler(file, resolver, new DecompilerSettings());

            // Escape invalid identifiers to prevent Roslyn from failing to
            // parse the generated code.  (This happens, for example, when there
            // is compiler-generated code that is not yet recognized/transformed
            // by the decompiler.)
            decompiler.AstTransforms.Add(new EscapeInvalidIdentifiers());

            var fullTypeName = new FullTypeName(fullName);

            // ILSpy only allows decompiling a type that comes from the 'Main
            // Module'.  It will throw on anything else.  Prevent this by doing
            // this quick check corresponding to:
            // https://github.com/icsharpcode/ILSpy/blob/4ebe075e5859939463ae420446f024f10c3bf077/ICSharpCode.Decompiler/CSharp/CSharpDecompiler.cs#L978
            var type = decompiler.TypeSystem.MainModule.GetTypeDefinition(fullTypeName);
            if (type is null)
            {
                containsUnsafeCode = false;
                return null;
            }

            containsUnsafeCode = true; // man whatever
            var text = decompiler.DecompileTypeAsString(fullTypeName);
            return text + Environment.NewLine + "#if false // Decompilation log" + Environment.NewLine + logger + "#endif" + Environment.NewLine;
        }
    }
}