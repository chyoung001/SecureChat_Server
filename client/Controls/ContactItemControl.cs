using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SecureChat.Common;
using SecureChat.Models;

namespace SecureChat.Controls;

public class ContactItemControl : UserControl
{
    // 색상 토큰은 SecureChat.Common.Theme 에서 중앙 관리
    private static readonly Color Canvas   = Theme.Canvas;
    private static readonly Color Surface  = Theme.Surface;
    private static readonly Color Hairline = Theme.Hairline;
    private static readonly Color Border   = Theme.Border;
    private static readonly Color Body     = Theme.Body;
    private static readonly Color Meta     = Theme.Meta;

    public Contact Contact { get; }
    public bool IsOnline { get; set; }

    private bool _isSelected;
    private bool _isHovered;

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; Invalidate(); SelectionChanged?.Invoke(this, Contact); }
    }

    public event EventHandler<Contact>? SelectionChanged;

    public ContactItemControl(Contact contact, bool isOnline = false)
    {
        Contact  = contact;
        IsOnline = isOnline;

        Height    = 64;
        BackColor = Canvas;
        Cursor         = Cursors.Hand;
        DoubleBuffered = true;

        MouseEnter += (_, _) => { _isHovered = true;  Invalidate(); };
        MouseLeave += (_, _) => { _isHovered = false; Invalidate(); };
        MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                _isSelected = !_isSelected;
            Invalidate();
            SelectionChanged?.Invoke(this, Contact);
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        g.Clear(_isHovered ? Surface : Canvas);

        // ── Checkbox ─────────────────────────────────────────────
        const int cbX = 14, cbY = 23, cbS = 18;
        var cbRect = new Rectangle(cbX, cbY, cbS, cbS);
        using var cbPath = RoundedRect(cbRect, 4);
        if (_isSelected)
        {
            g.FillPath(Brushes.Black, cbPath);
            using var ckFont = new Font("맑은 고딕", 9F, FontStyle.Bold, GraphicsUnit.Point);
            var ckSz = g.MeasureString("v", ckFont);
            g.DrawString("v", ckFont, Brushes.White,
                cbX + (cbS - ckSz.Width)  / 2,
                cbY + (cbS - ckSz.Height) / 2);
        }
        else
        {
            g.FillPath(new SolidBrush(Surface), cbPath);
            using var cbPen = new Pen(Border, 1f);
            g.DrawPath(cbPen, cbPath);
        }

        // ── Avatar ────────────────────────────────────────────────
        const int av = 40, avX = 42, avY = 12;
        using var avPath = RoundedRect(new Rectangle(avX, avY, av, av), 10);
        g.FillPath(new SolidBrush(GetAvatarColor(Contact.UserId)), avPath);

        string init = Contact.DisplayName.Length > 0 ? Contact.DisplayName[0].ToString() : "?";
        using var initFont = new Font("맑은 고딕", 13F, FontStyle.Bold, GraphicsUnit.Point);
        var initSz = g.MeasureString(init, initFont);
        g.DrawString(init, initFont, Brushes.White,
            avX + (av - initSz.Width)  / 2,
            avY + (av - initSz.Height) / 2);

        // ── Name ──────────────────────────────────────────────────
        const int textX = 94;
        using var nameFont = new Font("맑은 고딕", 10F, FontStyle.Bold, GraphicsUnit.Point);
        var nameSz = g.MeasureString(Contact.DisplayName, nameFont);
        g.DrawString(Contact.DisplayName, nameFont, new SolidBrush(Body), textX, 14);

        // Online indicator dot
        if (IsOnline)
        {
            float dotX = textX + nameSz.Width + 4;
            g.FillEllipse(Brushes.Black, dotX, 20, 8, 8);
        }

        // ── Username ──────────────────────────────────────────────
        using var userFont = new Font("맑은 고딕", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        g.DrawString("@" + Contact.Username, userFont, new SolidBrush(Meta), textX, 36);

        // ── Verified mark ─────────────────────────────────────────
        if (Contact.IsVerified)
        {
            using var vFont = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point);
            const string vMark = "verified";
            var vSz = g.MeasureString(vMark, vFont);
            g.DrawString(vMark, vFont, new SolidBrush(Meta),
                Width - 8 - vSz.Width, (Height - vSz.Height) / 2);
        }

        // ── Bottom divider ────────────────────────────────────────
        using var divPen = new Pen(Hairline);
        g.DrawLine(divPen, textX, Height - 1, Width, Height - 1);
    }

    private static Color GetAvatarColor(string seed) => Theme.AvatarColor(seed);

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
