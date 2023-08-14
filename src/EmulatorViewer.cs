using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.RpcContracts;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace VsAndroidEm
{
    public partial class EmulatorViewer : UserControl
    {
        private string _emulatorName;
        private Process _process;
        private IntPtr _mainWindowHandle;
        private IntPtr _childWindowHandle;
        private IntPtr _toolWindowHandle;
        private bool _toolWindowVisible;

        private bool _hosted;
        private bool _updateChildWindowSize = true;
        private Size _lastChildWindowSize;
        private Size _toolWindowSize;
        private Size _emulatorFrameSize;

        private bool _inError;

        private static readonly SemaphoreSlim _lockSemaphore = new SemaphoreSlim(1, 1);

        System.Windows.Forms.Timer _timerUpdate;

        public EmulatorViewer()
        {
            InitializeComponent();

        }

        public void Start(int processId, string emulatorName)
        {
            lock (this)
            {
                if (IsStarted)
                {
                    return;
                }

                _emulatorName = emulatorName;
                _process = Process.GetProcessById(processId);

                if (_process.HasExited)
                {
                    _process = null;
                    return;
                }

                _timerUpdate = new System.Windows.Forms.Timer
                {
                    Interval = 500
                };
                _timerUpdate.Tick += TimerUpdate_Tick;
                _timerUpdate.Start();
            }
        }

        public async Task StopAsync(bool closeEmulatorProcess = true, bool shutdownEmulator = false)
        {
            try
            {
                await _lockSemaphore.WaitAsync();

                if (!IsStarted)
                {
                    return;
                }

                await StopCoreAsync(closeEmulatorProcess, shutdownEmulator);

                if (_timerUpdate != null)
                {
                    _timerUpdate.Stop();
                    _timerUpdate.Tick -= TimerUpdate_Tick;
                    _timerUpdate.Dispose();
                    _timerUpdate = null;
                }
            }
            finally
            {
                _lockSemaphore.Release();
            }
        }

        private async Task<bool> WaitForProcessExitAsync(int milliseconds)
        {
            Task waitForExitTask = Task.Run(() => _process.WaitForExit());

            Task delayTask = Task.Delay(milliseconds);

            Task completedTask = await Task.WhenAny(waitForExitTask, delayTask);

            return (completedTask == waitForExitTask);
        }

        private async Task StopCoreAsync(bool closeEmulatorProcess, bool shutdownEmulator = false)
        {
            if (_process != null && closeEmulatorProcess)
            {
                // Command to gracefully close the emulator
                if (!_process.HasExited &&
                    (shutdownEmulator ? await AdbCLI.ShutdownEmulatorAsync(_emulatorName) : await AdbCLI.StopEmulatorAsync(_emulatorName)))
                {
                    if (await WaitForProcessExitAsync(5000))
                    {
                        _process = null;
                        ProcessExited?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            if (_process != null && closeEmulatorProcess)
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                        _process = null;
                        ProcessExited?.Invoke(this, EventArgs.Empty);
                    }
                }
                catch (Exception)
                {

                }
            }

            _process = null;
            _mainWindowHandle = IntPtr.Zero;
            _childWindowHandle = IntPtr.Zero;
            _hosted = false;
            _inError = false;
            _updateChildWindowSize = false;
            _toolWindowHandle = IntPtr.Zero;
            _toolWindowVisible = false;
            _emulatorFrameSize = default;
            _lastChildWindowSize = default;

            ShowToolWindow = false;
            LastErrorMessage = null;
        }

        public bool IsStarted => _process != null;

        public string LastErrorMessage { get; private set; }

        public bool ShowToolWindow { get; set; }

        public bool IsReadyToAcceptCommand => _process == null || _hosted;

        public event EventHandler ProcessExited;

        public event EventHandler ProcessAttached;

        public event EventHandler ErrorRaised;

        private void TimerUpdate_Tick(object sender, EventArgs e)
        {
            lock (this)
            {
                UpdateInternals();
            }
        }

        private void UpdateInternals()
        {
            if (_inError)
            {
                return;
            }

            if (!IsStarted)
            {
                return;
            }

            if (_process.HasExited)
            {
                ProcessExited?.Invoke(this, EventArgs.Empty);

                return;
            }

            if (_mainWindowHandle == IntPtr.Zero)
            {
                if (!Win32API.IsWindowVisible(_process.MainWindowHandle) ||
                    !Win32API.GetWindowRect(_process.MainWindowHandle, out var _))
                {
                    //main window not ready
                    return;
                }

                _mainWindowHandle = _process.MainWindowHandle;
                if (_mainWindowHandle == IntPtr.Zero)
                {
                    return;
                }
            }

            if (_childWindowHandle == IntPtr.Zero)
            {
                _childWindowHandle = Win32API.GetAllChildWindows(_mainWindowHandle)?.FirstOrDefault() ?? IntPtr.Zero;

                if (!Win32API.GetWindowRect(_childWindowHandle, out var childWindowRect))
                {
                    _childWindowHandle = IntPtr.Zero;
                }

                if (_childWindowHandle == IntPtr.Zero)
                {
                    return;
                }
            }

            if (!_hosted)
            {
                try
                {
                    var allProcessWindows = Win32API.EnumerateProcessWindowHandles(_process.Id)
                        .Where(_ => _ != _process.MainWindowHandle)
                        .ToList();

                    foreach (var processWindowHandle in allProcessWindows)
                    {
                        StringBuilder className = new(256);

                        if (0 == Win32API.GetClassName(processWindowHandle, className, className.Capacity))
                        {
                            throw new InvalidOperationException($"Unable to get window class name ({GetLastErrorMessage()})");
                        }

                        if (className.ToString() == "Qt5QWindowToolSaveBits")
                        {
                            _toolWindowHandle = processWindowHandle;
                            break;
                        }
                    }

                    allProcessWindows
                        .Where(_ => _ != _toolWindowHandle)
                        .ToList()
                        .ForEach(handle => Win32API.ShowWindow(handle, Win32API.SW_HIDE));

                    if (!Win32API.GetWindowRect(_childWindowHandle, out var childWindowRect))
                    {
                        _childWindowHandle = IntPtr.Zero;
                        return;
                        //throw new InvalidOperationException($"Unable to get initial child window rect ({GetLastErrorMessage()})");
                    }

                    _lastChildWindowSize = new Size(childWindowRect.Right - childWindowRect.Left, childWindowRect.Bottom - childWindowRect.Top);

                    childContainer.Width = _lastChildWindowSize.Width;
                    childContainer.Height = _lastChildWindowSize.Height;
                    childContainer.Left = Math.Max(0, (emulatorContainer.Width - childContainer.Width) / 2);
                    childContainer.Top = Math.Max(0, (emulatorContainer.Height - childContainer.Height) / 2);

                    var currentStyle = Win32API.GetWindowLong(_process.MainWindowHandle, Win32API.GWL_STYLE);

                    if ((currentStyle & (int)Win32API.WindowStyles.WS_CHILD) > 0)
                    {
                        throw new InvalidOperationException("Emulator window already hosted");
                    }

                    var childStyle = (IntPtr)(Win32API.WindowStyles.WS_CHILD |
                                              // the parent cannot draw over the child's area. this is needed to avoid refresh issues
                                              Win32API.WindowStyles.WS_CLIPCHILDREN |
                                              Win32API.WindowStyles.WS_VISIBLE |
                                              Win32API.WindowStyles.WS_MAXIMIZE
                                              );

                    if (0 == Win32API.SetWindowLong(_process.MainWindowHandle, Win32API.GWL_STYLE, (int)childStyle))
                    {
                        throw new InvalidOperationException($"Unable to set main window style ({GetLastErrorMessage()})");
                    }

                    if (IntPtr.Zero == Win32API.SetParent(_process.MainWindowHandle, childContainer.Handle))
                    {
                        throw new InvalidOperationException($"Unable to relocate emulator main window ({GetLastErrorMessage()})");
                    }

                    //setup tool window
                    if (!Win32API.GetWindowRect(_toolWindowHandle, out var toolWindowRect))
                    {
                        throw new InvalidOperationException($"Unable to get tool window rect ({GetLastErrorMessage()})");
                    }

                    _toolWindowSize = new Size(toolWindowRect.Right - toolWindowRect.Left, toolWindowRect.Bottom - toolWindowRect.Top);

                    childStyle = (IntPtr)(Win32API.WindowStyles.WS_CHILD |
                                          // the parent cannot draw over the child's area. this is needed to avoid refresh issues
                                          Win32API.WindowStyles.WS_CLIPCHILDREN |
                                          Win32API.WindowStyles.WS_VISIBLE |
                                          Win32API.WindowStyles.WS_MAXIMIZE
                                          );

                    if (0 == Win32API.SetWindowLong(_toolWindowHandle, Win32API.GWL_STYLE, (int)childStyle))
                    {
                        throw new InvalidOperationException($"Unable to set tool window style ({GetLastErrorMessage()})");
                    }

                    if (IntPtr.Zero == Win32API.SetParent(_toolWindowHandle, toolContainer.Handle))
                    {
                        throw new InvalidOperationException($"Unable to relocate emulator tool window ({GetLastErrorMessage()})");
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{_emulatorName}] Exception: {ex}");

                    _inError = true;
                    LastErrorMessage = ex.Message;
                    ErrorRaised?.Invoke(this, EventArgs.Empty);

                    return;
                }

                Debug.WriteLine($"[{_emulatorName}] MainWindow attached");

                ProcessAttached?.Invoke(this, EventArgs.Empty);

                _hosted = true;
            }

            if (_toolWindowVisible)
            {
                try
                {
                    if (!Win32API.GetWindowRect(_toolWindowHandle, out var toolWindowRect))
                    {
                        throw new InvalidOperationException("Unable to get tool window rect");
                    }

                    if (toolWindowRect.Left != 0 || toolWindowRect.Right != 0)
                    {
                        Debug.WriteLine("Win32API.SetWindowPos(_toolWindowHandle)");

                        Win32API.SetWindowPos(_toolWindowHandle, IntPtr.Zero, 0, 0, _toolWindowSize.Width, _toolWindowSize.Height, Win32API.SetWindowPosFlags.ShowWindow);

                        Win32API.UpdateWindow(_toolWindowHandle);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{_emulatorName}] Exception: {ex}");

                    _inError = true;
                    LastErrorMessage = ex.Message;
                    ErrorRaised?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }

            if (ShowToolWindow && !_toolWindowVisible)
            {
                toolContainer.Width = _toolWindowSize.Width;
                _toolWindowVisible = true;
                _updateChildWindowSize = true;
            }
            else if (!ShowToolWindow && _toolWindowVisible)
            {
                toolContainer.Width = 0;
                _toolWindowVisible = false;
                _updateChildWindowSize = true;
            }

            if (_updateChildWindowSize)
            {
                try
                {
                    if (!Win32API.SetWindowPos(_mainWindowHandle, IntPtr.Zero, 0, 0, emulatorContainer.Width + _emulatorFrameSize.Width, emulatorContainer.Height + _emulatorFrameSize.Height, Win32API.SetWindowPosFlags.ShowWindow))
                    {
                        throw new InvalidOperationException($"Unable to set main window position ({GetLastErrorMessage()})");
                    }

                    if (!Win32API.UpdateWindow(_process.MainWindowHandle))
                    {
                        throw new InvalidOperationException($"Unable to update main window ({GetLastErrorMessage()})");
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{_emulatorName}] Exception: {ex}");

                    _inError = true;
                    LastErrorMessage = ex.Message;
                    ErrorRaised?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }

            {
                try
                {
                    if (Win32API.GetWindowRect(_childWindowHandle, out var childWindowRect) &&
                        Win32API.GetWindowRect(_mainWindowHandle, out var mainWindowWindowRect))
                    {
                        var currentChildWindowSize = new Size(childWindowRect.Right - childWindowRect.Left, childWindowRect.Bottom - childWindowRect.Top);

                        if (currentChildWindowSize != _lastChildWindowSize || _updateChildWindowSize)
                        {
                            _lastChildWindowSize = new Size(childWindowRect.Right - childWindowRect.Left, childWindowRect.Bottom - childWindowRect.Top);

                            childContainer.Width = _lastChildWindowSize.Width;
                            childContainer.Height = _lastChildWindowSize.Height;
                            childContainer.Left = Math.Max(0, (emulatorContainer.Width - childContainer.Width) / 2);
                            childContainer.Top = Math.Max(0, (emulatorContainer.Height - childContainer.Height) / 2);

                            _emulatorFrameSize = new Size(childWindowRect.Left - mainWindowWindowRect.Left, childWindowRect.Top - mainWindowWindowRect.Top);

                            //childInternalContainer.Left = -_emulatorFrameSize.Width;
                            //childInternalContainer.Top = -_emulatorFrameSize.Height;
                            //childInternalContainer.Width = -childInternalContainer.Left + childContainer.Width;
                            //childInternalContainer.Height = -childInternalContainer.Top + childContainer.Height;

                            Debug.WriteLine($"[{_emulatorName}] MainWindow position adjusted");

                            Win32API.ShowWindow(_process.MainWindowHandle, Win32API.SW_SHOW);
                            Win32API.ShowWindow(_toolWindowHandle, Win32API.SW_SHOW);

                            _updateChildWindowSize = false;

                            ProcessAttached?.Invoke(this, EventArgs.Empty);
                        }

                        _timerUpdate.Interval = 100;
                    }
                    else
                    {
                        _childWindowHandle = IntPtr.Zero;
                        _updateChildWindowSize = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{_emulatorName}] Exception: {ex}");

                    _inError = true;
                    LastErrorMessage = ex.Message;
                    ErrorRaised?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }
        }

        private string GetLastErrorMessage()
        {
            uint errorCode = Win32API.GetLastError();

            if (errorCode == 0)
            {
                return string.Empty;
            }

            StringBuilder messageBuffer = new(512);
            Win32API.FormatMessage(Win32API.FORMAT_MESSAGE_FROM_SYSTEM | Win32API.FORMAT_MESSAGE_IGNORE_INSERTS,
                IntPtr.Zero, errorCode, 0, messageBuffer, (uint)messageBuffer.Capacity, IntPtr.Zero);

            return $"Error Code: {errorCode} - {messageBuffer.ToString().TrimEnd('\r', '\n')}";
        }

        protected override void OnResize(EventArgs e)
        {
            _updateChildWindowSize = true;
            base.OnResize(e);
        }
    }

}
