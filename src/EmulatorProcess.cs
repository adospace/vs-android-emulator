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
using System.Windows.Threading;

namespace VsAndroidEm
{
    public class EmulatorProcess : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly EmulatorViewer _viewer = new();
        private bool _isBusy;

        public EmulatorProcess(string name)
        {
            Name = name;
            StartCommand = new RelayCommand(Start, () => !IsRunning && !CanBeAttached);
            StopCommand = new AsyncRelayCommand(StopAsync, () => IsRunning && !CanBeAttached);
            ShowToolBarWindowCommand = new RelayCommand(ShowToolWindow, () => IsRunning && !CanBeAttached);
            ShutdownCommand = new AsyncRelayCommand(ShutdownAsync, () => IsRunning && !CanBeAttached);
            ForceAttachmentCommand = new RelayCommand(ForceAttachment, () => true);

            HostView = new WindowsFormsHost
            {
                Child = _viewer,
            };

            _viewer.ProcessAttached += (s, e) =>
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                {
                    OnPropertyChanged(nameof(IsStarted));
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(FormatName));
                    OnPropertyChanged(nameof(IsReadyToAcceptCommand));

                    CommandManager.InvalidateRequerySuggested();

                    ProcessAttached?.Invoke(this, e);
                });
            };

            _viewer.ProcessExited += (s, e) =>
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                {
                    OnPropertyChanged(nameof(IsStarted));
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(FormatName));
                    OnPropertyChanged(nameof(IsReadyToAcceptCommand));

                    CommandManager.InvalidateRequerySuggested();

                    ProcessExited?.Invoke(this, e);
                });
            };

            _viewer.ErrorRaised += (s, e) =>
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                {
                    OnPropertyChanged(nameof(IsStarted));
                    OnPropertyChanged(nameof(FormatName));
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(IsReadyToAcceptCommand));
                    OnPropertyChanged(nameof(LastErrorMessage));
                    OnPropertyChanged(nameof(LastErrorMessageVisibility));

                    CommandManager.InvalidateRequerySuggested();

                    ErrorRaised?.Invoke(this, e);
                });
            };

            _viewer.CanBeAttachedChanged += (s, e) =>
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(() =>
                {
                    OnPropertyChanged(nameof(CanBeAttached));

                    CommandManager.InvalidateRequerySuggested();
                });
            };
        }
        public ICommand StartCommand { get; }

        public ICommand ShowToolBarWindowCommand { get; }

        public ICommand StopCommand { get; }

        public ICommand ShutdownCommand { get; }

        public ICommand ForceAttachmentCommand { get; }

        public int ProcessId { get; private set; }

        public string Name { get; }

        public string FormatName => $"{Name}{(IsStarted ? "(Running)" : string.Empty)}";

        public WindowsFormsHost HostView { get; }

        public bool IsStarted => _viewer.IsStarted;

        public bool IsReadyToAcceptCommand => _viewer.IsReadyToAcceptCommand;

        public bool IsRunning => !IsBusy && IsStarted && IsReadyToAcceptCommand;

        public string LastErrorMessage => _viewer.LastErrorMessage;

        public bool CanBeAttached => _viewer.CanBeAttached;

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
        
        public event EventHandler ProcessAttached;

        public event EventHandler ErrorRaised;

        public void ShowToolWindow()
        {
            _viewer.ShowToolWindow = !_viewer.ShowToolWindow;
        }

        public void Monitor(int processId, string emulatorName)
        {
            IsBusy = true;
            ProcessId = processId;

            _viewer.Start(processId, emulatorName);

            IsBusy = false;
        }

        public void Start()
        {
            EmulatorCLI.RunEmulator(Name);
        }


        public async Task StopAsync()
        {
            IsBusy = true;

            await _viewer.StopAsync();

            ProcessId = 0;

            IsBusy = false;
        }

        public async Task ShutdownAsync()
        {
            IsBusy = true;

            await _viewer.StopAsync(shutdownEmulator: true);

            IsBusy = false;
        }

        private void ForceAttachment()
        {
            _viewer.ForceAttachment = true;
        }
    }
}
