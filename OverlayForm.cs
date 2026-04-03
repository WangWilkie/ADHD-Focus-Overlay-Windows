using System.Drawing;
using System.Windows.Forms;

namespace AdhdFocusOverlay
{
    internal sealed class OverlayForm : Form
    {
        public OverlayForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.Gray;
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void OnHandleCreated(System.EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.MakeOverlayWindow(Handle);
        }

        public void ApplyStyle(Color tintColor, int opacity)
        {
            BackColor = tintColor;
            Opacity = opacity / 255.0;
        }
    }
}
