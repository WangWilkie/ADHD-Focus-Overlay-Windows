using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AdhdFocusOverlay
{
    internal sealed class OverlayAppContext : ApplicationContext
    {
        private const string StartupValueName = "ADHDFocusOverlay";

        private readonly Rectangle virtualScreen;
        private readonly AppState state;
        private readonly List<OverlayForm> overlays;
        private readonly BorderForm borderForm;
        private readonly SettingsForm settingsForm;
        private readonly NotifyIcon notifyIcon;

        public OverlayAppContext()
        {
            virtualScreen = SystemInformation.VirtualScreen;
            state = AppState.Load(virtualScreen);
            state.StartupEnabled = GetStartupEnabled();

            overlays = new List<OverlayForm>
            {
                new OverlayForm(),
                new OverlayForm(),
                new OverlayForm(),
                new OverlayForm()
            };

            borderForm = new BorderForm(state.FocusRect, virtualScreen);
            borderForm.FocusRectChanged += BorderForm_FocusRectChanged;

            settingsForm = new SettingsForm();
            settingsForm.TintColor = state.TintColor;
            settingsForm.OverlayOpacity = state.OverlayOpacity;
            settingsForm.StartupEnabled = state.StartupEnabled;
            settingsForm.TintColorChanged += SettingsForm_TintColorChanged;
            settingsForm.OpacityChanged += SettingsForm_OpacityChanged;
            settingsForm.StartupChanged += SettingsForm_StartupChanged;

            var menu = new ContextMenuStrip();
            menu.Items.Add("显示设置", null, delegate { ShowSettings(); });
            menu.Items.Add("退出", null, delegate { ExitThread(); });

            notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "ADHD Focus Overlay",
                Visible = true,
                ContextMenuStrip = menu
            };
            notifyIcon.DoubleClick += delegate { ShowSettings(); };

            ApplyOverlayStyle();
            UpdateOverlayLayout();

            foreach (var overlay in overlays) overlay.Show();
            borderForm.Show();
        }

        protected override void ExitThreadCore()
        {
            state.Save();

            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            settingsForm.Dispose();
            borderForm.Dispose();
            foreach (var overlay in overlays) overlay.Dispose();

            base.ExitThreadCore();
        }

        private void BorderForm_FocusRectChanged(object sender, EventArgs e)
        {
            state.FocusRect = borderForm.FocusRect;
            UpdateOverlayLayout();
            state.Save();
        }

        private void SettingsForm_TintColorChanged(object sender, EventArgs e)
        {
            state.TintColor = settingsForm.TintColor;
            ApplyOverlayStyle();
            state.Save();
        }

        private void SettingsForm_OpacityChanged(object sender, EventArgs e)
        {
            state.OverlayOpacity = settingsForm.OverlayOpacity;
            ApplyOverlayStyle();
            state.Save();
        }

        private void SettingsForm_StartupChanged(object sender, EventArgs e)
        {
            state.StartupEnabled = settingsForm.StartupEnabled;
            SetStartupEnabled(state.StartupEnabled);
            state.Save();
        }

        private void ApplyOverlayStyle()
        {
            foreach (var overlay in overlays) overlay.ApplyStyle(state.TintColor, state.OverlayOpacity);
        }

        private void UpdateOverlayLayout()
        {
            var focusRect = Geometry.ClampToScreen(state.FocusRect, virtualScreen);
            state.FocusRect = focusRect;

            var leftRect = Rectangle.FromLTRB(virtualScreen.Left, virtualScreen.Top, focusRect.Left, virtualScreen.Bottom);
            var topRect = Rectangle.FromLTRB(focusRect.Left, virtualScreen.Top, focusRect.Right, focusRect.Top);
            var rightRect = Rectangle.FromLTRB(focusRect.Right, virtualScreen.Top, virtualScreen.Right, virtualScreen.Bottom);
            var bottomRect = Rectangle.FromLTRB(focusRect.Left, focusRect.Bottom, focusRect.Right, virtualScreen.Bottom);

            ApplyOverlayBounds(overlays[0], leftRect);
            ApplyOverlayBounds(overlays[1], topRect);
            ApplyOverlayBounds(overlays[2], rightRect);
            ApplyOverlayBounds(overlays[3], bottomRect);

            if (borderForm.FocusRect != focusRect) borderForm.SetFocusRect(focusRect);
        }

        private static void ApplyOverlayBounds(OverlayForm form, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                form.Hide();
                return;
            }

            form.Bounds = rect;
            if (!form.Visible) form.Show();
        }

        private void ShowSettings()
        {
            if (!settingsForm.Visible) settingsForm.Show();
            if (settingsForm.WindowState == FormWindowState.Minimized) settingsForm.WindowState = FormWindowState.Normal;
            settingsForm.BringToFront();
            settingsForm.Activate();
        }

        private static bool GetStartupEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false))
            {
                if (key == null) return false;
                return key.GetValue(StartupValueName) != null;
            }
        }

        private static void SetStartupEnabled(bool enabled)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                if (key == null) return;

                if (enabled)
                {
                    key.SetValue(StartupValueName, "\"" + Application.ExecutablePath + "\"");
                }
                else
                {
                    key.DeleteValue(StartupValueName, false);
                }
            }
        }
    }
}
