using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AdhdFocusOverlay
{
    internal sealed class BorderForm : Form
    {
        private const int BorderThickness = 8;
        private const int GripSize = 22;

        private readonly Rectangle virtualScreen;
        private bool dragging;
        private Point dragOrigin;
        private Rectangle dragStartRect;
        private DragMode dragMode;

        public BorderForm(Rectangle initialRect, Rectangle virtualScreenBounds)
        {
            virtualScreen = virtualScreenBounds;
            FocusRect = initialRect;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = Color.Black;
            TransparencyKey = Color.Magenta;
            DoubleBuffered = true;

            UpdateBoundsFromFocusRect();
        }

        public event EventHandler FocusRectChanged;

        public Rectangle FocusRect { get; private set; }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.MakeToolWindow(Handle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Color.Magenta);

            using (var brush = new SolidBrush(Color.FromArgb(245, 255, 255, 255)))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }

            using (var brush = new SolidBrush(Color.Magenta))
            {
                e.Graphics.FillRectangle(brush, GetInnerRect());
            }

            using (var pen = new Pen(Color.FromArgb(255, 255, 255), 2f))
            {
                var outline = new Rectangle(
                    BorderThickness / 2,
                    BorderThickness / 2,
                    Width - BorderThickness,
                    Height - BorderThickness);
                e.Graphics.DrawRectangle(pen, outline);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;

            dragMode = GetDragMode(PointToScreen(e.Location));
            if (dragMode == DragMode.None) return;

            dragging = true;
            dragOrigin = Control.MousePosition;
            dragStartRect = FocusRect;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging)
            {
                var current = Control.MousePosition;
                var deltaX = current.X - dragOrigin.X;
                var deltaY = current.Y - dragOrigin.Y;
                Rectangle newRect;

                if (dragMode == DragMode.Move)
                {
                    newRect = Geometry.Move(dragStartRect, deltaX, deltaY, virtualScreen);
                }
                else
                {
                    newRect = Geometry.Resize(dragStartRect, dragMode, deltaX, deltaY, virtualScreen);
                }

                if (newRect != FocusRect)
                {
                    FocusRect = newRect;
                    UpdateBoundsFromFocusRect();
                    RaiseFocusRectChanged();
                }

                return;
            }

            Cursor = GetCursorForMode(GetDragMode(Control.MousePosition));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            dragging = false;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!dragging) Cursor = Cursors.Default;
        }

        public void SetFocusRect(Rectangle rect)
        {
            FocusRect = Geometry.ClampToScreen(rect, virtualScreen);
            UpdateBoundsFromFocusRect();
            RaiseFocusRectChanged();
        }

        private void UpdateBoundsFromFocusRect()
        {
            Bounds = new Rectangle(
                FocusRect.X - BorderThickness,
                FocusRect.Y - BorderThickness,
                FocusRect.Width + BorderThickness * 2,
                FocusRect.Height + BorderThickness * 2);

            using (var outer = new Region(new Rectangle(0, 0, Width, Height)))
            {
                outer.Exclude(GetInnerRect());
                Region = outer.Clone();
            }

            Invalidate();
        }

        private Rectangle GetInnerRect()
        {
            return new Rectangle(
                BorderThickness,
                BorderThickness,
                Math.Max(1, Width - BorderThickness * 2),
                Math.Max(1, Height - BorderThickness * 2));
        }

        private DragMode GetDragMode(Point screenPoint)
        {
            var rect = FocusRect;
            var nearLeft = Math.Abs(screenPoint.X - rect.Left) <= GripSize;
            var nearRight = Math.Abs(screenPoint.X - rect.Right) <= GripSize;
            var nearTop = Math.Abs(screenPoint.Y - rect.Top) <= GripSize;
            var nearBottom = Math.Abs(screenPoint.Y - rect.Bottom) <= GripSize;

            DragMode mode = DragMode.None;
            if (nearLeft) mode |= DragMode.Left;
            else if (nearRight) mode |= DragMode.Right;

            if (nearTop) mode |= DragMode.Top;
            else if (nearBottom) mode |= DragMode.Bottom;

            if (mode != DragMode.None) return mode;
            return DragMode.Move;
        }

        private static Cursor GetCursorForMode(DragMode mode)
        {
            if ((mode & DragMode.Left) == DragMode.Left && (mode & DragMode.Top) == DragMode.Top) return Cursors.SizeNWSE;
            if ((mode & DragMode.Right) == DragMode.Right && (mode & DragMode.Bottom) == DragMode.Bottom) return Cursors.SizeNWSE;
            if ((mode & DragMode.Right) == DragMode.Right && (mode & DragMode.Top) == DragMode.Top) return Cursors.SizeNESW;
            if ((mode & DragMode.Left) == DragMode.Left && (mode & DragMode.Bottom) == DragMode.Bottom) return Cursors.SizeNESW;
            if ((mode & (DragMode.Left | DragMode.Right)) != 0) return Cursors.SizeWE;
            if ((mode & (DragMode.Top | DragMode.Bottom)) != 0) return Cursors.SizeNS;
            return Cursors.SizeAll;
        }

        private void RaiseFocusRectChanged()
        {
            var handler = FocusRectChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
