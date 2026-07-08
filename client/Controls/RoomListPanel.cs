namespace SecureChat.Controls;

/// <summary>
/// Scrollable room list — no native scrollbar, mouse-wheel only.
/// Shadows Controls/SuspendLayout/ResumeLayout so MainForm code needs zero changes.
/// </summary>
public class RoomListPanel : Panel
{
    private readonly FlowLayoutPanel _flow;

    private int MaxScroll => Math.Max(0, _flow.Height - ClientSize.Height);

    public RoomListPanel()
    {
        AutoScroll     = false;
        DoubleBuffered = true;

        _flow = new FlowLayoutPanel
        {
            FlowDirection  = FlowDirection.TopDown,
            WrapContents   = false,
            AutoScroll     = false,
            AutoSize       = true,
            AutoSizeMode   = AutoSizeMode.GrowAndShrink,
            BackColor      = Color.White,
            Padding        = new Padding(0),
            Location       = Point.Empty
        };

        base.Controls.Add(_flow);

        SizeChanged      += (_, _) => { _flow.Width = ClientSize.Width; Clamp(); };
        MouseWheel       += Scroll;
        _flow.MouseWheel += Scroll;

        // Forward wheel from each room item so hovering over items also scrolls
        _flow.ControlAdded   += (_, e) => { if (e.Control != null) e.Control.MouseWheel += Scroll; };
        _flow.ControlRemoved += (_, e) => { if (e.Control != null) e.Control.MouseWheel -= Scroll; };
    }

    private new void Scroll(object? sender, MouseEventArgs e)
    {
        int delta  = e.Delta > 0 ? 80 : -80;
        _flow.Top  = Math.Max(-MaxScroll, Math.Min(0, _flow.Top + delta));
    }

    private void Clamp() => _flow.Top = Math.Max(-MaxScroll, Math.Min(0, _flow.Top));

    // ── Passthrough so MainForm.cs needs no changes ──────────
    public new Control.ControlCollection Controls => _flow.Controls;

    public new void SuspendLayout()              => _flow.SuspendLayout();
    public new void ResumeLayout()               => _flow.ResumeLayout();
    public new void ResumeLayout(bool perform)   => _flow.ResumeLayout(perform);
}
