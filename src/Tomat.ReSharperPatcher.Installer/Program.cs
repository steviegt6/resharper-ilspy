using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Tomat.ReSharperPatcher.Installer;

internal static class Program
{
    private const           string pub_key = "002400000480000094000000060200000024000052534131000400000100010087f63ba6a789c30e210e7ec987234ad9fe33baf7367993bab1b312d6f72ca296b91ed5c658964ffb9e7570eb184a527c68c6bdba41cfe67d8cfd3f888234206bf39205a3652d3af3445bb6f715fdac532e289fea41229bac37762b67eb16f58fee717d2465fca9ee17f08ed16772a1fc52c1c17022e1f0d9bdd004524a663aca";
    private static readonly byte[] pub_key_bytes;

    static Program()
    {
        pub_key_bytes = new byte[pub_key.Length / 2];
        for (var i = 0; i < pub_key.Length; i += 2)
        {
            pub_key_bytes[i / 2] = Convert.ToByte(pub_key.Substring(i, 2), 16);
        }
    }

    public static void Main()
    {
        var rshDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Rider", "lib", "ReSharperHost");
        if (!Directory.Exists(rshDir))
        {
            Console.WriteLine("Directory not found: " + rshDir);
            return;
        }

        Console.WriteLine("Determining Rider.Backend executable...");

        var candidates  = (string[]) ["Rider.Backend.exe", "Rider.Backend.exe.backup"];
        var backendFile = default(string);
        foreach (var candidate in candidates)
        {
            Console.WriteLine("Testing: " + candidate);

            var candidatePath = Path.Combine(rshDir, candidate);
            if (!File.Exists(candidatePath))
            {
                Console.WriteLine("File not found: " + candidatePath);
                continue;
            }

            using var fs       = File.OpenRead(candidatePath);
            using var peReader = new PEReader(fs);

            if (!peReader.HasMetadata)
            {
                Console.WriteLine("File has no metadata");
                continue;
            }

            var metadataReader = peReader.GetMetadataReader();
            var asmDef         = metadataReader.GetAssemblyDefinition();

            if (!asmDef.GetAssemblyName().GetPublicKey()?.SequenceEqual(pub_key_bytes) ?? false)
            {
                Console.WriteLine("Assembly had no public key or public key did not match");
                continue;
            }

            var asmName = metadataReader.GetAssemblyDefinition().GetAssemblyName();
            if (asmName.Name != "Rider.Backend")
            {
                Console.WriteLine("Assembly name did not match: " + asmName.Name);
                continue;
            }

            backendFile = candidatePath;
            break;
        }

        if (backendFile is null)
        {
            Console.WriteLine("Backend executable not found!");
            return;
        }

        if (!backendFile.Contains("Rider.Backend.exe.backup"))
        {
            Console.WriteLine("Backing up executable...");

            var backupFile = backendFile + ".backup";
            if (File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }

            File.Copy(backendFile, backupFile);
        }
        else
        {
            Console.WriteLine("Backup file already present, skipping backup creation");
        }

        var rspDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "resharper-patcher");
        if (File.Exists(rspDir))
        {
            Console.WriteLine("Cannot create directory: " + rshDir);
        }

        Directory.CreateDirectory(rspDir);

        Console.WriteLine("Copying files to patch directory...");
        foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory()))
        {
            var dest = Path.Combine(rspDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        Console.WriteLine("Copying rsp.exe to ReSharperHost directory as Rider.Backend.exe...");
        File.Copy(Path.Combine(rspDir, "rsp.exe"), backendFile, true);

        Console.WriteLine("Copying rsp.dll to ReSharperHost directory...");
        File.Copy(Path.Combine(rspDir, "rsp.dll"), Path.Combine(rshDir, "rsp.dll"), true);
    }
}