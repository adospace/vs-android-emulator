using CliWrap;
using CliWrap.Buffered;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsAndroidEm;

class AdbCLI
{
    private static async Task<CommandResult> ExecuteCommandAsync(params string[] command)
    {
        var adbDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Android", "android-sdk", "platform-tools");
        var adbPath = Path.Combine(adbDirectory, "adb.exe");

        return await Cli.Wrap(adbPath)
            .WithArguments(command)
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory(adbDirectory)
            .ExecuteAsync();
    }

    private static async Task<BufferedCommandResult> ExecuteBufferedCommandAsync(params string[] command)
    {
        var adbDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Android", "android-sdk", "platform-tools");
        var adbPath = Path.Combine(adbDirectory, "adb.exe");

        var result = await Cli.Wrap(adbPath)
            .WithArguments(command)
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory(adbDirectory)
            .ExecuteBufferedAsync();



        return result;
    }

    public static async Task<bool> ShutdownEmulatorAsync(string emulatorName)
        => (await ExecuteCommandAsync("-s", emulatorName, "shell", "reboot", "-p")).ExitCode == 0;

    public static async Task<bool> KillEmulatorAsync(string emulatorName)
        => (await ExecuteCommandAsync("-s", emulatorName, "emu", "Kill")).ExitCode == 0;



}
