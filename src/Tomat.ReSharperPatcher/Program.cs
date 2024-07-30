using System;
using System.IO;
using System.Threading.Tasks;

using CliFx;

namespace Tomat.ReSharperPatcher;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        Environment.GetFolderPath(Environment.SpecialFolder)
        return await new CliApplicationBuilder()
                    .SetTitle("r# patcher")
                    .SetVersion('v' + typeof(Program).Assembly.GetName().Version?.ToString())
                    .SetDescription("a tool to patch r#")
                    .AddCommandsFromThisAssembly()
                    .Build()
                    .RunAsync(args);
    }
}