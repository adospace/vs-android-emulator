using CliWrap.Buffered;
using CliWrap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace VsAndroidEm;

class EmulatorCLI
{
    private static async Task<CommandResult> ExecuteCommandAsync(string command)
    {
        var adbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Android", "android-sdk", "platform-tools", "emulator.exe");

        return await Cli.Wrap(adbPath)
            .WithArguments(command.Split(' '))
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();
    }

    private static async Task<BufferedCommandResult> ExecuteBufferedCommandAsync(string command)
    {
        var adbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Android", "android-sdk", "platform-tools", "emulator.exe");

        return await Cli.Wrap(adbPath)
            .WithArguments(new[] { command })
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();
    }


}
