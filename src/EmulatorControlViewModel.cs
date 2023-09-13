using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnvDTE;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace VsAndroidEm;

public class EmulatorControlViewModel : ObservableObject
{
    public ObservableCollection<EmulatorProcess> _processes = new();
    private readonly DispatcherTimer _timer;

    public EmulatorControlViewModel() 
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        _timer.Tick += Timer_Tick;
        _timer.Start();

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
    }

    public ObservableCollection<EmulatorProcess> Processes => _processes;

    private EmulatorProcess _selectedEmulator;

    public EmulatorProcess SelectedEmulator
    {
        get => _selectedEmulator;
        set
        {
            SetProperty(ref _selectedEmulator, value);
            OnPropertyChanged(nameof(StartCommand));
            OnPropertyChanged(nameof(StopCommand));
            OnPropertyChanged(nameof(ShowToolBarWindowCommand));
            OnPropertyChanged(nameof(ShutdownCommand));
            OnPropertyChanged(nameof(ForceAttachmentCommand));
        }
    }

    public ICommand StartCommand => SelectedEmulator?.StartCommand;
    
    public ICommand StopCommand => SelectedEmulator?.StopCommand;
    
    public ICommand ShowToolBarWindowCommand => SelectedEmulator?.ShowToolBarWindowCommand;
    
    public ICommand ShutdownCommand => SelectedEmulator?.ShutdownCommand;

    public ICommand ForceAttachmentCommand => SelectedEmulator?.ForceAttachmentCommand;

    public ICommand RefreshCommand { get; }

    private void Timer_Tick(object sender, EventArgs e)
    {
        var avdRunningFolder = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "temp", "avd", "running");

        if (!Directory.Exists(avdRunningFolder))
        {
            return;
        }

        var avdRunningFolderFiles = Directory.GetFiles(avdRunningFolder, "*.ini");

        foreach (var file in avdRunningFolderFiles)
        {
            var processId = int.Parse(System.IO.Path.GetFileNameWithoutExtension(file).Substring(4)); //pid_

            if (_processes.Any(_ => _.ProcessId == processId))
            {
                continue;
            }

            if (!Win32API.CheckProcessIsRunning(processId))
            {
                continue;
            }

            GetFromIniFile(file);

            //try
            //{
            //    SelectedEmulator = GetFromIniFile(file);

            //    //force notifications
            //    OnPropertyChanged(nameof(SelectedEmulator));
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine(ex);
            //}

        }

        //SelectedEmulator = _processes.FirstOrDefault();
    }

    private EmulatorProcess GetFromIniFile(string iniFilePath)
    {
        var processId = int.Parse(System.IO.Path.GetFileNameWithoutExtension(iniFilePath).Substring(4));
        var iniValues = File.ReadAllLines(iniFilePath)
            .Select(_ => _.Split('='))
            .ToDictionary(_ => _[0], _ => _[1]);

        var avdName = iniValues["avd.name"];
        var emulatorName = $"emulator-{iniValues["port.serial"]}";

        foreach (var process in _processes)
        {
            if (process.Name ==  avdName) 
            {
                process.ProcessExited += Emulator_ProcessExited;
                process.ErrorRaised += Emulator_ErrorRaised;
                process.ProcessAttached += Emulator_ProcessAttached;
                

                process.Monitor(processId, emulatorName);

                return process;
            }
        }

        return null;
    }

    private void Emulator_ProcessAttached(object sender, EventArgs e)
    {
        var currentEmulator = SelectedEmulator;
        SelectedEmulator = null;
        SelectedEmulator = currentEmulator;
    }

    private void Emulator_ErrorRaised(object sender, EventArgs e)
    {

    }

    private async void Emulator_ProcessExited(object sender, EventArgs e)
    {
        try
        {
            var emulatorProcess = (EmulatorProcess)sender;
            emulatorProcess.ProcessExited -= Emulator_ProcessExited;
            emulatorProcess.ErrorRaised -= Emulator_ErrorRaised;
            emulatorProcess.ProcessAttached -= Emulator_ProcessAttached;

            await emulatorProcess.StopAsync();

            SelectedEmulator = _processes.FirstOrDefault(_ => _.IsStarted);

            //force notifications
            OnPropertyChanged(nameof(SelectedEmulator));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    public async Task RefreshAsync()
    {
        var avdList = await EmulatorCLI.GetEmulatorListAsync();
        if (avdList == null)
        {
            return;
        }

        foreach (var avdName in avdList)
        {
            if (_processes.Any(_=>_.Name == avdName))
            {
                continue;
            }    

            _processes.Add(new EmulatorProcess(avdName));
        }

        OnPropertyChanged(nameof(Processes));
    }

    public async Task StopAsync()
    {
        try
        {
            _timer.Tick -= Timer_Tick;
            _timer.Stop();

            foreach (var process in _processes.ToArray())
            {
                await process.StopAsync();

                var avdRunningFolderFiles = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "temp", "avd", "running");

                var processIniFile = System.IO.Path.Combine(avdRunningFolderFiles, $"pid_{process.ProcessId}.ini");

                File.Delete(processIniFile);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }



}
