using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace VsAndroidEm
{
    public partial class EmulatorViewer : UserControl
    {
        private Process _process;
        private IntPtr _mainWindowHandle;
        private IntPtr _childWindowHandle;
        private IntPtr _toolWindowHandle;
        private bool _toolWindowVisible;

        private bool _hosted;
        private bool _updateChildWindowSize = true;
        private Size _lastChildWindowSize;
        private Size _toolWindowSize;

        private bool _inError;

        Timer _timerUpdate;

        public EmulatorViewer()
        {
            InitializeComponent();

        }

        public void Start(int processId)
        {
            lock (this)
            {
                if (IsStarted)
                {
                    return;
                }

                _process = Process.GetProcessById(processId);

                if (_process.HasExited)
                {
                    _process = null;
                    return;
                }

                _timerUpdate = new Timer
                {
                    Interval = 1000
                };
                _timerUpdate.Tick += TimerUpdate_Tick;
                _timerUpdate.Start();
            }
        }

        public void Stop(bool closeEmulatorProcess = true)
        {
            lock (this)
            {
                if (!IsStarted)
                {
                    return;
                }

                StopCore(closeEmulatorProcess);

                _timerUpdate.Stop();
                _timerUpdate.Tick -= TimerUpdate_Tick;
                _timerUpdate.Dispose();
                _timerUpdate = null;
            }
        }

        private void StopCore(bool closeEmulatorProcess)
        {
            if (_process != null && closeEmulatorProcess)
            {
                try
                {
                    if (!_process.HasExited)
                    {
                        _process.Kill();
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

            ShowToolWindow = false;
            LastErrorMessage = null;
        }

        public bool IsStarted => _process != null;

        public string LastErrorMessage { get; private set; }

        public bool ShowToolWindow { get; set; }

        public event EventHandler ProcessExited;

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
                _mainWindowHandle = _process.MainWindowHandle;
                if (_mainWindowHandle == IntPtr.Zero)
                {
                    return;
                }
            }

            if (_childWindowHandle == IntPtr.Zero)
            {
                _childWindowHandle = Win32API.GetAllChildWindows(_mainWindowHandle)?.FirstOrDefault() ?? IntPtr.Zero;

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
                        StringBuilder className = new StringBuilder(256);

                        Win32API.GetClassName(processWindowHandle, className, className.Capacity);

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

                    Win32API.GetWindowRect(_childWindowHandle, out var childWindowRect);
                    _lastChildWindowSize = new Size(childWindowRect.Right - childWindowRect.Left, childWindowRect.Bottom - childWindowRect.Top);
                    childContainer.Width = _lastChildWindowSize.Width;
                    childContainer.Height = _lastChildWindowSize.Height;
                    childContainer.Left = Math.Max(0, (emulatorContainer.Width - childContainer.Width) / 2);
                    childContainer.Top = Math.Max(0, (emulatorContainer.Height - childContainer.Height) / 2);


                    var childStyle = (IntPtr)(Win32API.WindowStyles.WS_CHILD |
                                              // the parent cannot draw over the child's area. this is needed to avoid refresh issues
                                              Win32API.WindowStyles.WS_CLIPCHILDREN |
                                              Win32API.WindowStyles.WS_VISIBLE |
                                              Win32API.WindowStyles.WS_MAXIMIZE
                                              );

                    Win32API.SetWindowLong(_process.MainWindowHandle, Win32API.GWL_STYLE, (int)childStyle);

                    Win32API.SetParent(_process.MainWindowHandle, childContainer.Handle);


                    //setup tool window
                    Win32API.GetWindowRect(_toolWindowHandle, out var toolWindowRect);

                    _toolWindowSize = new Size(toolWindowRect.Right - toolWindowRect.Left, toolWindowRect.Bottom - toolWindowRect.Top);

                    childStyle = (IntPtr)(Win32API.WindowStyles.WS_CHILD |
                                          // the parent cannot draw over the child's area. this is needed to avoid refresh issues
                                          Win32API.WindowStyles.WS_CLIPCHILDREN |
                                          Win32API.WindowStyles.WS_VISIBLE |
                                          Win32API.WindowStyles.WS_MAXIMIZE
                                          );

                    Win32API.SetWindowLong(_toolWindowHandle, Win32API.GWL_STYLE, (int)childStyle);

                    Win32API.SetParent(_toolWindowHandle, toolContainer.Handle);
                }
                catch (Exception ex)
                {
                    _inError = true;
                    LastErrorMessage = ex.Message;
                    ErrorRaised?.Invoke(this, EventArgs.Empty);

                    return;
                }

                _hosted = true;
            }

            if (ShowToolWindow && !_toolWindowVisible)
            {
                toolContainer.Width = _toolWindowSize.Width;
                _toolWindowVisible = true;

                Win32API.SetWindowPos(_toolWindowHandle, IntPtr.Zero, 0, 0, _toolWindowSize.Width, _toolWindowSize.Height, Win32API.SetWindowPosFlags.ShowWindow);

                Win32API.UpdateWindow(_toolWindowHandle);
            }
            else if (!ShowToolWindow && _toolWindowVisible)
            {
                toolContainer.Width = 0;
                _toolWindowVisible = false;
            }

            if (_updateChildWindowSize)
            {
                try
                {

                    Win32API.SetWindowPos(_mainWindowHandle, IntPtr.Zero, 0, 0, emulatorContainer.Width, emulatorContainer.Height, Win32API.SetWindowPosFlags.ShowWindow);

                    Win32API.UpdateWindow(_process.MainWindowHandle);
                }
                catch (Exception ex)
                {
                    _inError = true;
                    LastErrorMessage = ex.Message;
                    ErrorRaised?.Invoke(this, EventArgs.Empty);
                    return;
                }

            }

            {
                try
                {
                    Win32API.GetWindowRect(_childWindowHandle, out var childWindowRect);

                    var currentChildWindowSize = new Size(childWindowRect.Right - childWindowRect.Left, childWindowRect.Bottom - childWindowRect.Top);

                    if (currentChildWindowSize != _lastChildWindowSize || _updateChildWindowSize)
                    {
                        _lastChildWindowSize = new Size(childWindowRect.Right - childWindowRect.Left, childWindowRect.Bottom - childWindowRect.Top);

                        childContainer.Width = _lastChildWindowSize.Width;
                        childContainer.Height = _lastChildWindowSize.Height;
                        childContainer.Left = Math.Max(0, (emulatorContainer.Width - childContainer.Width) / 2);
                        childContainer.Top = Math.Max(0, (emulatorContainer.Height - childContainer.Height) / 2);
                
                        _updateChildWindowSize = false;
                    }

                    _timerUpdate.Interval = 100;
                }
                catch (Exception ex)
                {
                    _inError = true;
                    LastErrorMessage = ex.Message;
                    ErrorRaised?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            _updateChildWindowSize = true;
            base.OnResize(e);
        }
    }

}
