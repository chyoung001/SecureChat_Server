using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SecureChat.Models;

namespace SecureChat.Controls;

public class RoomListItemControl : UserControl
{
    // Musinsa design tokens
    private static readonly Color Canvas    = Color.White;                        // #ffffff
    private static readonly Color Surface   = Color.FromArgb(245, 245, 245);     // #f5f5f5 hover
    private static readonly Color Black     = Color.Black;                        // #000000 text / avatar
    private static readonly Color Body      = Color.FromArgb(51, 51, 51);        // #333333 body text
    private static readonly Color Meta      = Color.FromArgb(153, 153, 153);     // #999999 time / preview
    private static readonly Color Hairline  = Color.FromArgb(238, 238, 238);     // #eeeeee divider

    public ChatRoom Room { get; private set; }
    public event EventHandler<ChatRoom>? Clicked;
    public event EventHandler<ChatRoom>? RightClicked;

    private bool _isHovered;

    public RoomListItemControl(ChatRoom room)
    {
        Room = room;
        Height = 80;
        BackColor = Canvas;
        Cursor = Cursors.Hand;
        DoubleBuffered = true;

        MouseEnter += (_, _) => { _isHovered = true; Invalidate(); };
        MouseLeave += (_, _) =>
        {
            if (!ClientRectangle.Contains(PointToClient(MousePosition)))
            { _isHovered = false; Invalidate(); }
        };
        MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left) Clicked?.Invoke(this, Room);
            else if (e.Button == MouseButtons.Right) RightClicked?.Invoke(this, Room);
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode    = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        g.Clear(_isHovered ? Surface : Canvas);

        // --- Avatar — vivid rounded-rect, white initial ---
        const int av = 44, avX = 14;
        int avY = (Height - av) / 2;
        var avColor = GetAvatarColor(Room.RoomId);
        using var avPath = RoundedRect(new RectangleF(avX, avY, av, av), 10f);
        g.FillPath(new SolidBrush(avColor), avPath);

        string initial = Room.Name.Length > 0 ? Room.Name[0].ToString() : "?";
        using var avFont = new Font("맑은 고딕", 15F, FontStyle.Bold, GraphicsUnit.Point);
        var sf0 = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(initial, avFont, Brushes.White,
            new RectangleF(avX, avY, av, av), sf0);

        int contentLeft = avX + av + 10;
        int rightPad = 12;

        // Measure font heights to position rows without overlap
        using var nameFont = new Font("맑은 고딕", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
        using var prevFont = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point);
        float nameH = g.MeasureString("A", nameFont).Height;
        float prevH = g.MeasureString("A", prevFont).Height;
        const float rowGap = 6f;
        float totalH = nameH + rowGap + prevH;
        float nameY = (Height - totalH) / 2f;
        float prevY  = nameY + nameH + rowGap;

        // --- Time ---
        string timeStr = FormatTime(Room.LastActivityAt);
        using var timeFont = new Font("맑은 고딕", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
        var timeSz = g.MeasureString(timeStr, timeFont);
        float timeX = Width - rightPad - timeSz.Width;
        using var metaBrush = new SolidBrush(Meta);
        g.DrawString(timeStr, timeFont, metaBrush, timeX, nameY);

        // --- Unread badge — black bg, white text ---
        float badgeRight = Width - rightPad;
        if (Room.UnreadCount > 0)
        {
            string badgeTxt = Room.UnreadCount > 99 ? "99+" : Room.UnreadCount.ToString();
            using var badgeFont = new Font("맑은 고딕", 7.5F, FontStyle.Bold, GraphicsUnit.Point);
            var bTxtSz = g.MeasureString(badgeTxt, badgeFont);
            float bW = Math.Max(20, bTxtSz.Width + 10);
            const float bH = 18;
            float bX = Width - rightPad - bW;
            float bY = prevY + (prevH - bH) / 2f;

            using var bdPath = RoundedRect(new RectangleF(bX, bY, bW, bH), 9);
            g.FillPath(Brushes.Black, bdPath);
            g.DrawString(badgeTxt, badgeFont, Brushes.White,
                bX + (bW - bTxtSz.Width) / 2,
                bY + (bH - bTxtSz.Height) / 2);

            badgeRight = bX - 4;
        }

        // --- Ephemeral timer badge ---
        if (Room.IsEphemeral && Room.EphemeralDisplay != null)
        {
            using var timerFont = new Font("맑은 고딕", 7.5F, FontStyle.Regular, GraphicsUnit.Point);
            var timerSz = g.MeasureString(Room.EphemeralDisplay, timerFont);
            float tW = timerSz.Width + 10, tH = 17;
            float tX = contentLeft, tY = prevY + (prevH - tH) / 2f;

            using var timerPath = RoundedRect(new RectangleF(tX, tY, tW, tH), 4);
            g.FillPath(new SolidBrush(Surface), timerPath);
            using var timerPen = new Pen(Color.FromArgb(221, 221, 221));
            g.DrawPath(timerPen, timerPath);
            g.DrawString(Room.EphemeralDisplay, timerFont, metaBrush,
                tX + 5, tY + (tH - timerSz.Height) / 2);

            string previewStr = GetPreviewText(skipEphemeral: true);
            float pX = tX + tW + 4, pW = badgeRight - pX;
            if (pW > 20)
            {
                var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(previewStr, prevFont, metaBrush,
                    new RectangleF(pX, prevY, pW, prevH + 2), sf);
            }
        }
        else
        {
            // --- Normal preview ---
            var sf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
            g.DrawString(GetPreviewText(), prevFont, metaBrush,
                new RectangleF(contentLeft, prevY, badgeRight - contentLeft, prevH + 2), sf);
        }

        // --- Name — black bold ---
        float nameRight = timeX - 6;
        var nameSf = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap };
        g.DrawString(Room.Name, nameFont, Brushes.Black,
            new RectangleF(contentLeft, nameY, nameRight - contentLeft, nameH + 2), nameSf);

        // --- Hairline divider #eeeeee ---
        using var divPen = new Pen(Hairline);
        g.DrawLine(divPen, contentLeft, Height - 1, Width, Height - 1);
    }

    private string GetPreviewText(bool skipEphemeral = false)
    {
        if (!skipEphemeral && Room.IsEphemeral && Room.EphemeralDisplay != null)
            return $"{Room.EphemeralDisplay} 후 사라집니다";
        if (Room.LastMessageText != null)
        {
            if (!Room.IsDirectMessage && Room.LastMessageSender != null)
                return $"{Room.LastMessageSender}: {Room.LastMessageText}";
            return Room.LastMessageText;
        }
        return Room.IsDirectMessage ? "1:1 대화" : $"그룹 · {Room.MemberIds.Count}명";
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
        p.AddArc(b.X, b.Y, r * 2, r * 2, 180, 90);
        p.AddArc(b.Right - r * 2, b.Y, r * 2, r * 2, 270, 90);
        p.AddArc(b.Right - r * 2, b.Bottom - r * 2, r * 2, r * 2, 0, 90);
        p.AddArc(b.X, b.Bottom - r * 2, r * 2, r * 2, 90, 90);
        p.CloseFigure();
        return p;
    }

    public void UpdateRoom(ChatRoom updated)
    {
        Room = updated;
        Invalidate();
    }

    public void UpdatePreview(ChatMessage message)
    {
        Room.LastMessagePreview = message;
        Room.LastActivityAt     = message.SentAt;
        Room.LastMessageText    = message.PlainText;
        Invalidate();
    }

    public void IncrementUnread() { Room.UnreadCount++; Invalidate(); }
    public void ClearUnread()     { Room.UnreadCount = 0; Invalidate(); }

    private static string FormatTime(DateTime when)
    {
        var diff = DateTime.Now - when;
        if (diff.TotalMinutes < 1)  return "방금";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}분";
        if (diff.TotalHours < 24 && when.Date == DateTime.Today) return $"{(int)diff.TotalHours}시간";
        if (when.Date == DateTime.Today.AddDays(-1)) return "어제";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}일";
        return when.ToString("MM/dd");
    }
}
