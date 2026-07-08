#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Windows.Forms;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Forms
{
    public partial class InviteMemberDialog : Form
    {
        // ── Design tokens ────────────────────────────────────────────
        private static readonly Color Canvas   = Color.White;
        private static readonly Color Surface  = Color.FromArgb(245, 245, 245);
        private static readonly Color Hairline = Color.FromArgb(238, 238, 238);
        private static readonly Color Border   = Color.FromArgb(221, 221, 221);
        private static readonly Color Body     = Color.FromArgb(34, 34, 34);
        private static readonly Color Meta     = Color.FromArgb(153, 153, 153);

        // ── Fields ───────────────────────────────────────────────────
        private readonly ChatRoom _room;
        private readonly IReadOnlyList<Contact> _contacts;
        private readonly IReadOnlySet<string> _existingMemberUserIds;

        private readonly List<ContactListItem> _items = new();

        private Panel     pnlBody    = null!;
        private Panel     pnlBottom  = null!;
        private Button    btnInvite  = null!;
        private TextBox   txtSearch  = null!;
        private FlowLayoutPanel flpContacts = null!;

        // ── Constructor ──────────────────────────────────────────────
        public InviteMemberDialog(
            ChatRoom room,
            IReadOnlyList<Contact> contacts,
            IReadOnlySet<string> existingMemberUserIds)
        {
            _room                  = room;
            _contacts              = contacts;
            _existingMemberUserIds = existingMemberUserIds;

            InitializeComponent();
            BuildUi();
        }

        // ── Public surface ───────────────────────────────────────────
        public IReadOnlyList<Contact> SelectedContacts =>
            _items.Where(i => i.IsSelected).Select(i => i.Contact).ToList();

        // ── UI construction ──────────────────────────────────────────
        private void BuildUi()
        {
            BuildBottomBar();
            BuildBody();
        }

        private void BuildBody()
        {
            pnlBody = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Canvas,
                Padding   = Padding.Empty,
            };

            // Breadcrumb
            var lblBreadcrumb = new Label
            {
                Text      = _room.Name,
                Font      = new Font(Font.FontFamily, 8.5f),
                ForeColor = Meta,
                AutoSize  = true,
                Location  = new Point(20, 14),
            };

            // Heading  — AutoSize so the large bold text is never clipped
            var lblHeading = new Label
            {
                Text      = "새 멤버 초대.",
                Font      = new Font("맑은 고딕", 17f, FontStyle.Bold),
                ForeColor = Body,
                AutoSize  = true,
                Location  = new Point(20, 32),
            };

            // Subtitle  — width deferred to pnlBody.Resize
            var lblSubtitle = new Label
            {
                Text      = "초대 시 방의 AES 공유키가 멤버의 RSA 공개키로 암호화 전달됩니다.",
                Font      = new Font(Font.FontFamily, 8.5f),
                ForeColor = Meta,
                Location  = new Point(20, 84),
                AutoSize  = false,
                Size      = new Size(10, 32),   // width set in Resize below
            };

            // Search frame  — width deferred to pnlBody.Resize
            var pnlSearchFrame = new Panel
            {
                Location  = new Point(16, 124),
                Size      = new Size(10, 44),   // width set in Resize below
                BackColor = Canvas,
            };
            pnlSearchFrame.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new RectangleF(0.5f, 0.5f, pnlSearchFrame.Width - 1f, pnlSearchFrame.Height - 1f);
                using var path = RoundedRect(rect, 4f);
                using var pen  = new Pen(Border);
                e.Graphics.DrawPath(pen, path);
            };

            var pnlSearchInner = new Panel
            {
                Location  = new Point(1, 1),
                Size      = new Size(pnlSearchFrame.Width - 2, pnlSearchFrame.Height - 2),
                BackColor = Surface,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            };
            pnlSearchFrame.Resize += (_, _) =>
                pnlSearchInner.Size = new Size(pnlSearchFrame.Width - 2, pnlSearchFrame.Height - 2);

            var lblQ = new Label
            {
                Text      = "Q",
                Font      = new Font(Font.FontFamily, 9f),
                ForeColor = Meta,
                AutoSize  = false,
                Size      = new Size(28, pnlSearchInner.Height),
                Location  = new Point(8, 0),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom,
            };

            txtSearch = new TextBox
            {
                BorderStyle     = BorderStyle.None,
                BackColor       = Surface,
                Font            = new Font(Font.FontFamily, 9.5f),
                Location        = new Point(36, (pnlSearchInner.Height - 20) / 2),
                Size            = new Size(pnlSearchInner.Width - 40, 20),
                PlaceholderText = "이름 또는 @username...",
                Anchor          = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            };
            pnlSearchInner.Resize += (_, _) =>
                txtSearch.Size = new Size(pnlSearchInner.Width - 40, txtSearch.Height);
            txtSearch.TextChanged += OnSearchChanged;

            pnlSearchInner.Controls.Add(lblQ);
            pnlSearchInner.Controls.Add(txtSearch);
            pnlSearchFrame.Controls.Add(pnlSearchInner);

            // Contacts label  (Y = 124 + 44 + 8 = 176)
            var lblContacts = new Label
            {
                Text      = "연락처",
                Font      = new Font(Font.FontFamily, 8.5f, FontStyle.Bold),
                ForeColor = Meta,
                AutoSize  = true,
                Location  = new Point(20, 176),
            };

            // Contacts flow panel  (Y = 176 + 20 + 4 = 200)
            // Size is set after the panel is added to pnlBody via Resize event to avoid 0-size bug
            flpContacts = new FlowLayoutPanel
            {
                Location      = new Point(0, 200),
                Size          = new Size(1, 1),
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoScroll    = true,
                BackColor     = Canvas,
                Anchor        = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            };
            pnlBody.Resize += (_, _) =>
            {
                int bw = pnlBody.ClientSize.Width;
                int bh = pnlBody.ClientSize.Height;
                if (bw <= 0) return;

                int innerW = bw - 40;  // 20px padding each side
                lblSubtitle.Size   = new Size(innerW, 32);
                pnlSearchFrame.Size = new Size(bw - 32, 44);

                if (bh > 0)
                {
                    flpContacts.Size = new Size(bw, Math.Max(1, bh - 200));
                    foreach (ContactListItem item in flpContacts.Controls)
                        item.Width = flpContacts.ClientSize.Width;
                }
            };

            foreach (var contact in _contacts)
            {
                bool isExisting = _existingMemberUserIds.Contains(contact.UserId);
                var item = new ContactListItem(contact, isExisting, flpContacts.Width);
                item.SelectionChanged += OnItemSelectionChanged;
                _items.Add(item);
                flpContacts.Controls.Add(item);
            }

            pnlBody.Controls.Add(lblBreadcrumb);
            pnlBody.Controls.Add(lblHeading);
            pnlBody.Controls.Add(lblSubtitle);
            pnlBody.Controls.Add(pnlSearchFrame);
            pnlBody.Controls.Add(lblContacts);
            pnlBody.Controls.Add(flpContacts);
            Controls.Add(pnlBody);
        }

        private void BuildBottomBar()
        {
            pnlBottom = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 56,
                BackColor = Canvas,
            };
            pnlBottom.Paint += (_, e) =>
            {
                using var pen = new Pen(Hairline);
                e.Graphics.DrawLine(pen, 0, 0, pnlBottom.Width, 0);
            };

            var lblWarning = new Label
            {
                Text      = "그룹 보안 한계 — 멤버 변경 시 키 회전 없이 이전 멤버가 과거 메시지를 볼 수 있습니다.",
                Font      = new Font(Font.FontFamily, 8f),
                ForeColor = Meta,
                Location  = new Point(12, 8),
                Size      = new Size(300, 40),
            };

            btnInvite = new Button
            {
                Text      = "초대",
                Font      = new Font(Font.FontFamily, 9.5f, FontStyle.Bold),
                BackColor = Body,
                ForeColor = Canvas,
                Size      = new Size(84, 36),
                FlatStyle = FlatStyle.Flat,
                Enabled   = false,
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            };
            btnInvite.FlatAppearance.BorderSize = 0;
            btnInvite.Location = new Point(pnlBottom.Width - 84 - 12, 10);
            pnlBottom.Resize  += (_, _) => btnInvite.Location = new Point(pnlBottom.Width - 84 - 12, 10);
            ApplyRadius(btnInvite, 4);
            btnInvite.Click += (_, _) => { DialogResult = DialogResult.OK; Close(); };

            pnlBottom.Controls.Add(lblWarning);
            pnlBottom.Controls.Add(btnInvite);
            Controls.Add(pnlBottom);
        }

        // ── Event handlers ───────────────────────────────────────────
        private void OnItemSelectionChanged(object? sender, bool selected)
        {
            btnInvite.Enabled = _items.Any(i => i.IsSelected);
        }

        private void OnSearchChanged(object? sender, EventArgs e)
        {
            string q = txtSearch.Text.Trim().ToLowerInvariant();
            foreach (var item in _items)
            {
                bool match = string.IsNullOrEmpty(q)
                    || item.Contact.DisplayName.ToLowerInvariant().Contains(q)
                    || item.Contact.Username.ToLowerInvariant().Contains(q);
                item.Visible = match;
            }
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

        private static GraphicsPath RoundedRect(RectangleF b, float r)
        {
            var path = new GraphicsPath();
            path.AddArc(b.X, b.Y, r * 2, r * 2, 180, 90);
            path.AddArc(b.Right - r * 2, b.Y, r * 2, r * 2, 270, 90);
            path.AddArc(b.Right - r * 2, b.Bottom - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(b.X, b.Bottom - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static void ApplyRadius(Control c, int r)
        {
            var path = new GraphicsPath();
            var rect = new RectangleF(0, 0, c.Width, c.Height);
            path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
            path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
            path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            c.Region = new Region(path);
        }

        // ── Inner class: ContactListItem ─────────────────────────────
        private sealed class ContactListItem : Panel
        {
            private static readonly Color Canvas   = Color.White;
            private static readonly Color Surface  = Color.FromArgb(245, 245, 245);
            private static readonly Color Hairline = Color.FromArgb(238, 238, 238);
            private static readonly Color Border   = Color.FromArgb(221, 221, 221);
            private static readonly Color Body     = Color.FromArgb(34, 34, 34);
            private static readonly Color Meta     = Color.FromArgb(153, 153, 153);

            public Contact Contact          { get; }
            public bool    IsExistingMember { get; }
            public bool    IsSelected       { get; private set; }

            public event EventHandler<bool>? SelectionChanged;

            private bool _hover;

            public ContactListItem(Contact contact, bool isExistingMember, int width)
            {
                Contact          = contact;
                IsExistingMember = isExistingMember;

                Height           = 64;
                Width            = width;
                BackColor        = Canvas;
                Cursor           = isExistingMember ? Cursors.Default : Cursors.Hand;
                DoubleBuffered   = true;
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                base.OnMouseEnter(e);
                if (!IsExistingMember) { _hover = true; Invalidate(); }
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                base.OnMouseLeave(e);
                _hover = false; Invalidate();
            }

            protected override void OnClick(EventArgs e)
            {
                base.OnClick(e);
                if (IsExistingMember) return;
                IsSelected = !IsSelected;
                Invalidate();
                SelectionChanged?.Invoke(this, IsSelected);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode      = SmoothingMode.AntiAlias;
                g.TextRenderingHint  = TextRenderingHint.ClearTypeGridFit;

                // Background
                g.Clear(_hover && !IsExistingMember ? Surface : Canvas);

                int cy = Height / 2;

                // Checkbox 18×18 at x=12
                var cbRect = new RectangleF(12, cy - 9, 18, 18);
                if (IsSelected)
                {
                    using var fillBrush = new SolidBrush(Body);
                    using var cbPath    = RoundedRect(cbRect, 4f);
                    g.FillPath(fillBrush, cbPath);
                    using var checkFont = new Font(FontFamily.GenericSansSerif, 9f, FontStyle.Bold);
                    using var wBrush    = new SolidBrush(Canvas);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("✓", checkFont, wBrush, cbRect, sf);
                }
                else
                {
                    using var fillBrush = new SolidBrush(Surface);
                    using var cbPath    = RoundedRect(cbRect, 4f);
                    g.FillPath(fillBrush, cbPath);
                    using var borderPen = new Pen(IsExistingMember ? Border : Border);
                    g.DrawPath(borderPen, cbPath);
                }

                // Avatar 36×36 at x=40
                var avRect = new RectangleF(40, cy - 18, 36, 36);
                using (var avPath = RoundedRect(avRect, 6f))
                using (var avBrush = new SolidBrush(IsExistingMember
                    ? Color.FromArgb(180, 180, 180)
                    : GetAvatarColor(Contact.UserId)))
                {
                    g.FillPath(avBrush, avPath);
                }
                string initial = string.IsNullOrEmpty(Contact.DisplayName) ? "?" : Contact.DisplayName[0].ToString().ToUpperInvariant();
                using (var avFont  = new Font(FontFamily.GenericSansSerif, 12f, FontStyle.Bold))
                using (var avBrush = new SolidBrush(Canvas))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(initial, avFont, avBrush, avRect, sf);
                }

                // Name
                Color nameColor = IsExistingMember ? Meta : Body;
                using (var nameFont  = new Font(FontFamily.GenericSansSerif, 9.5f, FontStyle.Bold))
                using (var nameBrush = new SolidBrush(nameColor))
                    g.DrawString(Contact.DisplayName, nameFont, nameBrush, 86, cy - 14);

                // Username
                using (var userFont  = new Font(FontFamily.GenericSansSerif, 8.5f))
                using (var userBrush = new SolidBrush(Meta))
                    g.DrawString("@" + Contact.Username, userFont, userBrush, 86, cy + 2);

                // "이미 참여" chip on right
                if (IsExistingMember)
                {
                    const string chipText = "이미 참여";
                    using var chipFont  = new Font(FontFamily.GenericSansSerif, 8f);
                    var textSize = g.MeasureString(chipText, chipFont);
                    float chipW  = textSize.Width + 16;
                    float chipH  = 22;
                    float chipX  = Width - chipW - 12;
                    float chipY  = cy - chipH / 2;
                    var chipRect = new RectangleF(chipX, chipY, chipW, chipH);
                    using var chipPath   = RoundedRect(chipRect, 6f);
                    using var chipFill   = new SolidBrush(Surface);
                    using var chipBorder = new Pen(Border);
                    g.FillPath(chipFill, chipPath);
                    g.DrawPath(chipBorder, chipPath);
                    using var chipBrush = new SolidBrush(Meta);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(chipText, chipFont, chipBrush, chipRect, sf);
                }

                // Hairline divider from x=86 to right
                using var hairPen = new Pen(Hairline);
                g.DrawLine(hairPen, 86, Height - 1, Width, Height - 1);
            }

            private static GraphicsPath RoundedRect(RectangleF b, float r)
            {
                var path = new GraphicsPath();
                path.AddArc(b.X, b.Y, r * 2, r * 2, 180, 90);
                path.AddArc(b.Right - r * 2, b.Y, r * 2, r * 2, 270, 90);
                path.AddArc(b.Right - r * 2, b.Bottom - r * 2, r * 2, r * 2, 0, 90);
                path.AddArc(b.X, b.Bottom - r * 2, r * 2, r * 2, 90, 90);
                path.CloseFigure();
                return path;
            }
        }
    }
}
