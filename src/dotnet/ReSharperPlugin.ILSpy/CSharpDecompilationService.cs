using System;
using System.Reflection.PortableExecutable;
using System.Text;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

using JetBrains.ReSharper.Psi.Modules;

namespace ReSharperPlugin.ILSpy;

internal static class CSharpDecompilationService
{
    public static string PerformDecompilation(string fullName, IAssemblyPsiModule module, out bool containsUnsafeCode)
    {
        var logger   = new StringBuilder();
        var resolver = new AssemblyResolver(module.GetSolution(), logger);

        // Load the assembly.
        PEFile file;
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