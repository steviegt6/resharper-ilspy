using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Rider.Backend;

internal static class AssemblyResolver
{
    private static readonly Dictionary<string, Assembly> asm_map = [];

    public static void Install()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            Console.WriteLine("Got AssemblyResolve event for: " + args.Name);

            if (asm_map.TryGetValue(args.Name, out var asm))
            {
                Console.WriteLine("Returning cached assembly: " + asm.FullName);
                return asm;
            }

            var asmName = new AssemblyName(args.Name);
            var rspDir  = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "resharper-patcher");

            var candidate = Path.Combine(rspDir, asmName.Name + ".dll");
            if (File.Exists(candidate))
            {
                Console.WriteLine("Loading assembly from: " + candidate);
                return asm_map[args.Name] = Assembly.LoadFrom(candidate);
            }

            Console.WriteLine("Assembly not found in rsp dir: " + candidate);
            return null;
        };
    }
}