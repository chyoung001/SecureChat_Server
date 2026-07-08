#nullable enable
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Forms;

public partial class AddFriendDialog : Form
{
    private static readonly Color Canvas  = Color.White;
    private static readonly Color Surface = Color.FromArgb(245, 245, 245);
    private static readonly Color Border  = Color.FromArgb(221, 221, 221);
    private static readonly Color Body    = Color.FromArgb(34, 34, 34);
    private static readonly Color Meta    = Color.FromArgb(153, 153, 153);
    private static readonly Color Accent  = Color.FromArgb(26, 188, 156);
    private static readonly Color ErrRed  = Color.FromArgb(192, 57, 43);
    private static readonly Color Green   = Color.FromArgb(39, 174, 96);

    // y 좌표 상수: 검색바 아래 콘텐츠 시작 지점
    private const int Mx      = 24;
    private const int SearchH = 52;
    private const int ContentY = 86; // 검색바 bottom(20+52=72) + gap 14

    private readonly IContactService        _contactService;
    private readonly IFriendRequestService  _friendRequestService;

    private TextBox  _txtSearch  = null!;
    private Panel    _pnlResult  = null!;
    private Label    _lblName    = null!;
    private Label    _lblUser    = null!;
    private Label    _lblVerDot  = null!;
    private Label    _lblVerText = null!;
    private Panel    _pnlAv      = null!;
    private Button   _btnAdd     = null!;
    private Label    _lblMsg     = null!;
    private Panel    _pnlSugGrid = null!;
    private Contact? _found;

    public AddFriendDialog(IContactService contactService, IFriendRequestService friendRequestService)
    {
        _contactService       = contactService;
        _friendRequestService = friendRequestService;
        InitializeComponent();
        BuildUI();
        _ = LoadSuggestionsAsync();
    }

    private void BuildUI()
    {
        Text            = "친구 추가";
        Size            = new Size(500, 500);
        MinimumSize     = new Size(500, 500);
        MaximumSize     = new Size(500, 500);
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = Canvas;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        MinimizeBox     = false;

        int cw      = ClientSize.Width - Mx * 2;
        const int btnW = 70;
        const int btnH = 38;

        // ── Search bar (h=52) ────────────────────────────────────
        var pnlSearch = new Panel
        {
            Location  = new Point(Mx, 20),
            Size      = new Size(cw, SearchH),
            BackColor = Color.Transparent,
        };
        pnlSearch.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new RectangleF(0.5f, 0.5f, cw - 1f, SearchH - 1f), 10f);
            g.FillPath(new SolidBrush(Surface), path);
            g.DrawPath(new Pen(Border), path);
            // 돋보기 아이콘
            using var iFont = new Font("Segoe UI Symbol", 13f);
            var iSz = g.MeasureString("🔍", iFont);
            g.DrawString("🔍", iFont, new SolidBrush(Meta), 14f, (SearchH - iSz.Height) / 2f);
        };

        _txtSearch = new TextBox
        {
            BorderStyle     = BorderStyle.None,
            BackColor       = Surface,
            Font            = new Font("맑은 고딕", 10.5f),
            PlaceholderText = "이름 또는 @username 검색...",
        };
        int txtX = 46;
        int txtW = cw - txtX - btnW - 16; // 버튼 왼쪽까지
        _txtSearch.Size     = new Size(txtW, _txtSearch.PreferredHeight);
        _txtSearch.Location = new Point(txtX, (SearchH - _txtSearch.Height) / 2);
        _txtSearch.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; _ = DoSearchAsync(); }
        };

        // 검색 버튼: 오른쪽 여백 8px 확보해서 잘림 방지
        var btnSearch = new Button
        {
            Text      = "검색",
            Font      = new Font("맑은 고딕", 9f, FontStyle.Bold),
            BackColor = Body,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size      = new Size(btnW, btnH),
            Location  = new Point(cw - btnW - 8, (SearchH - btnH) / 2),
            Cursor    = Cursors.Hand,
        };
        btnSearch.FlatAppearance.BorderSize = 0;
        ApplyRadius(btnSearch, 8);
        btnSearch.Click += async (_, _) => await DoSearchAsync();
        pnlSearch.Controls.AddRange([_txtSearch, btnSearch]);

        // ── Result card (기본 숨김) ──────────────────────────────
        _pnlResult = new Panel
        {
            Location  = new Point(Mx, ContentY),
            Size      = new Size(cw, 110),
            BackColor = Color.Transparent,
            Visible   = false,
        };
        _pnlResult.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new RectangleF(0.5f, 0.5f, cw - 1f, 109f), 10f);
            g.FillPath(new SolidBrush(Canvas), path);
            g.DrawPath(new Pen(Border), path);
        };

        _pnlAv = new Panel { Location = new Point(16, 35), Size = new Size(40, 40), BackColor = Color.Transparent };
        _pnlAv.Paint += (_, e) =>
        {
            if (_found == null) return;
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using var path = RoundedRect(new RectangleF(0, 0, 40, 40), 10f);
            g.FillPath(new SolidBrush(GetAvatarColor(_found.UserId)), path);
            string ini = _found.DisplayName.Length > 0 ? _found.DisplayName[0].ToString() : "?";
            using var fnt = new Font("맑은 고딕", 14f, FontStyle.Bold);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(ini, fnt, Brushes.White, new RectangleF(0, 0, 40, 40), sf);
        };

        _lblName = new Label
        {
            Font      = new Font("맑은 고딕", 10.5f, FontStyle.Bold),
            ForeColor = Body,
            AutoSize  = true,
            Location  = new Point(68, 26),
        };
        _lblVerDot = new Label
        {
            Text      = "●",
            Font      = new Font("맑은 고딕", 8f),
            ForeColor = Green,
            AutoSize  = true,
            Visible   = false,
        };
        _lblUser = new Label
        {
            Font      = new Font("맑은 고딕", 8.5f),
            ForeColor = Meta,
            AutoSize  = true,
            Location  = new Point(68, 48),
        };
        _lblVerText = new Label
        {
            Text      = "Verified Identity",
            Font      = new Font("맑은 고딕", 8f),
            ForeColor = Meta,
            AutoSize  = true,
            Location  = new Point(68, 68),
            Visible   = false,
        };

        _btnAdd = new Button
        {
            Text      = "친구 추가",
            Font      = new Font("맑은 고딕", 9f, FontStyle.Bold),
            BackColor = Body,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size      = new Size(86, 32),
            Location  = new Point(cw - 102, 39),
            Cursor    = Cursors.Hand,
            Visible   = false,
        };
        _btnAdd.FlatAppearance.BorderSize = 0;
        ApplyRadius(_btnAdd, 8);
        _btnAdd.Click += async (_, _) => await DoAddAsync();
        _pnlResult.Controls.AddRange([_pnlAv, _lblName, _lblVerDot, _lblUser, _lblVerText, _btnAdd]);

        // ── 상태 메시지 ─────────────────────────────────────────
        _lblMsg = new Label
        {
            Font      = new Font("맑은 고딕", 9f),
            ForeColor = Meta,
            AutoSize  = true,
            Visible   = false,
        };

        // ── 추천 친구 그리드 ─────────────────────────────────────
        // 고정값 대신 실제 ClientHeight 기준으로 높이 계산 → 잘림 방지
        int gridH = ClientSize.Height - ContentY - 24;
        _pnlSugGrid = new Panel
        {
            Location  = new Point(Mx, ContentY),
            Size      = new Size(cw, gridH),
            BackColor = Canvas,
        };

        Controls.AddRange([pnlSearch, _pnlResult, _lblMsg, _pnlSugGrid]);
    }

    // ── Suggestions ──────────────────────────────────────────────

    private async Task LoadSuggestionsAsync()
    {
        var contacts = await _contactService.GetContactsAsync();
        _pnlSugGrid.Controls.Clear();

        var list  = contacts.Take(6).ToList();
        int cardW = (_pnlSugGrid.Width - 8) / 2;
        const int cardH = 80;
        int col = 0, row = 0;

        foreach (var c in list)
        {
            int x = col * (cardW + 8);
            int y = row * (cardH + 8);
            _pnlSugGrid.Controls.Add(MakeSugCard(c, new Point(x, y), cardW, cardH));
            if (++col >= 2) { col = 0; row++; }
        }
    }

    private Panel MakeSugCard(Contact contact, Point loc, int w, int h)
    {
        var card = new Panel { Location = loc, Size = new Size(w, h), BackColor = Color.Transparent };
        card.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new RectangleF(0.5f, 0.5f, w - 1f, h - 1f), 10f);
            g.FillPath(new SolidBrush(Surface), path);
            g.DrawPath(new Pen(Border), path);
        };

        // 아바타
        var pnlAv = new Panel { Location = new Point(14, 22), Size = new Size(36, 36), BackColor = Surface };
        pnlAv.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using var path = RoundedRect(new RectangleF(0, 0, 36, 36), 9f);
            g.FillPath(new SolidBrush(GetAvatarColor(contact.UserId)), path);
            string ini = contact.DisplayName.Length > 0 ? contact.DisplayName[0].ToString() : "?";
            using var fnt = new Font("맑은 고딕", 12f, FontStyle.Bold);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(ini, fnt, Brushes.White, new RectangleF(0, 0, 36, 36), sf);
        };

        var lblN = new Label
        {
            Text      = contact.DisplayName,
            Font      = new Font("맑은 고딕", 9f, FontStyle.Bold),
            ForeColor = Body,
            AutoSize  = false,
            Size      = new Size(w - 114, 22),   // 오른쪽 여백 넉넉히 (+ 버튼 x=w-44, gap 8 + 버튼 30 + 여백 12)
            Location  = new Point(58, 18),
        };
        var lblU = new Label
        {
            Text      = "@" + contact.Username,
            Font      = new Font("맑은 고딕", 8f),
            ForeColor = Meta,
            AutoSize  = false,
            Size      = new Size(w - 114, 20),
            Location  = new Point(58, 42),
        };

        // + 버튼: Panel GDI 직접 드로잉 (Button Region 잘림 방지)
        var pnlPlus = MakePlusButton(30, 30, new Point(w - 44, 25),
            () => _contactService.AddContactAsync(contact.UserId));

        card.Controls.AddRange([pnlAv, lblN, lblU, pnlPlus]);
        return card;
    }

    // + 버튼을 GDI로 직접 그리는 Panel 반환
    private Panel MakePlusButton(int bw, int bh, Point loc, Func<Task> onClickAsync)
    {
        bool done = false;
        var pnl = new Panel { Location = loc, Size = new Size(bw, bh), BackColor = Surface, Cursor = Cursors.Hand };
        pnl.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float d = Math.Min(bw, bh) - 3f;
            float ox = (bw - d) / 2f, oy = (bh - d) / 2f;
            if (done)
            {
                using var pen = new Pen(Accent, 1.5f);
                g.DrawEllipse(pen, ox, oy, d, d);
                using var ck = new Pen(Accent, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                float cx = bw / 2f, cy = bh / 2f;
                g.DrawLine(ck, cx - 5.5f, cy, cx - 1.5f, cy + 4f);
                g.DrawLine(ck, cx - 1.5f, cy + 4f, cx + 5.5f, cy - 4f);
            }
            else
            {
                using var pen = new Pen(Border, 1.5f);
                g.DrawEllipse(pen, ox, oy, d, d);
                using var pl = new Pen(Meta, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                float cx = bw / 2f, cy = bh / 2f, o = 6f;
                g.DrawLine(pl, cx - o, cy, cx + o, cy);
                g.DrawLine(pl, cx, cy - o, cx, cy + o);
            }
        };
        pnl.MouseClick += async (_, _) =>
        {
            if (done) return;
            done = true;
            pnl.Invalidate();
            await onClickAsync();
        };
        return pnl;
    }

    // ── Search / Add ─────────────────────────────────────────────

    private async Task DoSearchAsync()
    {
        string query = _txtSearch.Text.Trim().TrimStart('@');
        if (string.IsNullOrEmpty(query)) return;

        // 이전 상태 초기화
        _pnlSugGrid.Visible = false;
        _pnlResult.Visible  = false;
        _lblMsg.Visible     = false;

        try
        {
            _found = await _contactService.SearchUserAsync(query);

            if (_found == null)
            {
                _lblMsg.Location = new Point(Mx, ContentY + 12);
                ShowMsg($"'{query}' 사용자를 찾을 수 없습니다.", ErrRed);
                return;
            }

            var existing      = await _contactService.GetContactsAsync();
            bool alreadyAdded = existing.Any(c => c.UserId == _found.UserId);

            _lblName.Text       = _found.DisplayName;
            _lblUser.Text       = $"@{_found.Username}";
            _lblVerDot.Visible  = _found.IsVerified;
            _lblVerText.Visible = _found.IsVerified;
            _lblVerDot.Location = new Point(_lblName.Location.X + _lblName.PreferredWidth + 4, _lblName.Location.Y + 3);
            _btnAdd.Visible     = !alreadyAdded;
            _btnAdd.Enabled     = true;
            _btnAdd.Text        = "친구 추가";
            _btnAdd.BackColor   = Body;
            _pnlAv.Invalidate();
            _pnlResult.Visible  = true;

            if (alreadyAdded)
            {
                // 결과 카드 아래에 메시지 표시
                _lblMsg.Location = new Point(Mx, ContentY + 118);
                ShowMsg("이미 연락처에 추가된 사용자입니다.", Meta);
            }
        }
        catch
        {
            _lblMsg.Location = new Point(Mx, ContentY + 12);
            ShowMsg("검색 중 오류가 발생했습니다.", ErrRed);
        }
    }

    private async Task DoAddAsync()
    {
        if (_found == null) return;
        _btnAdd.Enabled = false;
        _btnAdd.Text    = "전송 중...";
        try
        {
            await _friendRequestService.SendRequestAsync(_found.UserId);
            // 버튼을 "요청 전송됨" 상태로 변경
            _btnAdd.Text      = "요청 전송됨";
            _btnAdd.BackColor = Color.FromArgb(39, 174, 96);
            _lblMsg.Location  = new Point(Mx, ContentY + 118);
            ShowMsg($"{_found.DisplayName}님에게 친구 요청을 보냈습니다. 수락을 기다리는 중...", Meta);
        }
        catch
        {
            _btnAdd.Enabled = true;
            _btnAdd.Text    = "친구 추가";
            _lblMsg.Location = new Point(Mx, ContentY + 118);
            ShowMsg("요청 전송에 실패했습니다.", ErrRed);
        }
    }

    private void ShowMsg(string text, Color color)
    {
        _lblMsg.Text      = text;
        _lblMsg.ForeColor = color;
        _lblMsg.Visible   = true;
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
