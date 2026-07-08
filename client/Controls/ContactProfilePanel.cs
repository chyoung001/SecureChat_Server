#nullable enable
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SecureChat.Models;

namespace SecureChat.Controls;

public class ContactProfilePanel : Panel
{
    public event EventHandler?          CloseRequested;
    public event EventHandler<Contact>? OpenChatRequested;

    private readonly Contact _contact;

    private static readonly Color Bg   = Color.FromArgb(108, 117, 132);
    private static readonly Color Dark = Color.FromArgb(34,  34,  34);

    private const int AvSize = 88;
    private const int BarW   = 240;
    private const int BarH   = 58;

    private Panel  _pnlAv    = null!;
    private Label  _lblName  = null!;
    private Button _btnChat  = null!;
    private Button _btnClose = null!;

    public ContactProfilePanel(Contact contact)
    {
        _contact       = contact;
        Dock           = DockStyle.Fill;
        BackColor      = Bg;
        DoubleBuffered = true;
        BuildUI();
    }

    private void BuildUI()
    {
        // ── Avatar ───────────────────────────────────────────────
        _pnlAv = new Panel { Size = new Size(AvSize, AvSize), BackColor = Color.Transparent };
        _pnlAv.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using var path = RoundedRect(new RectangleF(0, 0, AvSize, AvSize), 22f);
            g.FillPath(new SolidBrush(GetAvatarColor(_contact.UserId)), path);
            string ini = (!string.IsNullOrEmpty(_contact.DisplayName) ? _contact.DisplayName[0]
                          : _contact.Username.Length > 0 ? _contact.Username[0] : '?').ToString();
            using var fnt = new Font("맑은 고딕", 30f, FontStyle.Bold);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(ini, fnt, Brushes.White, new RectangleF(0, 0, AvSize, AvSize), sf);
        };

        // ── Name ─────────────────────────────────────────────────
        _lblName = new Label
        {
            Text      = _contact.DisplayName ?? _contact.Username,
            Font      = new Font("맑은 고딕", 14f, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleCenter,
            Height    = 38,
        };

        // ── 1:1 채팅 버튼 (통화 버튼 없음, 단독 전체 너비) ──────
        _btnChat = new Button
        {
            Text      = "1:1 채팅",
            Font      = new Font("맑은 고딕", 9.5f),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Dark,
            Cursor    = Cursors.Hand,
        };
        _btnChat.FlatAppearance.BorderSize          = 0;
        _btnChat.FlatAppearance.MouseOverBackColor  = Color.FromArgb(245, 245, 245);
        _btnChat.Click += (_, _) => OpenChatRequested?.Invoke(this, _contact);

        // ── Close (×) — 마지막 추가로 z-order 최하위(=가장 뒤)가 되는 것을 피하기 위해
        //    BringToFront() 호출로 z-order 최상위 보장 ─────────────
        _btnClose = MakeCircleBtn("✕", 32);
        _btnClose.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);

        // 모든 자식 추가 후 Close 버튼을 맨 앞으로
        Controls.AddRange([_pnlAv, _lblName, _btnChat, _btnClose]);
        _btnClose.BringToFront();

        Resize += (_, _) => LayoutControls();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        LayoutControls();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Width <= 0 || Height <= 0) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // 흰색 pill (채팅 버튼 배경)
        float bx = Width / 2f - BarW / 2f;
        float by = Height - BarH - 40f;
        using var path = RoundedRect(new RectangleF(bx, by, BarW, BarH), 16f);
        g.FillPath(Brushes.White, path);
    }

    private void LayoutControls()
    {
        if (Width <= 0 || Height <= 0) return;
        int cx = Width / 2;

        _btnClose.Location = new Point(Width - 48, 14);

        int avY = (int)(Height * 0.38) - AvSize / 2;
        _pnlAv.Location  = new Point(cx - AvSize / 2, avY);

        _lblName.Width    = Width;
        _lblName.Location = new Point(0, _pnlAv.Bottom + 14);

        int barX = cx - BarW / 2;
        int barY = Height - BarH - 40;
        _btnChat.Size     = new Size(BarW, BarH);
        _btnChat.Location = new Point(barX, barY);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static Button MakeCircleBtn(string text, int size)
    {
        var btn = new Button
        {
            Text      = text,
            Font      = new Font("맑은 고딕", 10f),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(75, 82, 95),
            ForeColor = Color.White,
            Size      = new Size(size, size),
            Cursor    = Cursors.Hand,
        };
        btn.FlatAppearance.BorderSize         = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(90, 98, 112);
        btn.HandleCreated += (_, _) =>
        {
            var p = new GraphicsPath();
            p.AddEllipse(0, 0, size, size);
            btn.Region = new Region(p);
        };
        return btn;
    }

    private static Color GetAvatarColor(string seed)
    {
        Color[] palette =
        [
            Color.FromArgb( 52, 152, 219),
            Color.FromArgb(231,  76,  60),
            Color.FromArgb( 46, 204, 113),
            Color.FromArgb(155,  89, 182),
            Color.FromArgb(230, 126,  34),
        ];
        return palette[Math.Abs(seed.GetHashCode()) % palette.Length];
    }

    private static GraphicsPath RoundedRect(RectangleF b, float r)
    {
        var p = new GraphicsPath();
        p.AddArc(b.X,           b.Y,            r * 2, r * 2, 180, 90);
        p.AddArc(b.Right - r*2, b.Y,            r * 2, r * 2, 270, 90);
        p.AddArc(b.Right - r*2, b.Bottom - r*2, r * 2, r * 2,   0, 90);
        p.AddArc(b.X,           b.Bottom - r*2, r * 2, r * 2,  90, 90);
        p.CloseFigure();
        return p;
    }
}
