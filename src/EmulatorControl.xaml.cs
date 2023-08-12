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

            //_processes.Add(new EmulatorProcess(123, "Emu1", "Emu1"));
            //_processes.Add(new EmulatorProcess(123, "Emu2", "Emu2"));
            //_processes.Add(new EmulatorProcess(123, "Emu3", "Emu3"));
            //toolbar.Visibility = _processes.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            tabs.ItemsSource = _processes;
            tabs.SelectionChanged += (s, e) =>
            {
                hostViewPresenter.Content = (tabs.SelectedItem as EmulatorProcess)?.HostView;
                toolbar.Visibility = hostViewPresenter.Content == null ? Visibility.Collapsed : Visibility.Visible;
            };

            //ThemedDialogColors
            //VsBrushes.ButtonTextKey
            //VsResourceKeys.

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

                try
                {
                    var newEmulatorDetected = EmulatorProcess.CreateFromIniFile(file);

                    newEmulatorDetected.ProcessExited += Emulator_ProcessExited;
                    newEmulatorDetected.ErrorRaised += Emulator_ErrorRaised;

                    newEmulatorDetected.Start();

                    _processes.Add(newEmulatorDetected);

                    tabs.SelectedItem = newEmulatorDetected;
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }

            }
        }

        private void Emulator_ErrorRaised(object sender, EventArgs e)
        {

        }

        private async void Emulator_ProcessExited(object sender, EventArgs e)
        {
            try
            {
                var emulatorProcess = (EmulatorProcess)sender;
                await emulatorProcess.StopAsync();

                _processes.Remove(emulatorProcess);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
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

        internal async void Stop()
        {
            try
            {
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
}
