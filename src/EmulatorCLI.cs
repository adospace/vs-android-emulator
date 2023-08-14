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
    private static async Task<CommandResult> ExecuteCommandAsync(params string[] command)
    {
        var emulatorDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Android", "android-sdk", "emulator");
        var emulatorPath = Path.Combine(emulatorDirectory, "emulator.exe");

        return await Cli.Wrap(emulatorPath)
            .WithArguments(command)
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory(emulatorDirectory)
            .ExecuteAsync();
    }

    private static async Task<BufferedCommandResult> ExecuteBufferedCommandAsync(params string[] command)
    {
        ///"C:\Program Files (x86)\Android\android-sdk\emulator"
        var emulatorDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            "Android", "android-sdk", "emulator");
        var emulatorPath = Path.Combine(emulatorDirectory, "emulator.exe");

        var result = await Cli.Wrap(emulatorPath)
            .WithArguments(command)
            .WithValidation(CommandResultValidation.None)
            .WithWorkingDirectory(emulatorDirectory)
            .ExecuteBufferedAsync();

        System.Diagnostics.Debug.WriteLine(result.StandardError);

        System.Diagnostics.Debug.WriteLine(result.StandardOutput);

        return result;
    }

    public static void RunEmulator(string avdName)
    {
        Task.Run(async () => await ExecuteBufferedCommandAsync("-avd", avdName));
    }

    public static async Task<string[]> GetEmulatorListAsync()
    {
        var result = await ExecuteBufferedCommandAsync("-list-avds");

        if (result.ExitCode == 0)
        {
            return result.StandardOutput.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }

        return null;
    }

}
