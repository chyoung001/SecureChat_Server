using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SecureChat.Controls;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Forms;

public partial class CreateRoomDialog : Form
{
    // ── Musinsa design tokens ──────────────────────────────
    private static readonly Color Canvas   = Color.White;
    private static readonly Color Surface  = Color.FromArgb(245, 245, 245);  // #f5f5f5
    private static readonly Color Hairline = Color.FromArgb(238, 238, 238);  // #eeeeee
    private static readonly Color Border   = Color.FromArgb(221, 221, 221);  // #dddddd
    private static readonly Color Body     = Color.FromArgb(34,  34,  34);   // #222222
    private static readonly Color Meta     = Color.FromArgb(153, 153, 153);  // #999999

    // ── State ──────────────────────────────────────────────
    private readonly IContactService _contactService;
    private List<Contact> _allContacts = [];
    private readonly HashSet<string> _selectedIds = [];
    private bool _isDirect = true;

    // ── Results ────────────────────────────────────────────
    public List<Contact> SelectedContacts { get; private set; } = [];
    public bool IsDirectMessage => _isDirect;

    // ── Controls ───────────────────────────────────────────
    private Panel          pnlCard1v1       = null!;
    private Panel          pnlCardGroup     = null!;
    private FlowLayoutPanel flpChips        = null!;
    private TextBox        txtSearch        = null!;
    private Panel          pnlContactList   = null!;
    private FlowLayoutPanel flpContacts     = null!;
    private Label          lblSelectedCount = null!;
    private Button         btnCreate        = null!;
    private Button         btnCancel        = null!;

    public CreateRoomDialog(IContactService contactService)
    {
        _contactService = contactService;
        InitializeComponent();
        BuildUI();
        _ = LoadContactsAsync();
    }

    // ─────────────────────────────────────────────────────────
    //  UI construction
    // ─────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // ── Bottom action bar ─────────────────────────────────
        var pnlBottom = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 72,
            BackColor = Canvas
        };
        pnlBottom.Paint += (_, e) =>
        {
            using var pen = new Pen(Hairline);
            e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
        };

        btnCreate = new Button
        {
            Text      = "대화 시작",
            Font      = new Font("맑은 고딕", 9.5F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Black,
            ForeColor = Color.White,
            Size      = new Size(120, 40),
            Location  = new Point(ClientSize.Width - 16 - 120, 16),
            Cursor    = Cursors.Hand,
            Enabled   = false
        };
        btnCreate.FlatAppearance.BorderSize = 0;
        btnCreate.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 34, 34);
        ApplyRadius(btnCreate, 4);
        btnCreate.Click += BtnCreate_Click;

        btnCancel = new Button
        {
            Text      = "취소",
            Font      = new Font("맑은 고딕", 9.5F),
            FlatStyle = FlatStyle.Flat,
            BackColor = Canvas,
            ForeColor = Body,
            Size      = new Size(80, 40),
            Location  = new Point(btnCreate.Left - 8 - 80, 16),
            Cursor    = Cursors.Hand
        };
        btnCancel.FlatAppearance.BorderColor = Border;
        btnCancel.FlatAppearance.BorderSize  = 1;
        ApplyRadius(btnCancel, 4);
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        pnlBottom.Controls.AddRange([btnCreate, btnCancel]);

        // ── Contact list area (Fill) ──────────────────────────
        var pnlContactArea = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Canvas
        };

        var lblRecent = new Label
        {
            Text      = "친구 목록",
            Font      = new Font("맑은 고딕", 8.5F),
            ForeColor = Meta,
            BackColor = Canvas,
            Dock      = DockStyle.Top,
            Height    = 32,
            Padding   = new Padding(32, 10, 0, 0)
        };

        pnlContactList = new Panel
        {
            Dock       = DockStyle.Fill,
            BackColor  = Canvas,
            AutoScroll = false
        };

        flpContacts = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            AutoScroll    = false,
            AutoSize      = true,
            AutoSizeMode  = AutoSizeMode.GrowAndShrink,
            BackColor     = Canvas,
            Location      = Point.Empty
        };

        pnlContactList.Controls.Add(flpContacts);
        pnlContactList.SizeChanged += (_, _) =>
        {
            flpContacts.Width = pnlContactList.ClientSize.Width;
            foreach (ContactItemControl item in flpContacts.Controls)
                item.Width = Math.Max(1, pnlContactList.ClientSize.Width);
            ClampContactScroll();
        };

        Shown += (_, _) =>
        {
            // 폼이 표시된 후 패널 크기가 확정되므로 아이템 너비 재정렬
            flpContacts.Width = pnlContactList.ClientSize.Width;
            foreach (ContactItemControl item in flpContacts.Controls)
                item.Width = Math.Max(1, pnlContactList.ClientSize.Width);
        };
        pnlContactList.MouseWheel += ScrollContacts;
        flpContacts.MouseWheel    += ScrollContacts;

        pnlContactArea.Controls.Add(pnlContactList);
        pnlContactArea.Controls.Add(lblRecent);

        // ── Header / form fields (Top) ────────────────────────
        var pnlHeader = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 332,
            BackColor = Canvas
        };

        var lblTitle = new Label
        {
            Text      = "새 대화 시작",
            Font      = new Font("맑은 고딕", 22F, FontStyle.Bold),
            ForeColor = Color.Black,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(32, 28)
        };

        // 대화 유형 카드
        pnlCard1v1   = BuildTypeCard("1:1 대화", new Point(32, 100));
        pnlCardGroup = BuildTypeCard("그룹",     new Point(264, 100));

        pnlCard1v1.MouseClick   += (_, _) => SelectType(direct: true);
        pnlCardGroup.MouseClick += (_, _) => SelectType(direct: false);
        foreach (Control c in pnlCard1v1.Controls)
            c.MouseClick += (_, _) => SelectType(direct: true);
        foreach (Control c in pnlCardGroup.Controls)
            c.MouseClick += (_, _) => SelectType(direct: false);

        // 상대 선택 레이블
        var lblRecipient = new Label
        {
            Text      = "상대 선택",
            Font      = new Font("맑은 고딕", 8.5F),
            ForeColor = Meta,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(32, 200)
        };

        lblSelectedCount = new Label
        {
            Text      = "",
            Font      = new Font("맑은 고딕", 8.5F),
            ForeColor = Meta,
            BackColor = Color.Transparent,
            Size      = new Size(96, 16),
            TextAlign = ContentAlignment.MiddleRight,
            Location  = new Point(ClientSize.Width - 32 - 96, 200)
        };

        // 검색 인풋 — #dddddd 테두리, #f5f5f5 배경
        var pnlSearch = new Panel
        {
            Size      = new Size(456, 42),
            Location  = new Point(32, 224),
            BackColor = Border,   // 1px border frame
            Cursor    = Cursors.IBeam
        };
        var pnlSearchBody = new Panel
        {
            Location  = new Point(1, 1),
            Size      = new Size(454, 40),
            BackColor = Surface
        };
        txtSearch = new TextBox
        {
            BorderStyle     = BorderStyle.None,
            Font            = new Font("맑은 고딕", 9.5F),
            BackColor       = Surface,
            ForeColor       = Body,
            PlaceholderText = "이름 또는 @username 검색...",
            Location        = new Point(12, 11),
            Size            = new Size(430, 20)
        };
        txtSearch.TextChanged += (_, _) => FilterContacts();
        pnlSearchBody.Controls.Add(txtSearch);
        pnlSearch.Controls.Add(pnlSearchBody);

        // 선택된 연락처 칩
        flpChips = new FlowLayoutPanel
        {
            Location     = new Point(32, 276),
            Size         = new Size(456, 36),
            WrapContents = true,
            AutoSize     = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor    = Color.Transparent
        };

        pnlHeader.Controls.AddRange([
            lblTitle,
            pnlCard1v1, pnlCardGroup,
            lblRecipient, lblSelectedCount,
            pnlSearch, flpChips
        ]);

        Controls.Add(pnlContactArea);
        Controls.Add(pnlBottom);
        Controls.Add(pnlHeader);

        RefreshTypeCards();
    }

    private Panel BuildTypeCard(string title, Point location)
    {
        var card = new Panel
        {
            Size      = new Size(216, 68),
            Location  = location,
            BackColor = Surface,
            Cursor    = Cursors.Hand
        };

        var lblTitle = new Label
        {
            Text      = title,
            Font      = new Font("맑은 고딕", 9.5F, FontStyle.Bold),
            ForeColor = Color.Black,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(40, 24),
            Cursor    = Cursors.Hand
        };

        card.Controls.Add(lblTitle);

        card.Paint += (_, ev) =>
        {
            bool selected = (card == pnlCard1v1) ? _isDirect : !_isDirect;
            var g = ev.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
            using var path = RoundedRect(r, 4);

            // 배경: 선택 시 흰색, 비선택 시 #f5f5f5
            g.FillPath(selected ? Brushes.White : new SolidBrush(Surface), path);

            // 테두리: 선택 시 #000000 1.5px, 비선택 시 #eeeeee
            using var borderPen = new Pen(selected ? Color.Black : Hairline, selected ? 1.5f : 1f);
            g.DrawPath(borderPen, path);

            // 라디오 버튼
            int cx = 20, cy = 34, cr = 9;
            var radioRect = new Rectangle(cx - cr, cy - cr, cr * 2, cr * 2);
            if (selected)
            {
                g.FillEllipse(Brushes.Black, radioRect);
                g.FillEllipse(Brushes.White, cx - 4, cy - 4, 8, 8);
            }
            else
            {
                g.FillEllipse(new SolidBrush(Surface), radioRect);
                using var ePen = new Pen(Border, 1.5f);
                g.DrawEllipse(ePen, radioRect);
            }
        };

        return card;
    }

    // ─────────────────────────────────────────────────────────
    //  Data loading
    // ─────────────────────────────────────────────────────────

    private async Task LoadContactsAsync()
    {
        _allContacts = await _contactService.GetContactsAsync();
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(PopulateContacts); return; }
        PopulateContacts();
    }

    private void PopulateContacts()
    {
        flpContacts.SuspendLayout();
        flpContacts.Controls.Clear();

        for (int i = 0; i < _allContacts.Count; i++)
        {
            var contact = _allContacts[i];
            var item = new ContactItemControl(contact, isOnline: i % 2 == 0)
            {
                Width = Math.Max(1, pnlContactList.ClientSize.Width)
            };
            item.SelectionChanged += OnContactSelectionChanged;
            flpContacts.Controls.Add(item);
        }

        flpContacts.ResumeLayout();
    }

    // ─────────────────────────────────────────────────────────
    //  Interaction handlers
    // ─────────────────────────────────────────────────────────

    private void SelectType(bool direct)
    {
        _isDirect = direct;

        if (_isDirect && _selectedIds.Count > 1)
        {
            var first = _selectedIds.First();
            _selectedIds.Clear();
            _selectedIds.Add(first);
            foreach (ContactItemControl item in flpContacts.Controls)
                item.IsSelected = _selectedIds.Contains(item.Contact.UserId);
        }

        RefreshTypeCards();
        RebuildChips();
        UpdateCount();
    }

    private void OnContactSelectionChanged(object? sender, Contact contact)
    {
        if (sender is not ContactItemControl item) return;

        if (item.IsSelected)
        {
            if (_isDirect && _selectedIds.Count >= 1)
            {
                foreach (ContactItemControl other in flpContacts.Controls)
                {
                    if (other != item && other.IsSelected)
                    {
                        _selectedIds.Remove(other.Contact.UserId);
                        other.IsSelected = false;
                    }
                }
            }
            _selectedIds.Add(contact.UserId);
        }
        else
        {
            _selectedIds.Remove(contact.UserId);
        }

        RebuildChips();
        UpdateCount();
    }

    private void FilterContacts()
    {
        var q = txtSearch.Text.Trim().ToLowerInvariant();
        foreach (ContactItemControl item in flpContacts.Controls)
        {
            bool match = string.IsNullOrEmpty(q)
                || item.Contact.DisplayName.ToLowerInvariant().Contains(q)
                || item.Contact.Username.ToLowerInvariant().Contains(q);
            item.Visible = match;
        }
    }

    private void RefreshTypeCards()
    {
        pnlCard1v1.Invalidate();
        pnlCardGroup.Invalidate();
    }

    private void UpdateCount()
    {
        lblSelectedCount.Text = _selectedIds.Count > 0 ? $"{_selectedIds.Count}명 선택됨" : "";
        btnCreate.Enabled     = _selectedIds.Count > 0;
        // 비활성 상태 색상 조정
        btnCreate.BackColor   = btnCreate.Enabled ? Color.Black : Color.FromArgb(187, 187, 187);
    }

    private void RebuildChips()
    {
        flpChips.SuspendLayout();
        flpChips.Controls.Clear();

        foreach (var id in _selectedIds)
        {
            var contact = _allContacts.FirstOrDefault(c => c.UserId == id);
            if (contact == null) continue;
            flpChips.Controls.Add(BuildChip(contact));
        }

        flpChips.ResumeLayout();
    }

    private Panel BuildChip(Contact contact)
    {
        var chip = new Panel
        {
            Height    = 28,
            BackColor = Canvas,
            Margin    = new Padding(0, 0, 6, 4),
            Cursor    = Cursors.Hand
        };

        var lbl = new Label
        {
            Text      = contact.DisplayName + "  x",
            Font      = new Font("맑은 고딕", 8.5F),
            ForeColor = Body,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Location  = new Point(10, 5),
            Cursor    = Cursors.Hand
        };

        chip.Controls.Add(lbl);
        chip.Width = lbl.PreferredWidth + 20;

        // 선택 칩: #f5f5f5 bg, #eeeeee 테두리 (필터 칩 스타일)
        chip.Paint += (_, ev) =>
        {
            var g = ev.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = new Rectangle(0, 0, chip.Width - 1, chip.Height - 1);
            using var path = RoundedRect(r, 999);  // pill
            g.FillPath(new SolidBrush(Surface), path);
            using var pen = new Pen(Border, 1f);
            g.DrawPath(pen, path);
        };

        MouseEventHandler removeHandler = (_, _) =>
        {
            _selectedIds.Remove(contact.UserId);
            foreach (ContactItemControl item in flpContacts.Controls)
                if (item.Contact.UserId == contact.UserId)
                    item.IsSelected = false;
            RebuildChips();
            UpdateCount();
        };

        chip.MouseClick += removeHandler;
        lbl.MouseClick  += removeHandler;

        return chip;
    }

    private void BtnCreate_Click(object? sender, EventArgs e)
    {
        SelectedContacts = _allContacts.Where(c => _selectedIds.Contains(c.UserId)).ToList();
        DialogResult = DialogResult.OK;
        Close();
    }

    // ─────────────────────────────────────────────────────────
    //  Contact list scrolling
    // ─────────────────────────────────────────────────────────

    private int ContactScrollMax =>
        Math.Max(0, flpContacts.Height - pnlContactList.ClientSize.Height);

    private void ScrollContacts(object? sender, MouseEventArgs e)
    {
        int delta       = e.Delta > 0 ? 80 : -80;
        flpContacts.Top = Math.Max(-ContactScrollMax, Math.Min(0, flpContacts.Top + delta));
    }

    private void ClampContactScroll() =>
        flpContacts.Top = Math.Max(-ContactScrollMax, Math.Min(0, flpContacts.Top));

    // ─────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────

    private static void ApplyRadius(Control ctrl, int radius)
    {
        if (ctrl.Width <= 0 || ctrl.Height <= 0) return;
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(0, 0, d, d, 180, 90);
        path.AddArc(ctrl.Width - d, 0, d, d, 270, 90);
        path.AddArc(ctrl.Width - d, ctrl.Height - d, d, d, 0, 90);
        path.AddArc(0, ctrl.Height - d, d, d, 90, 90);
        path.CloseFigure();
        ctrl.Region = new Region(path);
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        if (radius >= 999)
        {
            // pill — radius = half height
            int rad = r.Height / 2;
            int d   = rad * 2;
            var pill = new GraphicsPath();
            pill.AddArc(r.X, r.Y, d, d, 180, 90);
            pill.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            pill.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            pill.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            pill.CloseFigure();
            return pill;
        }

        var path = new GraphicsPath();
        int diam = radius * 2;
        path.AddArc(r.X, r.Y, diam, diam, 180, 90);
        path.AddArc(r.Right - diam, r.Y, diam, diam, 270, 90);
        path.AddArc(r.Right - diam, r.Bottom - diam, diam, diam, 0, 90);
        path.AddArc(r.X, r.Bottom - diam, diam, diam, 90, 90);
        path.CloseFigure();
        return path;
    }
}
