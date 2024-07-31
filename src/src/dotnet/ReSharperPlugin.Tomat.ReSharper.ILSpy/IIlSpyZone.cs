using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.Application.Progress;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Debug;
using JetBrains.Metadata.Reader.API;
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
        containsUnsafeCode = true;
        return "big fat balls";
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
        containsUnsafeCode = true;
        return "big fat balls";
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
        containsUnsafeCode = true;
        return "big fat balls";
    }
}