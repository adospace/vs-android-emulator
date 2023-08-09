using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace VsAndroidEm
{
    /// <summary>
    /// Interaction logic for EmulatorControl.xaml
    /// </summary>
    public partial class EmulatorControl : UserControl
    {
        public ObservableCollection<EmulatorProcess> _processes = new ObservableCollection<EmulatorProcess>();
        private DispatcherTimer _timer;

        public EmulatorControl()
        {
            InitializeComponent();

            
            tabs.ItemsSource = _processes;

            //ThemedDialogColors

            this.Loaded += MainWindow_Loaded;
            
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var avdRunningFolderFiles = Directory.GetFiles(System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "temp", "avd", "running"), "*.ini");

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

                var newEmulatorDetected = EmulatorProcess.CreateFromIniFile(file);

                newEmulatorDetected.ProcessExited += Emulator_ProcessExited;
                newEmulatorDetected.ErrorRaised += Emulator_ErrorRaised;

                _processes.Add(newEmulatorDetected);

                newEmulatorDetected.Start();

                tabs.SelectedItem = newEmulatorDetected;
            }
        }

        private void Emulator_ErrorRaised(object sender, EventArgs e)
        {
            var emulatorProcess = (EmulatorProcess)sender;
            emulatorProcess.Stop();

            _processes.Remove(emulatorProcess);
        }

        private void Emulator_ProcessExited(object sender, EventArgs e)
        {
            var emulatorProcess = (EmulatorProcess)sender;
            emulatorProcess.Stop();

            _processes.Remove(emulatorProcess);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        internal void Stop()
        {
            _timer.Stop();

            foreach (var process in _processes.ToArray())
            {
                process.Stop();

                var avdRunningFolderFiles = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "temp", "avd", "running");

                var processIniFile = System.IO.Path.Combine(avdRunningFolderFiles, $"pid_{process.ProcessId}.ini");

                File.Delete(processIniFile);
            }
        }
    }
}
