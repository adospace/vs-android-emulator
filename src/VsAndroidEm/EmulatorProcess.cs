using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.Integration;

namespace VsAndroidEm
{
    public class EmulatorProcess
    {
        private readonly EmulatorViewer _viewer = new();

        public EmulatorProcess(int processId, string name)
        {
            ProcessId = processId;
            Name = name;

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

        public void Start()
        {
            _viewer.Start(ProcessId);
        }

        public void Stop()
        {
            _viewer.Stop();
        }
    }
}
