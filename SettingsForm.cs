using System;
using System.Drawing;
using System.Windows.Forms;

namespace AdhdFocusOverlay
{
    internal sealed class SettingsForm : Form
    {
        private readonly Panel previewPanel;
        private readonly Label opacityValueLabel;
        private readonly TrackBar opacityTrackBar;
        private readonly CheckBox startupCheckBox;

        public SettingsForm()
        {
            Text = "ADHD Focus Overlay";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(320, 200);

            var tintLabel = new Label { Text = "遮罩颜色", Left = 18, Top = 20, AutoSize = true };
            previewPanel = new Panel { Left = 18, Top = 45, Width = 48, Height = 24, BorderStyle = BorderStyle.FixedSingle };

            var chooseColorButton = new Button { Text = "选择颜色", Left = 80, Top = 42, Width = 96, Height = 28 };
            chooseColorButton.Click += ChooseColorButton_Click;

            var opacityLabel = new Label { Text = "透明度", Left = 18, Top = 88, AutoSize = true };
            opacityTrackBar = new TrackBar
            {
                Left = 18,
                Top = 110,
                Width = 220,
                Minimum = 20,
                Maximum = 245,
                TickFrequency = 15,
                SmallChange = 5,
                LargeChange = 15
            };
            opacityTrackBar.Scroll += OpacityTrackBar_Scroll;

            opacityValueLabel = new Label { Left = 250, Top = 114, Width = 48 };

            startupCheckBox = new CheckBox { Text = "开机启动", Left = 18, Top = 156, AutoSize = true };
            startupCheckBox.CheckedChanged += StartupCheckBox_CheckedChanged;

            var closeButton = new Button { Text = "关闭", Left = 224, Top = 152, Width = 78, Height = 30 };
            closeButton.Click += delegate { Hide(); };

            Controls.Add(tintLabel);
            Controls.Add(previewPanel);
            Controls.Add(chooseColorButton);
            Controls.Add(opacityLabel);
            Controls.Add(opacityTrackBar);
            Controls.Add(opacityValueLabel);
            Controls.Add(startupCheckBox);
            Controls.Add(closeButton);
        }

        public event EventHandler TintColorChanged;
        public event EventHandler OpacityChanged;
        public event EventHandler StartupChanged;

        public Color TintColor
        {
            get { return previewPanel.BackColor; }
            set { previewPanel.BackColor = value; }
        }

        public int OverlayOpacity
        {
            get { return opacityTrackBar.Value; }
            set
            {
                var safeValue = Math.Max(opacityTrackBar.Minimum, Math.Min(opacityTrackBar.Maximum, value));
                opacityTrackBar.Value = safeValue;
                opacityValueLabel.Text = safeValue + "/255";
            }
        }

        public bool StartupEnabled
        {
            get { return startupCheckBox.Checked; }
            set { startupCheckBox.Checked = value; }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            base.OnFormClosing(e);
        }

        private void ChooseColorButton_Click(object sender, EventArgs e)
        {
            using (var dialog = new ColorDialog())
            {
                dialog.AllowFullOpen = true;
                dialog.FullOpen = true;
                dialog.Color = TintColor;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                TintColor = dialog.Color;
                Raise(TintColorChanged);
            }
        }

        private void OpacityTrackBar_Scroll(object sender, EventArgs e)
        {
            opacityValueLabel.Text = opacityTrackBar.Value + "/255";
            Raise(OpacityChanged);
        }

        private void StartupCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Raise(StartupChanged);
        }

        private void Raise(EventHandler handler)
        {
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
