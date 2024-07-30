using System.Threading.Tasks;

using CliFx;

using Tomat.ReSharperPatcher.Platform;

namespace Tomat.ReSharperPatcher;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        CommonPlatform.InitializePlatform();
        
        return await new CliApplicationBuilder()
                    .SetTitle("rsp (r# patcher)")
                    .SetVersion('v' + typeof(Program).Assembly.GetName().Version?.ToString())
                    .SetDescription("a tool to patch r#")
                    .AddCommandsFromThisAssembly()
                    .Build()
                    .RunAsync(args);
    }
}