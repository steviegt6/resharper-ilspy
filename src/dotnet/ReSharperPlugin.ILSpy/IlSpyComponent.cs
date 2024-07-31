using System.Collections.Generic;
using System.Reflection;
using System.Text;

using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Notifications;
using JetBrains.Application.Progress;
using JetBrains.Lifetimes;
using JetBrains.Metadata.Debug;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.ExternalSources.CSharp.MetadataTranslator;
using JetBrains.ReSharper.Feature.Services.ExternalSources.MetadataTranslator;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;

using MonoMod.RuntimeDetour;

// TODO: I'm not very familiar with the R# SDK.
// I should figure out a way to support unloading and such.  Is ShellComponent
// the best option I have for hooking?  I'd also like to fire a notification
// once hooks have been applied.  Speaking of, hooks don't apply immediately --
// this is jarring.

namespace ReSharperPlugin.ILSpy;

[ShellComponent]
public class IlSpyComponent
{
    // ReSharper disable once CollectionNeverQueried.Local - Used to preserve
    //                                                       hook lifetimes.
    private static readonly List<object> lifetime_extender = [];

    public IlSpyComponent(Lifetime lifetime, UserNotifications notifications)
    {
        lifetime_extender.Add(
            new Hook(
                typeof(CSharpMetadataTranslator).GetMethod(nameof(TranslateByDecompiler), BindingFlags.Instance | BindingFlags.NonPublic)!,
                typeof(IlSpyComponent).GetMethod(nameof(TranslateByDecompiler), BindingFlags.Static             | BindingFlags.NonPublic)!
            )
        );

        lifetime_extender.Add(
            new Hook(
                typeof(CSharpMetadataTranslator).GetMethod("WriteHeader", BindingFlags.Instance | BindingFlags.NonPublic)!,
                typeof(IlSpyComponent).GetMethod(nameof(WriteHeader), BindingFlags.Static       | BindingFlags.NonPublic)!
            )
        );

        notifications.CreateNotification(lifetime, NotificationSeverity.INFO, "ILSpy Hooks Applied", "ReSharper has been patched to use ILSpy instead of dotPeek!");
    }

    private static string TranslateByDecompiler(
        // ReSharper disable UnusedParameter.Local - Must match original sig.
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
        // ReSharper restore UnusedParameter.Local
    )
    {
        return CSharpDecompilationService.PerformDecompilation(typeElement.GetClrName().FullName, assemblyPsiModule, out containsUnsafeCode);
    }

    private static (StringBuilder sb, int startLinesCount) WriteHeader(
        // ReSharper disable UnusedParameter.Local - Must match original sig.
        CSharpMetadataTranslator                        @this,
        [CanBeNull] ITypeElement                        element,
        [CanBeNull] IAssemblyPsiModule                  assemblyPsiModule,
        [CanBeNull] IMetadataTypeInfo                   typeInfo,
        MetadataTranslatorOptions                       options,
        [CanBeNull] IMetadataLocalVariablesNameProvider metadataLocalVariablesNameProvider
        // ReSharper restore UnusedParameter.Local
    )
    {
        const int start_lines_count = 7;

        var sb = new StringBuilder();

        sb.AppendLine($"#region Assembly {assemblyPsiModule?.Assembly.AssemblyName.FullName      ?? "<ERR: unknown psi module>"}");
        sb.AppendLine($"// {assemblyPsiModule?.Assembly.Location?.AssemblyPhysicalPath?.FullPath ?? "<ERR: unknown assembly location>"}");
        sb.AppendLine($"// Decompiled with ICSharpCode.Decompiler {typeof(ICSharpCode.Decompiler.DecompilerException).Assembly.GetName().Version}");
        sb.AppendLine("// Patched by Tomat: https://github.com/steviegt6/resharper-ilspy");
        sb.AppendLine("#endregion");
        sb.AppendLine(); // For formatting.

        return (sb, start_lines_count);
    }
}