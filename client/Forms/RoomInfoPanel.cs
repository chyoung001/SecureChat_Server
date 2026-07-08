#nullable enable
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Forms
{
    public partial class RoomInfoPanel : UserControl
    {
        private static readonly Color Canvas     = Color.White;
        private static readonly Color Surface    = Color.FromArgb(245, 245, 245);
        private static readonly Color Hairline   = Color.FromArgb(238, 238, 238);
        private static readonly Color Border     = Color.FromArgb(221, 221, 221);
        private static readonly Color Body       = Color.FromArgb(34, 34, 34);
        private static readonly Color Meta       = Color.FromArgb(153, 153, 153);
        private static readonly Color DangerBg   = Color.FromArgb(255, 240, 240);
        private static readonly Color DangerText = Color.FromArgb(220, 53, 69);

        private ChatRoom      _room;
        private readonly IAuthService _authService;
        private readonly IRoomService _roomService;

        public event EventHandler? CloseRequested;
        public event EventHandler? InviteRequested;
        public event EventHandler? LeaveRoomConfirmed;
        // arg: 위임 대상 UserId
        public event EventHandler<string>? TransferAdminRequested;

        private Panel   pnlScroll  = null!;
        private Panel   pnlContent = null!;
        private int     _builtForWidth = 0;

        // 강퇴·초대 완료 후 외부에서 호출해 멤버 목록을 최신 데이터로 교체한다.
        public void Refresh(ChatRoom updatedRoom)
        {
            _room          = updatedRoom;
            _builtForWidth = 0;   // 캐시 무효화 → 다음 TryRebuild에서 전체 재렌더
            TryRebuild();
        }

        public RoomInfoPanel(ChatRoom room, IAuthService authService, IRoomService roomService)
        {
            _room        = room;
            _authService = authService;
            _roomService = roomService;

            InitializeComponent();
            BackColor      = Canvas;
            DoubleBuffered = true;

            BuildCloseButton();
            BuildScrollShell();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            TryRebuild();
        }

        private void TryRebuild()
        {
            int w = pnlScroll?.ClientSize.Width ?? 0;
            if (w <= 0 || w == _builtForWidth) return;
            _builtForWidth = w;
            RebuildContent(w);
        }

        // ── Close button (floating, top-right) ───────────────────────

        private void BuildCloseButton()
        {
            var btnClose = new Button
            {
                Text      = "×",
                Font      = new Font("맑은 고딕", 13f),
                ForeColor = Meta,
                Size      = new Size(36, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Canvas,
                Cursor    = Cursors.Hand,
                Anchor    = AnchorStyles.Top | AnchorStyles.Right,
            };
            btnClose.FlatAppearance.BorderSize         = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Surface;
            btnClose.Click += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);

            Resize += (_, _) => btnClose.Location = new Point(Width - 40, 7);
            btnClose.Location = new Point(Width - 40, 7);

            Controls.Add(btnClose);
        }

        // ── Scroll shell ──────────────────────────────────────────────

        private void BuildScrollShell()
        {
            pnlScroll = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                BackColor  = Canvas,
            };
            pnlContent = new Panel
            {
                Location  = Point.Empty,
                BackColor = Canvas,
            };
            pnlScroll.Controls.Add(pnlContent);
            Controls.Add(pnlScroll);

            pnlScroll.Resize += (_, _) =>
            {
                int newW = pnlScroll.ClientSize.Width;
                if (newW > 0 && newW != _builtForWidth)
                {
                    _builtForWidth = 0; // force rebuild
                    TryRebuild();
                }
            };
        }

        // ── Content ───────────────────────────────────────────────────

        private void RebuildContent(int w)
        {
            pnlContent.Controls.Clear();
            pnlContent.Location = Point.Empty;

            int y   = 28;
            int pad = 24;

            // ── 1. Room name ──────────────────────────────────────────
            Add(Lbl(_room.Name, "맑은 고딕", 14f, FontStyle.Bold, Body, new Rectangle(pad, y, w - pad * 2, 56), ContentAlignment.MiddleCenter));
            y += 56 + 4;

            // ── 3. Meta ───────────────────────────────────────────────
            string meta = $"멤버 {_room.MemberIds.Count}명  ·  {_room.LastActivityAt:yyyy.MM.dd} 생성";
            Add(Lbl(meta, "맑은 고딕", 8.5f, FontStyle.Regular, Meta, new Rectangle(pad, y, w - pad * 2, 22), ContentAlignment.MiddleCenter));
            y += 22 + 14;

            // ── 4. Chips ──────────────────────────────────────────────
            var chipList = new List<string> { _room.IsDirectMessage ? "1:1" : "그룹" };
            if (_room.IsEphemeral && !string.IsNullOrEmpty(_room.EphemeralDisplay))
                chipList.Add(_room.EphemeralDisplay);
            chipList.Add("방 공유키");

            // Measure total chip width to center the row
            int chipH = 26, chipGap = 6;
            var chipSizes = chipList.Select(t => MeasureChipWidth(t)).ToList();
            int totalChipW = chipSizes.Sum() + chipGap * (chipList.Count - 1);
            int chipStartX = Math.Max(pad, (w - totalChipW) / 2);
            int cx = chipStartX;
            foreach (var (chipText, chipW) in chipList.Zip(chipSizes))
            {
                Add(MakeChip(chipText, new Rectangle(cx, y, chipW, chipH)));
                cx += chipW + chipGap;
            }
            y += chipH + 16;

            // ── 5. Action buttons ─────────────────────────────────────
            const int btnW = 80, btnH = 34, btnGap = 8;
            var actions = new List<(string label, EventHandler? handler)>
            {
                ("알림",  null),
                ("검색",  null),
            };
            if (!_room.IsDirectMessage)
                actions.Add(("초대", (_, _) => InviteRequested?.Invoke(this, EventArgs.Empty)));

            int totalBtnW = btnW * actions.Count + btnGap * (actions.Count - 1);
            int btnStartX = (w - totalBtnW) / 2;
            int bx = btnStartX;
            foreach (var (label, handler) in actions)
            {
                var btn = MakeActionButton(label, new Rectangle(bx, y, btnW, btnH));
                if (handler != null) btn.Click += handler;
                Add(btn);
                bx += btnW + btnGap;
            }
            y += btnH + 16;

            // ── 6. Divider ────────────────────────────────────────────
            y = Divider(y, w);

            // ── 7. Members header ─────────────────────────────────────
            Add(Lbl($"멤버 {_room.MemberIds.Count}", "맑은 고딕", 9f, FontStyle.Bold, Body,
                new Rectangle(16, y, 100, 36), ContentAlignment.MiddleLeft));
            var lblViewAll = Lbl("모두 보기", "맑은 고딕", 9f, FontStyle.Regular, Meta,
                new Rectangle(w - 100, y, 84, 36), ContentAlignment.MiddleRight);
            lblViewAll.Cursor = Cursors.Hand;
            Add(lblViewAll);
            y += 36;

            // ── 8. Member items ───────────────────────────────────────
            string? myId = _authService.CurrentUser?.UserId;
            var members = _room.Members.Count > 0
                ? _room.Members
                : _room.MemberIds.Select(id => new Models.RoomMember { UserId = id, DisplayName = id[..Math.Min(8, id.Length)] }).ToList();
            int cnt = Math.Min(members.Count, 5);
            for (int i = 0; i < cnt; i++)
            {
                var m = members[i];
                Add(MakeMemberItem(m, m.UserId == myId, w, y));
                y += 60;
            }
            y += 8;

            // ── 9. Divider ────────────────────────────────────────────
            y = Divider(y, w);

            // ── 10. Delete label ──────────────────────────────────────
            var lblDel = Lbl("대화 내용 모두 삭제", "맑은 고딕", 9f, FontStyle.Regular, Meta,
                new Rectangle(0, y, w, 36), ContentAlignment.MiddleCenter);
            lblDel.Cursor = Cursors.Hand;
            Add(lblDel);
            y += 36 + 8;

            // ── 11. Leave button ──────────────────────────────────────
            const int lm = 24;
            var btnLeave = new Button
            {
                Text      = "방 나가기",
                Font      = new Font("맑은 고딕", 10f, FontStyle.Bold),
                BackColor = DangerBg,
                ForeColor = DangerText,
                FlatStyle = FlatStyle.Flat,
                Location  = new Point(lm, y),
                Size      = new Size(w - lm * 2, 48),
                Cursor    = Cursors.Hand,
            };
            btnLeave.FlatAppearance.BorderSize         = 0;
            btnLeave.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 220, 220);
            btnLeave.Click += OnLeaveClicked;
            Add(btnLeave);
            y += 48 + 24;

            pnlContent.Size = new Size(w, y);
        }

        // ── Convenience wrappers ──────────────────────────────────────

        private void Add(Control c) => pnlContent.Controls.Add(c);

        private int Divider(int y, int w)
        {
            Add(new Panel { Location = new Point(0, y), Size = new Size(w, 1), BackColor = Hairline });
            return y + 1 + 12;
        }

        private static Label Lbl(string text, string fnt, float sz, FontStyle style, Color fore,
            Rectangle bounds, ContentAlignment align) => new Label
        {
            Text      = text,
            Font      = new Font(fnt, sz, style),
            ForeColor = fore,
            AutoSize  = false,
            TextAlign = align,
            Location  = bounds.Location,
            Size      = bounds.Size,
            BackColor = Color.Transparent,
        };

        private static int MeasureChipWidth(string text)
        {
            using var g   = Graphics.FromHwnd(IntPtr.Zero);
            using var fnt = new Font("맑은 고딕", 9f);
            return (int)g.MeasureString(text, fnt).Width + 22;
        }

        private static Panel MakeChip(string text, Rectangle bounds)
        {
            string t = text;
            var chip = new Panel { Location = bounds.Location, Size = bounds.Size, BackColor = Color.Transparent };
            chip.Paint += (_, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                var r = new RectangleF(0.5f, 0.5f, chip.Width - 1f, chip.Height - 1f);
                using var path = RoundedRect(r, 6f);
                g.FillPath(new SolidBrush(Surface), path);
                g.DrawPath(new Pen(Border), path);
                using var fnt = new Font("맑은 고딕", 9f);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(t, fnt, new SolidBrush(Meta), new RectangleF(0, 0, chip.Width, chip.Height), sf);
            };
            return chip;
        }

        private static Button MakeActionButton(string text, Rectangle bounds)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = new Font("맑은 고딕", 9f),
                ForeColor = Body,
                BackColor = Canvas,
                Location  = bounds.Location,
                Size      = bounds.Size,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
            };
            btn.FlatAppearance.BorderColor        = Border;
            btn.FlatAppearance.BorderSize         = 1;
            btn.FlatAppearance.MouseOverBackColor = Surface;
            return btn;
        }

        private Panel MakeMemberItem(Models.RoomMember member, bool isSelf, int w, int y)
        {
            var panel = new Panel { Location = new Point(0, y), Size = new Size(w, 60), BackColor = Canvas };

            string ini = string.IsNullOrEmpty(member.DisplayName) ? "?" : member.DisplayName[0].ToString().ToUpper();
            var pnlAv = new Panel { Location = new Point(16, 12), Size = new Size(36, 36), BackColor = Color.Transparent };
            pnlAv.Paint += (_, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                using var path = RoundedRect(new RectangleF(0, 0, 36, 36), 6f);
                g.FillPath(new SolidBrush(GetAvatarColor(member.UserId)), path);
                using var fnt = new Font("맑은 고딕", 12f, FontStyle.Bold);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(ini, fnt, Brushes.White, new RectangleF(0, 0, 36, 36), sf);
            };
            panel.Controls.Add(pnlAv);

            string nameText = member.DisplayName + (isSelf ? " (나)" : "");
            var lblName = new Label { Text = nameText, Font = new Font("맑은 고딕", 9.5f, FontStyle.Bold), ForeColor = Body, AutoSize = true, Location = new Point(62, 12) };
            panel.Controls.Add(lblName);
            bool isOnline = member.LastSeenAt > DateTime.MinValue
                && (DateTime.UtcNow - member.LastSeenAt).TotalMinutes < 5;
            string statusText = isOnline ? "온라인" : FormatLastSeen(member.LastSeenAt);
            var lblStatus = new Label
            {
                Text      = statusText,
                Font      = new Font("맑은 고딕", 8.5f),
                ForeColor = isOnline ? Color.FromArgb(46, 204, 113) : Meta,
                AutoSize  = true,
                Location  = new Point(62, 34)
            };
            panel.Controls.Add(lblStatus);

            if (member.IsAdmin)
            {
                var ownerChip = new Panel
                {
                    Location  = new Point(62 + lblName.PreferredWidth + 6, 14),
                    Size      = new Size(36, 18),
                    BackColor = Color.Transparent,
                };
                ownerChip.Paint += (_, e) =>
                {
                    var g = e.Graphics;
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using var path = RoundedRect(new RectangleF(0.5f, 0.5f, 35f, 17f), 5f);
                    g.FillPath(new SolidBrush(Surface), path);
                    g.DrawPath(new Pen(Border), path);
                    using var fnt = new Font("맑은 고딕", 8f);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("방장", fnt, new SolidBrush(Meta), new RectangleF(0, 0, 36, 18), sf);
                };
                panel.Controls.Add(ownerChip);
            }

            // 방장이고 본인이 아닌 멤버에게 위임/강퇴 버튼 표시
            string? myId = _authService.CurrentUser?.UserId;
            bool iAmAdmin = _room.Members.Any(m => m.UserId == myId && m.IsAdmin);
            if (!isSelf && iAmAdmin && !member.IsAdmin)
            {
                // 위임 버튼 (오른쪽에서 두 번째)
                var btnDelegate = new Button
                {
                    Text      = "위임",
                    Font      = new Font("맑은 고딕", 8.5f),
                    ForeColor = Color.FromArgb(52, 152, 219),
                    Size      = new Size(44, 26),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(235, 245, 255),
                    Cursor    = Cursors.Hand,
                    Location  = new Point(w - 106, 17),
                };
                btnDelegate.FlatAppearance.BorderSize         = 0;
                btnDelegate.FlatAppearance.MouseOverBackColor = Color.FromArgb(210, 235, 255);
                btnDelegate.Click += async (_, _) => await OnTransferAdminClickedAsync(member);
                panel.Controls.Add(btnDelegate);

                // 강퇴 버튼
                var btnKick = new Button
                {
                    Text      = "강퇴",
                    Font      = new Font("맑은 고딕", 8.5f),
                    ForeColor = DangerText,
                    Size      = new Size(44, 26),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = DangerBg,
                    Cursor    = Cursors.Hand,
                    Location  = new Point(w - 56, 17),
                };
                btnKick.FlatAppearance.BorderSize         = 0;
                btnKick.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 210, 210);
                btnKick.Click += async (_, _) => await OnKickClickedAsync(member);
                panel.Controls.Add(btnKick);
            }
            else if (!isSelf)
            {
                var btnMore = new Button
                {
                    Text      = "···",
                    Font      = new Font("맑은 고딕", 11f),
                    ForeColor = Meta,
                    Size      = new Size(30, 30),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Canvas,
                    Cursor    = Cursors.Hand,
                    Location  = new Point(w - 38, 15),
                };
                btnMore.FlatAppearance.BorderSize         = 0;
                btnMore.FlatAppearance.MouseOverBackColor = Surface;
                panel.Controls.Add(btnMore);
            }

            panel.Paint += (_, e) =>
            {
                using var pen = new Pen(Hairline);
                e.Graphics.DrawLine(pen, 16, panel.Height - 1, panel.Width - 16, panel.Height - 1);
            };
            return panel;
        }

        private Task OnTransferAdminClickedAsync(Models.RoomMember target)
        {
            var confirm = (DialogResult)Invoke(() =>
                MessageBox.Show(
                    $"{target.DisplayName} 님에게 방장 권한을 위임하시겠습니까?\n위임 후 본인은 일반 멤버가 됩니다.",
                    "방장 위임",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question));
            if (confirm == DialogResult.Yes)
                Invoke(() => TransferAdminRequested?.Invoke(this, target.UserId));
            return Task.CompletedTask;
        }

        private async Task OnKickClickedAsync(Models.RoomMember target)
        {
            // UI 스레드에서 확인 다이얼로그
            var confirm = (DialogResult)Invoke(() =>
                MessageBox.Show($"{target.DisplayName} 님을 강퇴하시겠습니까?", "강퇴",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning));
            if (confirm != DialogResult.Yes) return;

            var ok = await _roomService.KickMemberAsync(_room.RoomId, target.UserId);

            if (!ok)
            {
                Invoke(() => MessageBox.Show("강퇴에 실패했습니다.", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error));
                return;
            }

            // 서버에서 최신 멤버 목록 재조회 후 패널 갱신
            var updated = await _roomService.GetRoomAsync(_room.RoomId);
            if (updated is not null)
                Invoke(() => Refresh(updated));
        }

        private void OnLeaveClicked(object? sender, EventArgs e)
        {
            if (MessageBox.Show("정말로 방을 나가시겠습니까?", "방 나가기",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                LeaveRoomConfirmed?.Invoke(this, EventArgs.Empty);
        }

        private static string FormatLastSeen(DateTime lastSeenAt)
        {
            if (lastSeenAt == DateTime.MinValue) return "오프라인";
            var diff = DateTime.UtcNow - lastSeenAt;
            if (diff.TotalMinutes < 60)  return $"{(int)diff.TotalMinutes}분 전";
            if (diff.TotalHours   < 24)  return $"{(int)diff.TotalHours}시간 전";
            if (diff.TotalDays    < 7)   return $"{(int)diff.TotalDays}일 전";
            return lastSeenAt.ToLocalTime().ToString("yyyy.MM.dd");
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
    }
}
