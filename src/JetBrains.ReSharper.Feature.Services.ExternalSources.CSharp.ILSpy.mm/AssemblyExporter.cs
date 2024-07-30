using System;
using System.Collections.Generic;
using System.Text;

using JetBrains.Application.Progress;
using JetBrains.Metadata.Debug;
using JetBrains.ProjectModel.Model2.Assemblies.Interfaces;
using JetBrains.ReSharper.Feature.Services.ExternalSources.Core;
using JetBrains.ReSharper.Feature.Services.ExternalSources.CSharp.AssemblyExport;
using JetBrains.ReSharper.Feature.Services.ExternalSources.MetadataTranslator;
using JetBrains.ReSharper.Feature.Services.ExternalSources.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Feature.Services.ExternalSources.CSharp;

// ReSharper disable once InconsistentNaming
public class patch_AssemblyExporter /*: AssemblyExporter*/
{
    private readonly object                         myContainsUnsafeCodeLock   = null!;
    private readonly Tuple<PsiLanguageType, string> myLanguageAndExtensionPair = null!;
    private readonly ISharedDecompilationCache      mySharedDecompilationCache = null!;
    private readonly HashSet<FileSystemPath>        myDecompilationCacheItems  = null!;

    private          IMetadataTranslator       myTranslator               = null!;
    private          DebugData?                myDebugData                = null!;
    private          IAssemblyExportParameters myAssemblyExportParameters = null!;
    private          IAssembly                 myAssembly                 = null!;
    private readonly PsiLanguageType           myLanguage                 = null!;
    private          bool                      myContainsUnsafeCode;

    private extern MetadataTranslatorOptions GetTranslatorOptions(bool addPartialModifier = false);

    private extern string GetFullUrl(string url);

    private bool DecompileTypeElement(ITypeElement? typeElement, bool isPartial, FileSystemPath filePath, string relativeFilePath, IProgressIndicator progressIndicator)
    {
        if (typeElement?.Module is not IAssemblyPsiModule context)
        {
            return false;
        }

        var translatorOptions = GetTranslatorOptions(isPartial);
        if (!myTranslator.IsAvailable)
        {
            return false;
        }

        var documentIndex = myDebugData != null ? myDebugData.CreateDocument(GetFullUrl(relativeFilePath)).Index : 0;
        // var text       = myTranslator.TranslateTopLevelTypeElement(typeElement, context, translatorOptions, null, out var containsUnsafeCode, progressIndicator, myDebugData, documentIndex);
        var text = AnnoyingDecompilerImpl.TranslateTopLevelTypeElement(typeElement, context, translatorOptions, null, out var containsUnsafeCode, progressIndicator, myDebugData, documentIndex);
        if (text == null)
        {
            return false;
        }

        DecompilationCacheItem? decompilationCacheItem = null;
        if (myAssemblyExportParameters.PutSourcesIntoDecompilationCache)
        {
            var text2 = typeElement.TryGetFileName(myLanguageAndExtensionPair);
            if (!text2.IsEmpty())
            {
                var typeCacheMoniker = MonikerUtil.GetTypeCacheMoniker(typeElement);
                var fullPath         = mySharedDecompilationCache.GetFilePath("decompiler", myAssembly, typeCacheMoniker, text2).FullPath;
                var sourceDebugData  = myDebugData?.CreateDocumentSection(documentIndex, fullPath);
                using (ReadLockCookie.Create())
                {
                    decompilationCacheItem = mySharedDecompilationCache.PutCacheItemIfPropertiesHaveChanged("decompiler", myAssembly, typeCacheMoniker, text2, translatorOptions.ToPropertiesDictionary(typeElement, myLanguage), text, sourceDebugData);
                }
                lock (myDecompilationCacheItems)
                {
                    if (decompilationCacheItem != null && !myDecompilationCacheItems.Add(decompilationCacheItem.Location))
                    {
                        Logger.LogErrorWithSensitiveData("Element has already been added to decompilation cache", new Pair<string, object>[1] { Pair.Of("Element", (object)decompilationCacheItem.Location) });
                    }
                }
            }
        }
        if (decompilationCacheItem == null || decompilationCacheItem.Location != filePath)
        {
            filePath.WriteAllText(text, Encoding.UTF8);
        }

        lock (myContainsUnsafeCodeLock)
        {
            myContainsUnsafeCode |= containsUnsafeCode;
        }

        return true;
    }
}