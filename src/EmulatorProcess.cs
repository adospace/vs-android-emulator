using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Input;

namespace VsAndroidEm
{
    public class EmulatorProcess : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly EmulatorViewer _viewer = new();
        private bool _isBusy;

        public EmulatorProcess(int processId, string name, string emulatorName)
        {
            ProcessId = processId;
            Name = name;
            EmulatorName = emulatorName;
            StartCommand = new RelayCommand(Start, () => !IsBusy);
            StopCommand = new AsyncRelayCommand(StopAsync, () => !IsBusy);
            ShowToolBarWindowCommand = new RelayCommand(ShowToolWindow, () => !IsBusy);
            ShutdownCommand = new AsyncRelayCommand(ShutdownAsync, () => !IsBusy);

            HostView = new WindowsFormsHost
            {
                Child = _viewer,
            };

            _viewer.ProcessExited += (s, e) => ProcessExited?.Invoke(this, e);
            _viewer.ErrorRaised += (s, e) =>
            {
                OnPropertyChanged("LastErrorMessage");
                OnPropertyChanged("LastErrorMessageVisibility");                
                ErrorRaised?.Invoke(this, e);
            };
        }

        public static EmulatorProcess CreateFromIniFile(string iniFilePath)
        {
            var processId = int.Parse(System.IO.Path.GetFileNameWithoutExtension(iniFilePath).Substring(4));
            var iniValues = File.ReadAllLines(iniFilePath)
                .Select(_ => _.Split('='))
                .ToDictionary(_ => _[0], _ => _[1]);

            var avdName = iniValues["avd.name"];
            var emulatorName = $"emulator-{iniValues["port.serial"]}";

            return new EmulatorProcess(processId, avdName, emulatorName);
        }

        public ICommand StartCommand { get; }
        public ICommand ShowToolBarWindowCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand ShutdownCommand { get; }

        public int ProcessId { get; }

        public string Name { get; }

        public string EmulatorName { get; }

        public WindowsFormsHost HostView { get; }

        public bool IsStarted => _viewer.IsStarted;

        public string LastErrorMessage => _viewer.LastErrorMessage;

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                SetProperty(ref _isBusy, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public Visibility LastErrorMessageVisibility 
            => !string.IsNullOrEmpty(_viewer.LastErrorMessage) ? Visibility.Visible : Visibility.Collapsed;

        public event EventHandler ProcessExited;

        public event EventHandler ErrorRaised;

        public void ShowToolWindow()
        {
            _viewer.ShowToolWindow = !_viewer.ShowToolWindow;
        }

        public void Start()
        {
            IsBusy = true;

            _viewer.Start(ProcessId, EmulatorName);

            IsBusy = false;
        }

        public async Task StopAsync()
        {
            IsBusy = true;

            await _viewer.StopAsync();

            IsBusy = false;
        }

        public async Task ShutdownAsync()
        {
            IsBusy = true;

            await _viewer.StopAsync(shutdownEmulator: true);

            IsBusy = false;
        }

    }
}
