using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SecureChat.Models;

namespace SecureChat.Controls;

public class MessageBubbleControl : UserControl
{
    // Musinsa design tokens
    private static readonly Color Canvas    = Color.White;                     // #ffffff page bg
    private static readonly Color OutBubble = Color.Black;                    // #000000 outgoing bubble
    private static readonly Color InBubble  = Color.FromArgb(245, 245, 245); // #f5f5f5 incoming bubble
    private static readonly Color OutText   = Color.White;                    // #ffffff outgoing text
    private static readonly Color InText    = Color.FromArgb(51, 51, 51);    // #333333 incoming text
    private static readonly Color Meta      = Color.FromArgb(153, 153, 153); // #999999 time / sender

    private readonly Label  _lblSender;
    private readonly Label  _lblBody;
    private readonly Label  _lblMeta;
    private readonly Panel  _bubble;
    private readonly bool   _hideHeader;
    private System.Windows.Forms.Timer? _ttlTimer;

    public ChatMessage Message { get; }
    public event EventHandler<ChatMessage>? Expired;

    public MessageBubbleControl(ChatMessage message, bool hideHeader = false)
    {
        Message     = message;
        _hideHeader = hideHeader;

        AutoSize  = false;
        BackColor = Canvas;
        Padding   = new Padding(0);
        Height    = 64;

        // Bubble panel
        _bubble = new Panel
        {
            BackColor = message.IsMine ? OutBubble : InBubble,
            AutoSize  = false
        };
        _bubble.Paint += Bubble_Paint;

        // Sender name (incoming only, 연속 메시지면 숨김)
        bool showSender = !message.IsMine && !hideHeader;
        _lblSender = new Label
        {
            Text      = showSender ? message.SenderName : string.Empty,
            Font      = new Font("맑은 고딕", 8F, FontStyle.Bold),
            ForeColor = Meta,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Visible   = showSender
        };

        // Message body
        _lblBody = new Label
        {
            Text      = message.PlainText,
            Font      = new Font("맑은 고딕", 10F),
            ForeColor = message.IsMine ? OutText : InText,
            AutoSize  = false,
            BackColor = Color.Transparent
        };

        // Meta (time + status + TTL)
        _lblMeta = new Label
        {
            Text      = BuildMetaText(),
            Font      = new Font("맑은 고딕", 7.5F),
            ForeColor = Meta,
            AutoSize  = true,
            BackColor = Color.Transparent
        };

        _bubble.Controls.Add(_lblBody);
        Controls.Add(_lblSender);
        Controls.Add(_bubble);
        Controls.Add(_lblMeta);

        Resize += (_, _) => LayoutChildren();
        LayoutChildren();

        if (message.TtlSeconds > 0)
        {
            if (!message.ExpiresAt.HasValue)
                message.ExpiresAt = message.SentAt.AddSeconds(message.TtlSeconds);
            StartTtlTimer();
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Layout
    // ─────────────────────────────────────────────────────────

    private void LayoutChildren()
    {
        if (Width <= 0) return;

        int avatarW    = Message.IsMine ? 0 : 44;
        int avatarPad  = Message.IsMine ? 0 : 8;
        int leftBase   = avatarPad + avatarW + 8;
        int rightPad   = 12;
        int available  = Width - leftBase - rightPad;
        int bubbleMax  = (int)(available * 0.72);

        var bodySize = TextRenderer.MeasureText(
            _lblBody.Text, _lblBody.Font,
            new Size(bubbleMax - 24, 4000),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);

        _lblBody.Size     = new Size(bodySize.Width + 4, bodySize.Height + 4);
        int bubbleWidth   = Math.Min(bubbleMax, bodySize.Width + 24);
        int bubbleHeight  = bodySize.Height + 16;
        _bubble.Size      = new Size(bubbleWidth, bubbleHeight);
        _lblBody.Location = new Point(12, 8);

        int topPad       = _hideHeader ? 2 : 6;
        int senderHeight = Message.IsMine ? 0 : (_lblSender.Visible ? _lblSender.Height + 2 : 0);
        int bubbleTop       = topPad + senderHeight;

        if (Message.IsMine)
        {
            _bubble.Location = new Point(Width - rightPad - bubbleWidth, bubbleTop);
            _lblMeta.Location = new Point(
                _bubble.Left - _lblMeta.Width - 6,
                _bubble.Bottom - _lblMeta.Height);
        }
        else
        {
            _lblSender.Location = new Point(leftBase, topPad);
            _bubble.Location    = new Point(leftBase, bubbleTop);
            _lblMeta.Location   = new Point(
                _bubble.Right + 6,
                _bubble.Bottom - _lblMeta.Height);
        }

        Height = Math.Max(60, bubbleTop + bubbleHeight + _lblMeta.Height + 6);
        Invalidate();
    }

    // ─────────────────────────────────────────────────────────
    //  Paint — bubble + avatar
    // ─────────────────────────────────────────────────────────

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (Message.IsMine || _hideHeader) return;

        // Draw avatar circle — black with white initial
        var g = e.Graphics;
        g.SmoothingMode    = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        const int av = 36;
        int avX = 8;
        int avY = (_bubble.Top + _bubble.Height / 2) - av / 2;

        using var avPath = RoundedRect(new Rectangle(avX, avY, av, av), 9);
        g.FillPath(new SolidBrush(GetAvatarColor(Message.SenderName)), avPath);

        string initial = Message.SenderName.Length > 0 ? Message.SenderName[0].ToString() : "?";
        using var font = new Font("맑은 고딕", 12F, FontStyle.Bold, GraphicsUnit.Point);
        var sz = g.MeasureString(initial, font);
        g.DrawString(initial, font, Brushes.White,
            avX + (av - sz.Width) / 2,
            avY + (av - sz.Height) / 2);
    }

    private void Bubble_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = _bubble.ClientRectangle;
        rect.Width  -= 1;
        rect.Height -= 1;
        using var path  = RoundedRect(rect, 4);
        using var brush = new SolidBrush(_bubble.BackColor);
        g.FillPath(brush, path);
    }

    // ─────────────────────────────────────────────────────────
    //  TTL timer
    // ─────────────────────────────────────────────────────────

    private void StartTtlTimer()
    {
        _ttlTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _ttlTimer.Tick += (_, _) =>
        {
            _lblMeta.Text = BuildMetaText();
            if (Message.ExpiresAt.HasValue && DateTime.Now >= Message.ExpiresAt.Value)
            {
                _ttlTimer.Stop();
                Message.Status = MessageStatus.Expired;
                Expired?.Invoke(this, Message);
                Parent?.Controls.Remove(this);
                Dispose();
            }
        };
        _ttlTimer.Start();
    }

    private string BuildMetaText()
    {
        var time   = Message.SentAt.ToString("HH:mm");
        var status = Message.IsMine ? StatusGlyph(Message.Status) : string.Empty;
        var ttl    = Message.TtlSeconds > 0 && Message.ExpiresAt.HasValue
            ? $" {Math.Max(0, (int)(Message.ExpiresAt.Value - DateTime.Now).TotalSeconds)}s"
            : string.Empty;
        return Message.IsMine ? $"{status} {time}{ttl}" : $"{time}{ttl}";
    }

    private static string StatusGlyph(MessageStatus s) => s switch
    {
        MessageStatus.Sending   => "···",
        MessageStatus.Sent      => "✓",
        MessageStatus.Delivered => "✓✓",
        MessageStatus.Read      => "✓✓",
        MessageStatus.Failed    => "✕",
        MessageStatus.Expired   => "—",
        _                       => string.Empty
    };

    private static readonly Color ReadColor      = Color.FromArgb(41, 182, 246);  // #29B6F6 blue
    private static readonly Color DefaultMeta    = Color.FromArgb(153, 153, 153); // #999999

    public void UpdateStatus(MessageStatus status)
    {
        Message.Status  = status;
        _lblMeta.Text   = BuildMetaText();
        _lblMeta.ForeColor = status == MessageStatus.Read ? ReadColor : DefaultMeta;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _ttlTimer?.Stop(); _ttlTimer?.Dispose(); }
        base.Dispose(disposing);
    }

    // ─────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────

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

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
