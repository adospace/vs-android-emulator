using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VsAndroidEm;

class EmulatorInfo
{
    public int EmulatorProcessId { get; set; }

    public int VisualStudioProcessId { get; set; }

    public long MainWindowHandle { get; set; }

    public long ChildWindowHandle { get; set; }

    public long ToolWindowHandle { get; set; }

}

class EmulatorMonitor
{
    private static readonly Mutex _sharedLock = new(false, nameof(VsAndroidEm));

    public EmulatorInfo GetExistingEmulatorInfo(int emulatorProcessId)
    {
        try
        {
            if (!_sharedLock.WaitOne(1000))
            {
                return null;
            }

            var sharedFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "_emulatorMonitor.info");

            if (!File.Exists(sharedFilePath))
            {
                return null;
            }

            var infos = JsonConvert.DeserializeObject<EmulatorInfo[]>(File.ReadAllText(sharedFilePath));

            if (infos == null)
            {
                return null;
            }

            return infos.FirstOrDefault(_ => _.EmulatorProcessId == emulatorProcessId);

        }
        finally 
        {
            _sharedLock.ReleaseMutex();
        }
    }

    public void SaveEmulatorInfo(EmulatorInfo info)
    {
        try
        {
            if (!_sharedLock.WaitOne(1000))
            {
                return;
            }

            var sharedFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "_emulatorMonitor.info");

            var infos = new List<EmulatorInfo>();

            if (File.Exists(sharedFilePath))
            {
                infos = JsonConvert.DeserializeObject<EmulatorInfo[]>(File.ReadAllText(sharedFilePath))?.ToList() ?? new List<EmulatorInfo>();
            }

            infos.RemoveAll(_ => _.EmulatorProcessId == info.EmulatorProcessId);
            infos.Add(info);

            var json = JsonConvert.SerializeObject(infos);

            File.WriteAllText(sharedFilePath, json);
        }
        finally
        {
            _sharedLock.ReleaseMutex();
        }
    }
}
