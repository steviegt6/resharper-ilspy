using JetBrains.Application.Progress;
using JetBrains.Metadata.Debug;
using JetBrains.ReSharper.Feature.Services.ExternalSources.MetadataTranslator;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;

namespace JetBrains.ReSharper.Feature.Services.ExternalSources.CSharp;

internal static class AnnoyingDecompilerImpl
{
    public static string? TranslateTopLevelTypeElement(
        ITypeElement                         element,
        IAssemblyPsiModule                   context,
        MetadataTranslatorOptions            options,
        IMetadataLocalVariablesNameProvider? metadataLocalVariablesNameProvider,
        out bool                             containsUnsafeCode,
        IProgressIndicator                   indicator,
        DebugData?                           debugData     = null,
        int                                  documentIndex = 0
    )
    {
        containsUnsafeCode = true;
        return "big fat balls";
    }
}