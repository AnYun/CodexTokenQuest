namespace CodexTokenQuest.Desktop;

internal sealed class PixelScrollPanel : Panel
{
    private const int CardGap = 8;
    private const int ScrollbarWidth = 11;
    private const int WheelStep = 42;

    private int _contentHeight;
    private int _scrollOffset;
    private bool _layingOut;
    private bool _draggingThumb;
    private int _dragStartY;
    private int _dragStartOffset;

    private int MaximumOffset => Math.Max(0, _contentHeight - ClientSize.Height);
    private bool NeedsScrollbar => MaximumOffset > 0;

    internal PixelScrollPanel()
    {
        DoubleBuffered = true;
        BackColor = HudColors.Background;
        SetStyle(ControlStyles.ResizeRedraw, true);
    }

    protected override void OnControlAdded(ControlEventArgs eventArgs)
    {
        base.OnControlAdded(eventArgs);
        if (eventArgs.Control is { } control)
        {
            control.MouseWheel += ChildMouseWheel;
        }
        PerformLayout();
    }

    protected override void OnControlRemoved(ControlEventArgs eventArgs)
    {
        if (eventArgs.Control is { } control)
        {
            control.MouseWheel -= ChildMouseWheel;
        }
        base.OnControlRemoved(eventArgs);
        PerformLayout();
    }

    protected override void OnLayout(LayoutEventArgs eventArgs)
    {
        base.OnLayout(eventArgs);
        if (_layingOut)
        {
            return;
        }

        _layingOut = true;
        try
        {
            _contentHeight = Controls.Count == 0
                ? 0
                : Controls.Cast<Control>().Sum(control => control.Height) + CardGap * (Controls.Count - 1);
            _scrollOffset = Math.Clamp(_scrollOffset, 0, MaximumOffset);
            var cardWidth = Math.Max(1, ClientSize.Width - (NeedsScrollbar ? ScrollbarWidth + 6 : 0));
            var y = -_scrollOffset;
            foreach (Control control in Controls)
            {
                control.SetBounds(0, y, cardWidth, control.Height);
                y += control.Height + CardGap;
            }
        }
        finally
        {
            _layingOut = false;
        }
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs eventArgs)
    {
        base.OnMouseWheel(eventArgs);
        ScrollBy(-Math.Sign(eventArgs.Delta) * WheelStep);
    }

    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        base.OnMouseDown(eventArgs);
        if (!NeedsScrollbar || eventArgs.Button != MouseButtons.Left || !ScrollbarTrack.Contains(eventArgs.Location))
        {
            return;
        }

        var thumb = ScrollbarThumb;
        if (thumb.Contains(eventArgs.Location))
        {
            _draggingThumb = true;
            _dragStartY = eventArgs.Y;
            _dragStartOffset = _scrollOffset;
            Capture = true;
            return;
        }

        ScrollBy(eventArgs.Y < thumb.Top ? -ClientSize.Height : ClientSize.Height);
    }

    protected override void OnMouseMove(MouseEventArgs eventArgs)
    {
        base.OnMouseMove(eventArgs);
        if (!_draggingThumb)
        {
            Cursor = NeedsScrollbar && ScrollbarTrack.Contains(eventArgs.Location) ? Cursors.Hand : Cursors.Default;
            return;
        }

        var travel = Math.Max(1, ScrollbarTrack.Height - ScrollbarThumb.Height);
        var offset = _dragStartOffset + (int)Math.Round((eventArgs.Y - _dragStartY) * (double)MaximumOffset / travel);
        SetScrollOffset(offset);
    }

    protected override void OnMouseUp(MouseEventArgs eventArgs)
    {
        base.OnMouseUp(eventArgs);
        _draggingThumb = false;
        Capture = false;
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        if (!NeedsScrollbar)
        {
            return;
        }

        var graphics = eventArgs.Graphics;
        using var trackBrush = new SolidBrush(HudColors.Ink);
        using var trackPen = new Pen(HudColors.Grid);
        using var thumbBrush = new SolidBrush(HudColors.Cyan);
        using var shineBrush = new SolidBrush(HudColors.Cream);
        graphics.FillRectangle(trackBrush, ScrollbarTrack);
        graphics.DrawRectangle(trackPen, ScrollbarTrack.X, ScrollbarTrack.Y, ScrollbarTrack.Width - 1, ScrollbarTrack.Height - 1);
        graphics.FillRectangle(thumbBrush, ScrollbarThumb);
        var shine = ScrollbarThumb;
        shine.Inflate(-2, -2);
        if (shine.Width > 0 && shine.Height > 0)
        {
            graphics.FillRectangle(shineBrush, shine.X, shine.Y, 2, Math.Min(4, shine.Height));
        }
    }

    private Rectangle ScrollbarTrack => new(ClientSize.Width - ScrollbarWidth, 1, ScrollbarWidth - 2, Math.Max(1, ClientSize.Height - 2));

    private Rectangle ScrollbarThumb
    {
        get
        {
            var track = ScrollbarTrack;
            var height = Math.Max(18, (int)Math.Round(track.Height * (double)ClientSize.Height / Math.Max(ClientSize.Height, _contentHeight)));
            height = Math.Min(track.Height, height);
            var travel = Math.Max(0, track.Height - height);
            var y = MaximumOffset == 0 ? track.Y : track.Y + (int)Math.Round(travel * (double)_scrollOffset / MaximumOffset);
            return new Rectangle(track.X + 2, y, Math.Max(3, track.Width - 4), height);
        }
    }

    private void ChildMouseWheel(object? sender, MouseEventArgs eventArgs) =>
        ScrollBy(-Math.Sign(eventArgs.Delta) * WheelStep);

    private void ScrollBy(int delta) => SetScrollOffset(_scrollOffset + delta);

    private void SetScrollOffset(int offset)
    {
        var next = Math.Clamp(offset, 0, MaximumOffset);
        if (next == _scrollOffset)
        {
            return;
        }

        _scrollOffset = next;
        PerformLayout();
    }
}
