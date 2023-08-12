using Community.VisualStudio.Toolkit;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace VsAndroidEm
{
    public class EmulatorWindow : BaseToolWindow<EmulatorWindow>
    {
        private Pane _pane;
        private EmulatorControl _emulatorControl;

        public override string GetTitle(int toolWindowId) => "Android Emulator Previewer";

        public override Type PaneType => typeof(Pane);

        public override Task<FrameworkElement> CreateAsync(int toolWindowId, CancellationToken cancellationToken)
        {
            return Task.FromResult<FrameworkElement>(_emulatorControl = new EmulatorControl());
        }

        public override void SetPane(ToolWindowPane pane, int toolWindowId)
        {
            _pane = (Pane)pane;
            _pane.Closing += Pane_Closing;
            base.SetPane(pane, toolWindowId);
        }

        private void Pane_Closing(object sender, EventArgs e)
        {
            this._emulatorControl.Stop();
        }

        [Guid("43F4976D-7ACE-49B8-A177-991E323B74E9")]
        internal class Pane : ToolkitToolWindowPane
        {
            public Pane()
            {
                BitmapImageMoniker = KnownMonikers.ToolWindow;
            }

            public event EventHandler Closing;

            protected override void OnClose()
            {
                Closing?.Invoke(this, EventArgs.Empty);
                base.OnClose();
            }
        }
    }
}
