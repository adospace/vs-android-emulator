using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public string AvdName { get; set; }

    public int EmulatorProcessId { get; set; }

    public int VisualStudioProcessId { get; set; }

    public long MainWindowHandle { get; set; }

    public long ChildWindowHandle { get; set; }

    public long ToolWindowHandle { get; set; }

}

class EmulatorMonitor
{
    private static readonly Mutex _sharedLock = new(false, nameof(VsAndroidEm));

    public EmulatorInfo GetExistingEmulatorInfo(string avdName)
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

            var infos = JsonConvert.DeserializeObject<List<EmulatorInfo>>(File.ReadAllText(sharedFilePath));

            if (infos == null)
            {
                return null;
            }

            bool infoMustBeSavedBack = false;
            foreach (var info in infos.ToArray())
            {
                if (string.IsNullOrEmpty(info.AvdName))
                {
                    infoMustBeSavedBack = true;
                    infos.Remove(info);
                    continue;
                }

                if (info.EmulatorProcessId != 0)
                {
                    if (!Win32API.CheckProcessIsRunning(info.EmulatorProcessId))
                    {
                        infos.Remove(info);
                        infoMustBeSavedBack = true;
                    };
                }

                if (info.VisualStudioProcessId != 0)
                {
                    if (!Win32API.CheckProcessIsRunning(info.VisualStudioProcessId))
                    {
                        infos.Remove(info);
                        infoMustBeSavedBack = true;
                    };
                }
            }

            var foundEmulatorInfoWithProcessId = infos.FirstOrDefault(_ => _.AvdName == avdName && _.EmulatorProcessId != 0);
            var foundEmulatorInfo = infos.FirstOrDefault(_ => _.AvdName == avdName && _.EmulatorProcessId == 0);

            if (foundEmulatorInfo != null && foundEmulatorInfoWithProcessId != null)
            {
                infos.Remove(foundEmulatorInfo);
                infoMustBeSavedBack = true;
            }

            if (infoMustBeSavedBack)
            {
                var json = JsonConvert.SerializeObject(infos);

                File.WriteAllText(sharedFilePath, json);
            }

            return foundEmulatorInfoWithProcessId ?? foundEmulatorInfo;

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

            infos.RemoveAll(_ => _.AvdName == info.AvdName || string.IsNullOrEmpty(_.AvdName));
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
