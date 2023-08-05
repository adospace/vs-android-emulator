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
using System.Windows.Forms.Integration;
using System.Windows.Input;

namespace VsAndroidEm
{
    public class EmulatorProcess : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly EmulatorViewer _viewer = new();

        public EmulatorProcess(int processId, string name)
        {
            ProcessId = processId;
            Name = name;
            StartCommand = new RelayCommand(Start);
            StopCommand = new RelayCommand(Stop);

            HostView = new WindowsFormsHost
            {
                Child = _viewer,
            };

            _viewer.ProcessExited += (s, e) => ProcessExited?.Invoke(this, e);
            _viewer.ErrorRaised += (s, e) => ErrorRaised?.Invoke(this, e);
        }

        public static EmulatorProcess CreateFromIniFile(string iniFilePath)
        {
            var processId = int.Parse(System.IO.Path.GetFileNameWithoutExtension(iniFilePath).Substring(4));
            var avdName = File.ReadAllLines(iniFilePath)
                .Select(_ => _.Split('='))
                .First(_ => _[0] == "avd.name")
                [1];

            return new EmulatorProcess(processId, avdName);
        }

        public int ProcessId { get; }

        public string Name { get; }

        public WindowsFormsHost HostView { get; }

        public bool IsStarted => _viewer.IsStarted;

        public string LastErrorMessage => _viewer.LastErrorMessage;


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
            _viewer.Start(ProcessId);
        }

        public ICommand StopCommand { get; }

        public void Stop()
        {
            _viewer.Stop();
        }

    }
}
