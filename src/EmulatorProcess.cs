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

        public EmulatorProcess(int processId, string name, string emulatorName)
        {
            ProcessId = processId;
            Name = name;
            EmulatorName = emulatorName;
            StartCommand = new RelayCommand(Start);
            StopCommand = new AsyncRelayCommand(StopAsync);

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

        public int ProcessId { get; }

        public string Name { get; }
        public string EmulatorName { get; }
        public WindowsFormsHost HostView { get; }

        public bool IsStarted => _viewer.IsStarted;

        public string LastErrorMessage => _viewer.LastErrorMessage;

        public Visibility LastErrorMessageVisibility 
            => !string.IsNullOrEmpty(_viewer.LastErrorMessage) ? Visibility.Visible : Visibility.Collapsed;

        public event EventHandler ProcessExited;

        public event EventHandler ErrorRaised;

        public bool ShowToolBarWindow
        {
            get => _viewer.ShowToolWindow;
            set => _viewer.ShowToolWindow = value;
        }

        public ICommand StartCommand { get; }

        public void Start()
        {
            _viewer.Start(ProcessId, EmulatorName);
        }

        public ICommand StopCommand { get; }

        public async Task StopAsync()
        {
            await _viewer.StopAsync();
        }

    }
}
