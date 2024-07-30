using System;
using System.Runtime.CompilerServices;

namespace Rider.Backend;

/// <summary>
///     Entry-point into our stub Rider.Backend executable.
/// </summary>
internal static class Program
{
    [STAThread]
    public static void Main()
    {
        AssemblyResolver.Install();
        Patcher.Apply();
        LaunchRider();
        JetBrains.BooleanUtil
    }

    /// <summary>
    ///     Launches the actual Rider.Backend.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void LaunchRider()
    {
        JetBrains.Rider.Backend.Product.RiderBackendProgram.Main();
    }
}