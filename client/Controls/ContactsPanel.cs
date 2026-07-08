#nullable enable
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Controls;

public partial class ContactsPanel : UserControl
{
    // ── Design tokens ────────────────────────────────────────
    private static readonly Color Canvas   = Color.White;
    private static readonly Color Surface  = Color.FromArgb(245, 245, 245);
    private static readonly Color Hairline = Color.FromArgb(238, 238, 238);
    private static readonly Color Border   = Color.FromArgb(221, 221, 221);
    private static readonly Color Body     = Color.FromArgb(34, 34, 34);
    private static readonly Color Meta     = Color.FromArgb(153, 153, 153);
    private static readonly Color Teal     = Color.FromArgb(26, 188, 156);

    private readonly IContactService       _contactService;
    private readonly IAuthService          _authService;
    private readonly IFriendRequestService _friendRequestService;

    private List<Contact> _allContacts = [];
    private string        _activeTab   = "all";
    private string        _searchQuery = "";

    public event EventHandler?          CloseRequested    = delegate { };
    public event EventHandler<Contact>? OpenChatRequested;
    public event EventHandler<Contact>? ProfileRequested;

    // ── Controls ─────────────────────────────────────────────
    private Panel          pnlHeader    = null!;
    private Panel          pnlSearchBar = null!;
    private Panel          pnlTabBar    = null!;
    private FlowLayoutPanel flpList     = null!;
    private Panel          pnlStatus    = null!;
    private TextBox        txtSearch    = null!;
    private Label          lblCount     = null!;
    private Label          lblStatus    = null!;

    // Tab buttons
    private Button btnTabAll       = null!;
    private Button btnTabOnline    = null!;
    private Button btnTabVerified  = null!;
    private Button btnTabBlocked   = null!;

    public ContactsPanel(IContactService contactService, IAuthService authService,
                         IFriendRequestService friendRequestService)
    {
        _contactService       = contactService;
        _authService          = authService;
        _friendRequestService = friendRequestService;

        InitializeComponent();
        Dock           = DockStyle.Fill;
        BackColor      = Canvas;
        DoubleBuffered = true;

        BuildUI();
        _ = LoadAsync();
    }

    // ─────────────────────────────────────────────────────────
    //  UI construction
    // ─────────────────────────────────────────────────────────

    private void BuildUI()
    {
        BuildStatusBar();  // Dock=Bottom — 먼저
        BuildList();       // Dock=Fill   — 먼저
        BuildTabBar();     // Dock=Top    — 나중일수록 위로
        BuildSearchBar();  // Dock=Top
        BuildHeader();     // Dock=Top    — 마지막 → 최상단
    }

    private void BuildHeader()
    {
        pnlHeader = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 72,
            BackColor = Canvas,
        };
        pnlHeader.Paint += (_, e) =>
        {
            using var pen = new Pen(Hairline);
            e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
        };

        var lblTitle = new Label
        {
            Text      = "연락처",
            Font      = new Font("맑은 고딕", 18f, FontStyle.Bold),
            ForeColor = Body,
            AutoSize  = true,
            Location  = new Point(20, 20),
        };

        lblCount = new Label
        {
            Text      = "0",
            Font      = new Font("맑은 고딕", 18f, FontStyle.Bold),
            ForeColor = Meta,
            AutoSize  = true,
        };
        lblTitle.SizeChanged += (_, _) => lblCount.Location = new Point(lblTitle.Right + 8, lblTitle.Top);
        lblCount.Location = new Point(lblTitle.Right + 8, lblTitle.Top);

        var btnAddFriend = new Button
        {
            Text      = "+ 친구 추가",
            Font      = new Font("맑은 고딕", 9f, FontStyle.Bold),
            BackColor = Teal,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            AutoSize  = false,
            Size      = new Size(130, 34),
            Cursor    = Cursors.Hand,
            Anchor    = AnchorStyles.Top | AnchorStyles.Right,
        };
        btnAddFriend.FlatAppearance.BorderSize         = 0;
        btnAddFriend.FlatAppearance.MouseOverBackColor = Color.FromArgb(22, 160, 133);
        ApplyRadius(btnAddFriend, 8);
        pnlHeader.Resize += (_, _) => btnAddFriend.Location = new Point(pnlHeader.Width - 146, 19);
        btnAddFriend.Location = new Point(pnlHeader.Width - 146, 19);
        btnAddFriend.Click += async (_, _) =>
        {
            using var dlg = new SecureChat.Forms.AddFriendDialog(_contactService, _friendRequestService);
            dlg.ShowDialog(FindForm());
            // 다이얼로그 닫힌 후 목록 갱신
            _allContacts = await _contactService.GetContactsAsync();
            if (!IsDisposed) RefreshUI();
        };

        pnlHeader.Controls.AddRange([lblTitle, lblCount, btnAddFriend]);
        Controls.Add(pnlHeader);
    }

    private void BuildSearchBar()
    {
        pnlSearchBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 56,
            BackColor = Canvas,
        };

        var pnlFrame = new Panel
        {
            BackColor = Surface,
            Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        pnlFrame.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            // 둥근 배경
            var r = new RectangleF(0.5f, 0.5f, pnlFrame.Width - 1f, pnlFrame.Height - 1f);
            using var path = RoundedRect(r, 8f);
            g.FillPath(new SolidBrush(Surface), path);
            g.DrawPath(new Pen(Border), path);
            // 돋보기 아이콘
            using var iconFont = new Font("맑은 고딕", 11f);
            g.DrawString("🔍", iconFont, new SolidBrush(Meta),
                new RectangleF(6, 0, 28, pnlFrame.Height),
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        };

        txtSearch = new TextBox
        {
            BorderStyle     = BorderStyle.None,
            BackColor       = Surface,
            Font            = new Font("맑은 고딕", 9.5f),
            PlaceholderText = "이름 또는 @username...",
            Anchor          = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        txtSearch.TextChanged += (_, _) => { _searchQuery = txtSearch.Text.Trim(); ApplyFilter(); };
        void PositionSearchBox()
        {
            int ty = (pnlFrame.Height - txtSearch.Height) / 2;
            txtSearch.Location = new Point(36, Math.Max(0, ty));
            txtSearch.Size     = new Size(Math.Max(1, pnlFrame.Width - 44), txtSearch.Height);
        }
        pnlFrame.Resize += (_, _) => PositionSearchBox();

        pnlFrame.Controls.Add(txtSearch);

        var btnFilter = new Button
        {
            Text      = "▽",
            Font      = new Font("맑은 고딕", 9f),
            ForeColor = Meta,
            FlatStyle = FlatStyle.Flat,
            BackColor = Canvas,
            Size      = new Size(36, 36),
            Cursor    = Cursors.Hand,
            Anchor    = AnchorStyles.Top | AnchorStyles.Right,
        };
        btnFilter.FlatAppearance.BorderColor = Border;
        btnFilter.FlatAppearance.BorderSize  = 1;
        btnFilter.FlatAppearance.MouseOverBackColor = Surface;

        pnlSearchBar.Resize += (_, _) =>
        {
            int h = pnlSearchBar.Height;
            int frameW = pnlSearchBar.Width - 20 - 8 - 36;
            pnlFrame.Bounds   = new Rectangle(10, (h - 36) / 2, Math.Max(1, frameW), 36);
            btnFilter.Location = new Point(pnlSearchBar.Width - 10 - 36, (h - 36) / 2);
        };

        pnlSearchBar.Controls.AddRange([pnlFrame, btnFilter]);
        Controls.Add(pnlSearchBar);
    }

    private void BuildTabBar()
    {
        pnlTabBar = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 52,
            BackColor = Canvas,
        };

        (string key, string label)[] tabs =
        [
            ("all",      "전체"),
            ("online",   "온라인"),
            ("verified", "검증됨"),
            ("blocked",  "차단"),
        ];

        var buttons = new List<Button>();
        foreach (var (key, label) in tabs)
        {
            var btn = new Button
            {
                Tag       = key,
                Text      = label,
                Font      = new Font("맑은 고딕", 9f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Surface,
                ForeColor = Body,
                Cursor    = Cursors.Hand,
                Size      = new Size(80, 32),
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Hairline;
            btn.Click += OnTabClick;
            buttons.Add(btn);

            if (key == "all") { btnTabAll      = btn; }
            else if (key == "online")   { btnTabOnline   = btn; }
            else if (key == "verified") { btnTabVerified = btn; }
            else                        { btnTabBlocked  = btn; }
        }

        var pnlPill = new Panel
        {
            BackColor = Surface,
            Anchor    = AnchorStyles.Top | AnchorStyles.Left,
        };
        pnlPill.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = RoundedRect(new RectangleF(0, 0, pnlPill.Width, pnlPill.Height), 8f);
            e.Graphics.FillPath(new SolidBrush(Surface), path);
            e.Graphics.DrawPath(new Pen(Hairline), path);
        };

        int bx = 4;
        foreach (var btn in buttons)
        {
            btn.Location = new Point(bx, 4);
            pnlPill.Controls.Add(btn);
            bx += btn.Width + 2;
        }
        pnlPill.Size = new Size(bx + 4, 40);

        pnlTabBar.Resize += (_, _) => pnlPill.Location = new Point(10, (pnlTabBar.Height - pnlPill.Height) / 2);
        pnlPill.Location = new Point(10, (pnlTabBar.Height - pnlPill.Height) / 2);

        pnlTabBar.Controls.Add(pnlPill);
        Controls.Add(pnlTabBar);

        UpdateTabStyles();
    }

    private void BuildList()
    {
        flpList = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            AutoScroll    = true,
            BackColor     = Canvas,
            Padding       = new Padding(0, 8, 0, 8),
        };
        flpList.SizeChanged += (_, _) =>
        {
            foreach (Control c in flpList.Controls)
                c.Width = Math.Max(1, flpList.ClientSize.Width);
        };

        // 자식 컨트롤의 마우스 휠 이벤트를 flpList로 전달
        flpList.ControlAdded += (_, e) => { if (e.Control != null) RedirectWheelToList(e.Control); };
        flpList.MouseWheel   += FlpList_MouseWheel;

        Controls.Add(flpList);
    }

    private void BuildStatusBar()
    {
        pnlStatus = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 32,
            BackColor = Surface,
        };
        pnlStatus.Paint += (_, e) =>
        {
            using var pen = new Pen(Hairline);
            e.Graphics.DrawLine(pen, 0, 0, pnlStatus.Width, 0);
        };

        lblStatus = new Label
        {
            Text      = "● 공개키 캐시 0개",
            Font      = new Font("맑은 고딕", 8.5f),
            ForeColor = Body,
            AutoSize  = true,
            Location  = new Point(12, 8),
        };

        pnlStatus.Controls.Add(lblStatus);
        Controls.Add(pnlStatus);
    }

    // ─────────────────────────────────────────────────────────
    //  Data
    // ─────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        _allContacts = await _contactService.GetContactsAsync();
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(RefreshUI); return; }
        RefreshUI();
    }

    private void RefreshUI()
    {
        int total = _allContacts.Count;
        lblCount.Text  = total.ToString();
        lblStatus.Text = $"● 공개키 캐시 {total}개";
        UpdateTabCounts();
        ApplyFilter();
    }

    private void UpdateTabCounts()
    {
        int unblocked = _allContacts.Count(c => !c.IsBlocked);
        int blocked   = _allContacts.Count(c =>  c.IsBlocked);
        btnTabAll.Text      = $"전체 {unblocked}";
        btnTabOnline.Text   = $"온라인 {unblocked}";
        btnTabVerified.Text = $"검증됨 {_allContacts.Count(c => c.IsVerified && !c.IsBlocked)}";
        btnTabBlocked.Text  = $"차단 {blocked}";
    }

    private void ApplyFilter()
    {
        var me = _authService.CurrentUser?.Username ?? "";

        IEnumerable<Contact> filtered = _activeTab switch
        {
            "verified" => _allContacts.Where(c => c.IsVerified && !c.IsBlocked),
            "blocked"  => _allContacts.Where(c => c.IsBlocked),
            _          => _allContacts.Where(c => !c.IsBlocked),
        };

        if (!string.IsNullOrEmpty(_searchQuery))
            filtered = filtered.Where(c =>
                c.DisplayName.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase) ||
                c.Username.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));

        RenderContacts(filtered.ToList(), me);
    }

    // ─────────────────────────────────────────────────────────
    //  Render
    // ─────────────────────────────────────────────────────────

    private void RenderContacts(List<Contact> contacts, string myUsername)
    {
        flpList.SuspendLayout();
        flpList.Controls.Clear();

        int listW = Math.Max(1, flpList.ClientSize.Width);

        string? lastInitial = null;
        foreach (var contact in contacts.OrderBy(c => c.DisplayName))
        {
            string initial = GetKoreanInitial(contact.DisplayName);
            if (initial != lastInitial)
            {
                flpList.Controls.Add(MakeSectionHeader(initial, listW));
                lastInitial = initial;
            }
            bool isSelf = contact.Username.Equals(myUsername, StringComparison.OrdinalIgnoreCase);
            flpList.Controls.Add(MakeContactRow(contact, isSelf, listW));
        }

        flpList.ResumeLayout();
    }

    private static Panel MakeSectionHeader(string initial, int w)
    {
        var pnl = new Panel { Size = new Size(w, 32), BackColor = Canvas };
        var lbl = new Label
        {
            Text      = initial,
            Font      = new Font("맑은 고딕", 9f),
            ForeColor = Meta,
            AutoSize  = true,
            Location  = new Point(20, 8),
        };
        pnl.Controls.Add(lbl);
        return pnl;
    }

    private Panel MakeContactRow(Contact contact, bool isSelf, int w)
    {
        var pnl = new Panel { Size = new Size(w, 64), BackColor = Canvas, Cursor = Cursors.Hand };

        // Avatar
        string ini  = string.IsNullOrEmpty(contact.DisplayName) ? "?" : contact.DisplayName[0].ToString().ToUpperInvariant();
        var pnlAv   = new Panel { Location = new Point(20, 14), Size = new Size(36, 36), BackColor = Color.Transparent };
        var avColor = GetAvatarColor(contact.UserId);
        pnlAv.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            using var path = RoundedRect(new RectangleF(0, 0, 36, 36), 8f);
            g.FillPath(new SolidBrush(avColor), path);
            using var fnt = new Font("맑은 고딕", 13f, FontStyle.Bold);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(ini, fnt, Brushes.White, new RectangleF(0, 0, 36, 36), sf);
        };
        pnl.Controls.Add(pnlAv);

        // Name row
        int textX = 68;
        var lblName = new Label
        {
            Text      = contact.DisplayName + (isSelf ? " (나)" : ""),
            Font      = new Font("맑은 고딕", 9.5f, FontStyle.Bold),
            ForeColor = Body,
            AutoSize  = true,
            Location  = new Point(textX, 12),
        };
        pnl.Controls.Add(lblName);

        // Verified badge
        if (contact.IsVerified)
        {
            var lblBadge = new Label
            {
                Text      = "🛡️",
                Font      = new Font("맑은 고딕", 9f),
                AutoSize  = true,
                BackColor = Color.Transparent,
            };
            lblName.SizeChanged += (_, _) => lblBadge.Location = new Point(lblName.Right + 4, lblName.Top);
            lblBadge.Location = new Point(lblName.Right + 4, lblName.Top);
            pnl.Controls.Add(lblBadge);
        }

        // Status + username
        var lblSub = new Label
        {
            Text      = $"● 온라인   @{contact.Username}",
            Font      = new Font("맑은 고딕", 8.5f),
            ForeColor = Meta,
            AutoSize  = true,
            Location  = new Point(textX, 33),
        };
        pnl.Controls.Add(lblSub);

        // Action buttons (not for self)
        if (!isSelf)
        {
            var btnDm   = MakeIconBtn("▷",  10f);
            var btnMore = MakeIconBtn("⋯", 12f);
            void PositionActions()
            {
                btnMore.Location = new Point(pnl.Width - 46, 17);
                btnDm.Location   = new Point(pnl.Width - 86, 17);
            }
            pnl.Resize += (_, _) => PositionActions();
            PositionActions();

            btnDm.Click   += (_, _) => OpenChatRequested?.Invoke(this, contact);
            btnMore.Click += (_, _) => ShowContactMenu(btnMore, contact);
            pnl.Controls.AddRange([btnDm, btnMore]);
        }

        // 행 클릭 → 프로필 열기 (본인 제외)
        if (!isSelf)
            pnl.Click += (_, _) => ProfileRequested?.Invoke(this, contact);

        // Hover effect
        pnl.MouseEnter += (_, _) => { pnl.BackColor = Surface; pnlAv.Invalidate(); };
        pnl.MouseLeave += (_, _) => { pnl.BackColor = Canvas; pnlAv.Invalidate(); };

        // Hairline divider
        pnl.Paint += (_, e) =>
        {
            using var pen = new Pen(Hairline);
            e.Graphics.DrawLine(pen, 20, pnl.Height - 1, pnl.Width - 20, pnl.Height - 1);
        };

        return pnl;
    }

    private static Button MakeIconBtn(string text, float fontSize)
    {
        var btn = new Button
        {
            Text      = text,
            Font      = new Font("맑은 고딕", fontSize, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 80, 80),
            Size      = new Size(36, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            Cursor    = Cursors.Hand,
        };
        btn.FlatAppearance.BorderSize         = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 220, 220);
        return btn;
    }

    // ─────────────────────────────────────────────────────────
    //  Tab events
    // ─────────────────────────────────────────────────────────

    private void ShowContactMenu(Control anchor, Contact contact)
    {
        var cms = new ContextMenuStrip();
        cms.Font = new Font("맑은 고딕", 9.5f);

        var itemChat = new ToolStripMenuItem("💬  대화 시작");
        itemChat.Click += (_, _) => OpenChatRequested?.Invoke(this, contact);
        cms.Items.Add(itemChat);

        cms.Items.Add(new ToolStripSeparator());

        var itemBlock = new ToolStripMenuItem("🚫  차단");
        itemBlock.Click += async (_, _) =>
        {
            var result = MessageBox.Show(
                $"{contact.DisplayName}님을 차단하시겠습니까?",
                "차단 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;
            await _contactService.BlockContactAsync(contact.UserId);
            _allContacts = await _contactService.GetContactsAsync();
            if (!IsDisposed) RefreshUI();
        };
        cms.Items.Add(itemBlock);

        var itemRemove = new ToolStripMenuItem("🗑  연락처 삭제");
        itemRemove.ForeColor = Color.FromArgb(192, 57, 43);
        itemRemove.Click += async (_, _) =>
        {
            var result = MessageBox.Show(
                $"{contact.DisplayName}님을 연락처에서 삭제하시겠습니까?",
                "삭제 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;
            await _contactService.RemoveContactAsync(contact.UserId);
            _allContacts = await _contactService.GetContactsAsync();
            if (!IsDisposed) RefreshUI();
        };
        cms.Items.Add(itemRemove);

        cms.Show(anchor, new Point(0, anchor.Height));
    }

    private void OnTabClick(object? sender, EventArgs e)
    {
        if (sender is Button btn && btn.Tag is string key && key != _activeTab)
        {
            _activeTab = key;
            UpdateTabStyles();
            ApplyFilter();
        }
    }

    private void UpdateTabStyles()
    {
        foreach (Button btn in new[] { btnTabAll, btnTabOnline, btnTabVerified, btnTabBlocked })
        {
            if (btn == null) continue;
            bool active = (string?)btn.Tag == _activeTab;
            btn.BackColor = active ? Canvas : Surface;
            btn.ForeColor = active ? Body   : Meta;
            btn.Font      = new Font("맑은 고딕", 9f, active ? FontStyle.Bold : FontStyle.Regular);
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────

    private static string GetKoreanInitial(string name)
    {
        if (string.IsNullOrEmpty(name)) return "#";
        char c = name[0];
        if (c >= 0xAC00 && c <= 0xD7A3)
        {
            int idx = (c - 0xAC00) / 28 / 21;
            string[] initials = ["ㄱ","ㄲ","ㄴ","ㄷ","ㄸ","ㄹ","ㅁ","ㅂ","ㅃ","ㅅ","ㅆ","ㅇ","ㅈ","ㅉ","ㅊ","ㅋ","ㅌ","ㅍ","ㅎ"];
            return initials[idx];
        }
        return c.ToString().ToUpper();
    }

    private static Color GetAvatarColor(string seed)
    {
        Color[] palette =
        [
            Color.FromArgb( 52, 152, 219),  // 파란
            Color.FromArgb(231,  76,  60),  // 빨간
            Color.FromArgb( 46, 204, 113),  // 초록
            Color.FromArgb(155,  89, 182),  // 보라
            Color.FromArgb(230, 126,  34),  // 주황
        ];
        int idx = Math.Abs(seed.GetHashCode()) % palette.Length;
        return palette[idx];
    }

    private void RedirectWheelToList(Control c)
    {
        c.MouseWheel += FlpList_MouseWheel;
        foreach (Control child in c.Controls)
            RedirectWheelToList(child);
    }

    private void FlpList_MouseWheel(object? sender, MouseEventArgs e)
    {
        int delta = -(e.Delta / 120) * SystemInformation.MouseWheelScrollLines * 20;
        int newVal = Math.Clamp(flpList.VerticalScroll.Value + delta,
                                flpList.VerticalScroll.Minimum,
                                flpList.VerticalScroll.Maximum);
        flpList.VerticalScroll.Value = newVal;
        flpList.PerformLayout();
    }

    private static void ApplyRadius(Control c, int r)
    {
        void Update()
        {
            if (c.Width <= 0 || c.Height <= 0) return;
            var path = new GraphicsPath();
            path.AddArc(0, 0, r * 2, r * 2, 180, 90);
            path.AddArc(c.Width - r * 2, 0, r * 2, r * 2, 270, 90);
            path.AddArc(c.Width - r * 2, c.Height - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(0, c.Height - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            c.Region = new Region(path);
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
