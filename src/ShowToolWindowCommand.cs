using Community.VisualStudio.Toolkit;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsAndroidEm;

[Command(PackageIds.ShowToolWindowCommand)]
internal class ShowToolWindowCommand : BaseCommand<ShowToolWindowCommand>
{
    protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
    {
        await EmulatorWindow.ShowAsync();
    }
}
