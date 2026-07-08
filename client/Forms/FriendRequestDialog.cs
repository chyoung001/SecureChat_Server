#nullable enable
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Forms;

public class FriendRequestDialog : Form
{
    private static readonly Color Canvas  = Color.White;
    private static readonly Color Surface = Color.FromArgb(245, 245, 245);
    private static readonly Color Border  = Color.FromArgb(221, 221, 221);
    private static readonly Color Body    = Color.FromArgb(34, 34, 34);
    private static readonly Color Meta    = Color.FromArgb(153, 153, 153);
    private static readonly Color Red     = Color.FromArgb(231, 76, 60);

    private readonly List<FriendRequest>   _requests;
    private readonly IFriendRequestService _service;
    private Panel? _pnlScroll;

    public FriendRequestDialog(List<FriendRequest> requests, IFriendRequestService service)
    {
        _requests = requests;
        _service  = service;
        BuildUI();
    }

    private void BuildUI()
    {
        const int mx      = 20;
        const int cardH   = 76;
        const int titleH  = 56;
        const int maxScH  = 400;
        const int clientW = 480;

        Text            = "친구 요청";
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = Canvas;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;

        Controls.Add(new Label
        {
            Text      = "친구 요청",
            Font      = new Font("맑은 고딕", 12f, FontStyle.Bold),
            ForeColor = Body,
            AutoSize  = true,
            Location  = new Point(mx, 18),
        });

        if (_requests.Count == 0)
        {
            ClientSize = new Size(clientW, 160);
            Controls.Add(new Label
            {
                Text      = "대기 중인 친구 요청이 없습니다.",
                Font      = new Font("맑은 고딕", 9f),
                ForeColor = Meta,
                AutoSize  = true,
                Location  = new Point(mx, 56),
            });
            return;
        }

        int totalH  = _requests.Count * (cardH + 8) - 8 + mx;
        int scrollH = Math.Min(totalH, maxScH);
        ClientSize  = new Size(clientW, titleH + scrollH + 16);

        _pnlScroll = new Panel
        {
            Location   = new Point(0, titleH),
            Size       = new Size(clientW, scrollH),
            AutoScroll = true,
            BackColor  = Canvas,
        };
        Controls.Add(_pnlScroll);

        // 마우스 휠: 폼과 패널 양쪽에서 스크롤 전달
        MouseWheel         += (_, e) => ScrollPanel(e.Delta);
        _pnlScroll.MouseWheel += (_, e) => ScrollPanel(e.Delta);

        int cardW = clientW - mx * 2 - SystemInformation.VerticalScrollBarWidth;
        int y = 8;
        foreach (var req in _requests)
        {
            _pnlScroll.Controls.Add(MakeCard(req, mx, y, cardW, cardH));
            y += cardH + 8;
        }
    }

    private void ScrollPanel(int delta)
    {
        if (_pnlScroll == null) return;
        int cur    = -_pnlScroll.AutoScrollPosition.Y;
        int newY   = cur - delta / 3;
        int maxY   = Math.Max(0, _pnlScroll.VerticalScroll.Maximum - _pnlScroll.ClientSize.Height + 1);
        _pnlScroll.AutoScrollPosition = new Point(0, Math.Clamp(newY, 0, maxY));
    }

    private Panel MakeCard(FriendRequest req, int x, int y, int w, int h)
    {
        var card = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.Transparent };
        card.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new RectangleF(0.5f, 0.5f, w - 1f, h - 1f), 10f);
            g.FillPath(new SolidBrush(Surface), path);
            g.DrawPath(new Pen(Border), path);
        };

        // 아바타
        var pnlAv = new Panel { Location = new Point(14, 20), Size = new Size(36, 36), BackColor = Surface };
        pnlAv.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using var path = RoundedRect(new RectangleF(0, 0, 36, 36), 9f);
            g.FillPath(new SolidBrush(GetAvatarColor(req.SenderId)), path);
            string ini = req.SenderName.Length > 0 ? req.SenderName[0].ToString() : "?";
            using var fnt = new Font("맑은 고딕", 12f, FontStyle.Bold);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(ini, fnt, Brushes.White, new RectangleF(0, 0, 36, 36), sf);
        };

        var lblName = new Label
        {
            Text      = req.SenderName,
            Font      = new Font("맑은 고딕", 9.5f, FontStyle.Bold),
            ForeColor = Body,
            AutoSize  = true,
            Location  = new Point(58, 16),
        };
        var lblUser = new Label
        {
            Text      = "@" + req.SenderUsername,
            Font      = new Font("맑은 고딕", 8.5f),
            ForeColor = Meta,
            AutoSize  = true,
            Location  = new Point(58, 36),
        };

        // 수락 버튼
        var btnAccept = new Button
        {
            Text      = "수락",
            Font      = new Font("맑은 고딕", 8.5f, FontStyle.Bold),
            BackColor = Body,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size      = new Size(60, 30),
            Location  = new Point(w - 134, 23),
            Cursor    = Cursors.Hand,
        };
        btnAccept.FlatAppearance.BorderSize = 0;
        ApplyRadius(btnAccept, 6);
        btnAccept.Click += async (_, _) =>
        {
            btnAccept.Enabled = false;
            await _service.AcceptRequestAsync(req.RequestId);
            card.Visible = false;
            CheckEmpty();
        };

        // 거절 버튼
        var btnReject = new Button
        {
            Text      = "거절",
            Font      = new Font("맑은 고딕", 8.5f),
            BackColor = Surface,
            ForeColor = Body,
            FlatStyle = FlatStyle.Flat,
            Size      = new Size(60, 30),
            Location  = new Point(w - 68, 23),
            Cursor    = Cursors.Hand,
        };
        btnReject.FlatAppearance.BorderColor = Border;
        btnReject.FlatAppearance.BorderSize  = 1;
        ApplyRadius(btnReject, 6);
        btnReject.Click += async (_, _) =>
        {
            btnReject.Enabled = false;
            await _service.RejectRequestAsync(req.RequestId);
            card.Visible = false;
            CheckEmpty();
        };

        card.Controls.AddRange([pnlAv, lblName, lblUser, btnAccept, btnReject]);
        return card;
    }

    private void CheckEmpty()
    {
        bool anyVisible = _pnlScroll?.Controls.OfType<Panel>().Any(p => p.Visible) ?? false;
        if (!anyVisible) Close();
    }

    // ── Helpers ──────────────────────────────────────────────────

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

    private static void ApplyRadius(Control c, int r)
    {
        void Update()
        {
            if (c.Width <= 0 || c.Height <= 0) return;
            var p = new GraphicsPath();
            p.AddArc(0, 0, r * 2, r * 2, 180, 90);
            p.AddArc(c.Width - r * 2, 0, r * 2, r * 2, 270, 90);
            p.AddArc(c.Width - r * 2, c.Height - r * 2, r * 2, r * 2, 0, 90);
            p.AddArc(0, c.Height - r * 2, r * 2, r * 2, 90, 90);
            p.CloseFigure();
            c.Region = new Region(p);
        }
        c.Resize += (_, _) => Update();
        Update();
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
}
