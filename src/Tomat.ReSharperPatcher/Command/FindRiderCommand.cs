using System.Threading.Tasks;

using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

namespace Tomat.ReSharperPatcher.Command;

[Command("find-rider", Description = "Attempts to locate your Rider installation")]
public sealed class FindRiderCommand : ICommand
{
    ValueTask ICommand.ExecuteAsync(IConsole console)
    {
        
    }
}