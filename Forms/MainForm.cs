using System.Drawing.Drawing2D;
using System.Drawing.Text;
using Microsoft.Extensions.DependencyInjection;
using SecureChat.Controls;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Forms;

public partial class MainForm : Form
{
    // ── Musinsa design tokens ──────────────────────────────
    private static readonly Color Canvas   = Color.White;
    private static readonly Color Surface  = Color.FromArgb(245, 245, 245);  // #f5f5f5
    private static readonly Color Hairline = Color.FromArgb(238, 238, 238);  // #eeeeee
    private static readonly Color Meta     = Color.FromArgb(153, 153, 153);  // #999999

    private readonly IAuthService            _authService;
    private readonly IRoomService            _roomService;
    private readonly IChatService            _chatService;
    private readonly IMessageStore           _messageStore;
    private readonly IContactService         _contactService;
    private readonly IFriendRequestService   _friendRequestService;

    private readonly Dictionary<string, RoomListItemControl> _roomControls = new();
    private List<ChatRoom>   _allRooms          = [];
    private ChatPanel?            _activeChatPanel;
    private ContactsPanel?        _activeContactsPanel;
    private ContactProfilePanel?  _activeProfilePanel;

    private string _activeFilter  = "all";
    private string _avatarInitial = "?";
    private string _avatarSeed    = "";

    // Tab hit-rects populated in PnlFilterTabs_Paint
    private RectangleF _tabAllRect;
    private RectangleF _tabDmRect;
    private RectangleF _tabGroupRect;

    public MainForm(IAuthService authService, IRoomService roomService, IChatService chatService,
                    IMessageStore messageStore, IContactService contactService,
                    IFriendRequestService friendRequestService)
    {
        _authService          = authService;
        _roomService          = roomService;
        _chatService          = chatService;
        _messageStore         = messageStore;
        _contactService       = contactService;
        _friendRequestService = friendRequestService;
        InitializeComponent();

        ApplyRoundedRegion(btnNewRoom, 4);
        ApplyRoundedRegion(btnCreateRoom, 4);
        ApplyRoundedRegion(btnAddContactRight, 4);

        // 친구 요청 이벤트 구독
        _friendRequestService.RequestReceived  += OnRequestReceived;
        _friendRequestService.RequestResponded += OnRequestResponded;
    }

    // ─────────────────────────────────────────────────────────
    //  Form lifecycle
    // ─────────────────────────────────────────────────────────

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        var user = _authService.CurrentUser;
        if (user != null)
        {
            string display = string.IsNullOrEmpty(user.DisplayName) ? user.Username : user.DisplayName;
            lblDisplayName.Text = display;
            lblUsername.Text    = $"@{user.Username}";
            _avatarInitial = display.Length > 0 ? display[0].ToString() : "?";
            _avatarSeed    = user.Username;
            pnlAvatar.Invalidate();
        }

        lblFingerprint.Text = GenerateFingerprint();

        UpdateConnectionStatus(_chatService.State);
        _chatService.ConnectionStateChanged += OnConnectionStateChanged;
        _chatService.MessageReceived        += OnMessageReceived;
        _chatService.NewRoomReceived        += OnNewRoomReceived;
        _chatService.SessionInvalidated     += OnSessionInvalidated;
        _chatService.KickedFromRoom         += OnKickedFromRoom;
        _chatService.RoomMemberChanged      += OnRoomMemberChangedInList;
        _chatService.AdminTransferred       += OnRoomMemberChangedInList;

        flpRoomList.SizeChanged += FlpRoomList_SizeChanged;

        try
        {
            await LoadRoomsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"방 목록을 불러올 수 없습니다. 서버 연결을 확인하세요.\n{ex.Message}",
                "초기화 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        CenterEmptyState();
        RefreshRequestBadgeAsync();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _chatService.ConnectionStateChanged    -= OnConnectionStateChanged;
        _chatService.MessageReceived           -= OnMessageReceived;
        _chatService.NewRoomReceived           -= OnNewRoomReceived;
        _chatService.SessionInvalidated        -= OnSessionInvalidated;
        _chatService.KickedFromRoom            -= OnKickedFromRoom;
        _chatService.RoomMemberChanged         -= OnRoomMemberChangedInList;
        _chatService.AdminTransferred          -= OnRoomMemberChangedInList;
        _friendRequestService.RequestReceived  -= OnRequestReceived;
        _friendRequestService.RequestResponded -= OnRequestResponded;
        _activeChatPanel?.Dispose();
        Application.Exit();
    }

    // ─────────────────────────────────────────────────────────
    //  Room list
    // ─────────────────────────────────────────────────────────

    private async Task LoadRoomsAsync()
    {
        _allRooms = await _roomService.GetMyRoomsAsync();
        RenderRooms(FilteredRooms());
    }

    private IEnumerable<ChatRoom> FilteredRooms()
    {
        var source = _allRooms.AsEnumerable();
        source = _activeFilter switch
        {
            "dm"    => source.Where(r => r.IsDirectMessage),
            "group" => source.Where(r => !r.IsDirectMessage),
            _       => source
        };
        var q = txtSearch.Text.Trim();
        if (!string.IsNullOrEmpty(q))
            source = source.Where(r => r.Name.Contains(q, StringComparison.OrdinalIgnoreCase));
        return source.OrderByDescending(r => r.LastActivityAt);
    }

    private void RenderRooms(IEnumerable<ChatRoom> rooms)
    {
        flpRoomList.SuspendLayout();
        flpRoomList.Controls.Clear();
        _roomControls.Clear();

        foreach (var r in rooms)
        {
            // 인메모리 스토어에 복호화된 메시지가 있으면 미리보기로 사용
            var cached = _messageStore.GetMessagesForRoom(r.RoomId);
            if (cached.Count > 0)
            {
                var latest = cached.OrderByDescending(m => m.SentAt).First();
                r.LastMessageText   = latest.PlainText;
                r.LastMessageSender = latest.IsMine ? null : latest.SenderName;
                r.LastActivityAt    = latest.SentAt;
            }

            var item = new RoomListItemControl(r);
            item.Width   = Math.Max(1, flpRoomList.ClientSize.Width);
            item.Clicked += OnRoomClicked;
            flpRoomList.Controls.Add(item);
            _roomControls[r.RoomId] = item;
        }

        flpRoomList.ResumeLayout();
    }

    private void FlpRoomList_SizeChanged(object? sender, EventArgs e)
    {
        foreach (RoomListItemControl ctl in flpRoomList.Controls)
            ctl.Width = Math.Max(1, flpRoomList.ClientSize.Width);
    }

    private void OnRoomClicked(object? sender, ChatRoom room)
    {
        if (_activeChatPanel?.RoomId == room.RoomId) return;

        CloseContactsPanel();
        CloseChatPanel();

        pnlEmptyState.Visible = false;

        _activeChatPanel = new ChatPanel(room.RoomId, _chatService, _messageStore, _authService, _roomService, _contactService);
        _activeChatPanel.CloseRequested += async (_, _) =>
        {
            CloseChatPanel();
            await LoadRoomsAsync();
        };
        pnlRight.Controls.Add(_activeChatPanel);

        if (_roomControls.TryGetValue(room.RoomId, out var ctl))
            ctl.ClearUnread();
    }

    private void CloseChatPanel()
    {
        if (_activeChatPanel == null) return;
        pnlRight.Controls.Remove(_activeChatPanel);
        _activeChatPanel.Dispose();
        _activeChatPanel = null;
        pnlEmptyState.Visible = true;
        CenterEmptyState();
    }

    // ─────────────────────────────────────────────────────────
    //  Search & filter
    // ─────────────────────────────────────────────────────────

    private void txtSearch_TextChanged(object? sender, EventArgs e) => RenderRooms(FilteredRooms());

    private async void btnNewRoom_Click(object? sender, EventArgs e)
    {
        using var dialog = Program.Services.GetRequiredService<CreateRoomDialog>();
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (dialog.SelectedContacts.Count == 0) return;

        ChatRoom room;
        if (dialog.IsDirectMessage)
        {
            room = await _roomService.CreateDirectMessageAsync(dialog.SelectedContacts[0].UserId);
        }
        else
        {
            var memberIds = dialog.SelectedContacts.Select(c => c.UserId).ToList();
            var name = string.Join(", ", dialog.SelectedContacts.Select(c => c.DisplayName));
            room = await _roomService.CreateRoomAsync(name, memberIds);
        }

        await LoadRoomsAsync();

        // 생성된 방 바로 열기
        if (_roomControls.TryGetValue(room.RoomId, out var ctl))
            OnRoomClicked(ctl, room);
    }

    // ── 친구 요청 이벤트 핸들러 ─────────────────────────────────

    private void OnRequestReceived(object? sender, FriendRequest req)
    {
        // 배지 갱신 후 토스트 알림
        RefreshRequestBadgeAsync();
        ShowRequestToast(req.SenderName);
    }

    private void OnRequestResponded(object? sender, FriendRequest req)
    {
        string name = string.IsNullOrEmpty(req.ReceiverName) ? req.ReceiverId : req.ReceiverName;
        string msg  = req.Status == FriendRequestStatus.Accepted
            ? $"{name} 님이 친구 요청을 수락했습니다."
            : $"{name} 님이 친구 요청을 거절했습니다.";
        MessageBox.Show(msg, "친구 요청 결과", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private int  _notifCount;
    private bool _notifHover;

    private async void RefreshRequestBadgeAsync()
    {
        try
        {
            var pending = await _friendRequestService.GetIncomingRequestsAsync();
            int count   = pending.Count;

            if (!IsHandleCreated) return;
            Invoke(() =>
            {
                _notifCount = count;
                pnlNotifBtn.Invalidate();
            });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // 배지 갱신 실패는 조용히 무시 — UI 크래시 방지
        }
    }

    private void ShowRequestToast(string senderName)
    {
        MessageBox.Show($"{senderName} 님이 친구 요청을 보냈습니다.", "새 친구 요청",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void PnlNotifBtn_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        // 호버 배경
        if (_notifHover)
        {
            using var hb = new SolidBrush(Color.FromArgb(245, 245, 245));
            g.FillRectangle(hb, 3, 6, 28, 28);
        }

        // 벨 아이콘 — 40×40 패널 기준, 중심 (17,23)
        const int cx = 17, cy = 23;
        var ic = Color.FromArgb(100, 100, 100);
        using var ib = new SolidBrush(ic);

        using var bell = new GraphicsPath();
        bell.AddArc(cx - 8, cy - 13, 16, 14, 180, 180);
        bell.AddLine(cx + 8, cy - 6, cx + 10, cy + 4);
        bell.AddLine(cx + 10, cy + 4, cx - 10, cy + 4);
        bell.AddLine(cx - 10, cy + 4, cx - 8, cy - 6);
        bell.CloseFigure();
        g.FillPath(ib, bell);

        g.FillRectangle(ib, cx - 11, cy + 3, 22, 3);  // 하단 테두리
        g.FillEllipse(ib, cx - 3, cy + 6, 6, 5);       // 추
        using var hp = new Pen(ic, 1.5f);
        g.DrawArc(hp, cx - 3, cy - 16, 6, 6, 0, -180); // 손잡이

        // 배지 (요청 있을 때만)
        if (_notifCount > 0)
        {
            string txt = _notifCount > 9 ? "9+" : _notifCount.ToString();
            var br = new RectangleF(25, 1, 15, 15);
            g.FillEllipse(new SolidBrush(Color.FromArgb(231, 76, 60)), br);
            using var sf  = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var fnt = new Font("맑은 고딕", 6.5f, FontStyle.Bold);
            g.DrawString(txt, fnt, Brushes.White, br, sf);
        }
    }

    private void PnlNotifBtn_MouseEnter(object? sender, EventArgs e) { _notifHover = true;  pnlNotifBtn.Invalidate(); }
    private void PnlNotifBtn_MouseLeave(object? sender, EventArgs e) { _notifHover = false; pnlNotifBtn.Invalidate(); }

    private async void PnlNotifBtn_Click(object? sender, EventArgs e)
    {
        var pending = await _friendRequestService.GetIncomingRequestsAsync();
        if (pending.Count == 0)
        {
            MessageBox.Show("대기 중인 친구 요청이 없습니다.", "친구 요청", MessageBoxButtons.OK);
            return;
        }

        using var dlg = new FriendRequestDialog(pending, _friendRequestService);
        dlg.ShowDialog(this);
        RefreshRequestBadgeAsync();
    }

    private void btnAddContactRight_Click(object? sender, EventArgs e)
    {
        using var dlg = new AddFriendDialog(_contactService, _friendRequestService);
        dlg.ShowDialog(this);
    }

    private void btnAddContact_Click(object? sender, EventArgs e)
    {
        // 토글: 이미 열려 있으면 닫기
        if (_activeContactsPanel != null)
        {
            CloseContactsPanel();
            return;
        }

        CloseChatPanel();

        _activeContactsPanel = new ContactsPanel(_contactService, _authService, _friendRequestService);
        _activeContactsPanel.CloseRequested    += (_, _) => CloseContactsPanel();
        _activeContactsPanel.ProfileRequested  += (_, contact) => ShowContactProfile(contact);
        _activeContactsPanel.OpenChatRequested += async (_, contact) =>
        {
            CloseContactsPanel();
            var room = await _roomService.CreateDirectMessageAsync(contact.UserId);
            await LoadRoomsAsync();
            if (_roomControls.TryGetValue(room.RoomId, out var ctl))
                OnRoomClicked(ctl, room);
        };
        pnlRight.Controls.Add(_activeContactsPanel);
        pnlEmptyState.Visible = false;
    }

    private void CloseContactsPanel()
    {
        if (_activeContactsPanel == null) return;
        pnlRight.Controls.Remove(_activeContactsPanel);
        _activeContactsPanel.Dispose();
        _activeContactsPanel = null;
        pnlEmptyState.Visible = true;
        CenterEmptyState();
    }

    private void ShowContactProfile(Contact contact)
    {
        CloseProfilePanel();
        pnlEmptyState.Visible = false;

        // contacts panel을 숨겨서 z-order 충돌 방지
        if (_activeContactsPanel != null)
            _activeContactsPanel.Visible = false;

        _activeProfilePanel = new ContactProfilePanel(contact);
        _activeProfilePanel.CloseRequested += (_, _) => CloseProfilePanel();
        _activeProfilePanel.OpenChatRequested += async (_, c) =>
        {
            CloseProfilePanel();
            CloseContactsPanel();
            var room = await _roomService.CreateDirectMessageAsync(c.UserId);
            await LoadRoomsAsync();
            if (_roomControls.TryGetValue(room.RoomId, out var ctl))
                OnRoomClicked(ctl, room);
        };
        pnlRight.Controls.Add(_activeProfilePanel);
        _activeProfilePanel.BringToFront();
    }

    private void CloseProfilePanel()
    {
        if (_activeProfilePanel == null) return;
        pnlRight.Controls.Remove(_activeProfilePanel);
        _activeProfilePanel.Dispose();
        _activeProfilePanel = null;

        // contacts panel 복원
        if (_activeContactsPanel != null)
        {
            _activeContactsPanel.Visible = true;
            return;
        }

        if (_activeChatPanel == null)
        {
            pnlEmptyState.Visible = true;
            CenterEmptyState();
        }
    }

    private void btnSettings_Click(object? sender, EventArgs e)
    {
        var settings = Program.Services.GetRequiredService<SettingsForm>();
        settings.Show(this);
    }

    // ─────────────────────────────────────────────────────────
    //  Filter tab — selected chip: black bg / white text
    // ─────────────────────────────────────────────────────────

    private void PnlFilterTabs_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode     = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.Clear(Canvas);

        string[] labels  = ["전체", "1:1", "그룹"];
        string[] filters = ["all", "dm", "group"];
        float[]  tabW    = [56f, 46f, 52f];

        // Container pill — #f5f5f5 bg + #eeeeee border
        float totalW = tabW.Sum() + 8f;
        var container = new RectangleF(10, 10, totalW, 36);
        using var bgPath = RoundedRect(container, 6f);
        g.FillPath(new SolidBrush(Surface), bgPath);
        using var containerPen = new Pen(Hairline, 1f);
        g.DrawPath(containerPen, bgPath);

        RectangleF[] rects = new RectangleF[3];
        float tx = 14f;
        for (int i = 0; i < labels.Length; i++)
        {
            rects[i] = new RectangleF(tx, 14f, tabW[i], 28f);

            bool active = _activeFilter == filters[i];
            if (active)
            {
                // White selected chip with black border — "lifted" appearance
                using var selPath = RoundedRect(rects[i], 4f);
                g.FillPath(Brushes.White, selPath);
                using var selPen = new Pen(Color.Black, 1.5f);
                g.DrawPath(selPen, selPath);
            }

            // Active = black bold, inactive = #333 regular (clearly readable)
            var textColor = active ? Color.Black : Color.FromArgb(51, 51, 51);
            using var tf = new Font("맑은 고딕", 9.5F,
                active ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
            var sf = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            g.DrawString(labels[i], tf, new SolidBrush(textColor), rects[i], sf);

            tx += tabW[i];
        }

        _tabAllRect   = rects[0];
        _tabDmRect    = rects[1];
        _tabGroupRect = rects[2];
    }

    private void PnlFilterTabs_MouseClick(object? sender, MouseEventArgs e)
    {
        string prev = _activeFilter;
        if (_tabAllRect.Contains(e.Location))        _activeFilter = "all";
        else if (_tabDmRect.Contains(e.Location))    _activeFilter = "dm";
        else if (_tabGroupRect.Contains(e.Location)) _activeFilter = "group";

        if (_activeFilter != prev)
        {
            pnlFilterTabs.Invalidate();
            RenderRooms(FilteredRooms());
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Search bar — #f5f5f5 fill, 4px radius
    // ─────────────────────────────────────────────────────────

    private void PnlSearch_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Canvas);

        var boxRect = new RectangleF(10, 8, pnlSearch.Width - 20, 40);
        using var boxPath = RoundedRect(boxRect, 4f);
        g.FillPath(new SolidBrush(Surface), boxPath);

        using var iconFont = new Font("맑은 고딕", 10F, FontStyle.Regular, GraphicsUnit.Point);
        g.DrawString("Q", iconFont, new SolidBrush(Meta), new PointF(18, 16));
    }

    // ─────────────────────────────────────────────────────────
    //  Header — white canvas, hairline #eeeeee bottom border
    // ─────────────────────────────────────────────────────────

    private void PnlHeader_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new Pen(Hairline);
        e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
    }

    private void PnlAvatar_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode    = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using var avPath = RoundedRect(new RectangleF(0, 0, 46, 46), 11f);
        g.FillPath(new SolidBrush(GetAvatarColor(_avatarSeed)), avPath);

        using var font = new Font("맑은 고딕", 16F, FontStyle.Bold, GraphicsUnit.Point);
        var sz = g.MeasureString(_avatarInitial, font);
        g.DrawString(_avatarInitial, font, Brushes.White,
            (46 - sz.Width) / 2, (46 - sz.Height) / 2);
    }

    // ─────────────────────────────────────────────────────────
    //  Empty state — shield icon, tip card
    // ─────────────────────────────────────────────────────────

    private void PnlShield_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var bgPath = RoundedRect(new RectangleF(0, 0, 96, 96), 4f);
        g.FillPath(new SolidBrush(Surface), bgPath);
        using var borderPen = new Pen(Hairline);
        g.DrawPath(borderPen, bgPath);

        using var iconFont = new Font("맑은 고딕", 34F, FontStyle.Bold, GraphicsUnit.Point);
        const string icon = "S";
        var sz = g.MeasureString(icon, iconFont);
        g.DrawString(icon, iconFont, Brushes.Black,
            (96 - sz.Width) / 2, (96 - sz.Height) / 2);
    }

    private void PnlTip_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var bgPath = RoundedRect(new RectangleF(0, 0, pnlTip.Width, pnlTip.Height), 4f);
        g.FillPath(new SolidBrush(Surface), bgPath);
        using var borderPen = new Pen(Hairline);
        g.DrawPath(borderPen, bgPath);
    }

    private void PnlRight_Resize(object? sender, EventArgs e) => CenterEmptyState();

    private void CenterEmptyState()
    {
        int x = (pnlRight.ClientSize.Width  - pnlEmptyState.Width)  / 2;
        int y = (pnlRight.ClientSize.Height - pnlEmptyState.Height) / 2;
        pnlEmptyState.Location = new Point(Math.Max(0, x), Math.Max(0, y));
    }

    // ─────────────────────────────────────────────────────────
    //  Connection status
    // ─────────────────────────────────────────────────────────

    private void OnConnectionStateChanged(object? sender, ConnectionState state)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke(new Action(() => UpdateConnectionStatus(state)));
        else UpdateConnectionStatus(state);
    }

    private void UpdateConnectionStatus(ConnectionState state)
    {
        switch (state)
        {
            case ConnectionState.Connected:
                lblConnectionStatus.Text      = "● 서버 연결됨";
                lblConnectionStatus.ForeColor = Color.Black;
                break;
            case ConnectionState.Connecting:
            case ConnectionState.Reconnecting:
                lblConnectionStatus.Text      = state == ConnectionState.Connecting ? "● 연결 중..." : "● 재연결 중...";
                lblConnectionStatus.ForeColor = Meta;
                break;
            default:
                lblConnectionStatus.Text      = "● 연결 끊김";
                lblConnectionStatus.ForeColor = Meta;
                break;
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Message received
    // ─────────────────────────────────────────────────────────

    private void OnMessageReceived(object? sender, ChatMessage message)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke(new Action(() => HandleMessageReceived(message)));
        else HandleMessageReceived(message);
    }

    private void HandleMessageReceived(ChatMessage message)
    {
        if (_roomControls.TryGetValue(message.RoomId, out var ctl))
        {
            ctl.UpdatePreview(message);
            if (_activeChatPanel?.RoomId != message.RoomId)
                ctl.IncrementUnread();
        }
        else
        {
            // 아직 목록에 없는 방의 메시지 → 방 목록 갱신
            _ = LoadRoomsAsync();
        }
    }

    private void OnNewRoomReceived(object? sender, string roomId)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke(async () => await LoadRoomsAsync());
        else _ = LoadRoomsAsync();
    }

    private void OnKickedFromRoom(object? sender, string roomId)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) { BeginInvoke(() => OnKickedFromRoom(sender, roomId)); return; }

        // 현재 열린 방이 강퇴된 방이면 닫기
        if (_activeChatPanel?.RoomId == roomId)
            CloseChatPanel();

        _ = LoadRoomsAsync();
        MessageBox.Show("방에서 강퇴되었습니다.", "강퇴", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void OnRoomMemberChangedInList(object? sender, string roomId)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) { BeginInvoke(() => OnRoomMemberChangedInList(sender, roomId)); return; }

        // 방 목록 아이템의 멤버 수를 실시간으로 갱신 (서버 재조회)
        _ = RefreshRoomListItemAsync(roomId);
    }

    private async Task RefreshRoomListItemAsync(string roomId)
    {
        var updated = await _roomService.GetRoomAsync(roomId);
        if (updated is null || IsDisposed || !IsHandleCreated) return;

        // _allRooms 캐시 갱신
        var idx = _allRooms.FindIndex(r => r.RoomId == roomId);
        if (idx >= 0) _allRooms[idx] = updated;

        Invoke(() =>
        {
            if (_roomControls.TryGetValue(roomId, out var ctl))
                ctl.UpdateRoom(updated);
        });
    }

    private void OnSessionInvalidated(object? sender, string reason)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) { BeginInvoke(() => OnSessionInvalidated(sender, reason)); return; }

        _authService.LogoutAsync();

        MessageBox.Show(reason, "세션 만료", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        var loginForm = Program.Services.GetRequiredService<LoginForm>();
        loginForm.Show();
        Close();
    }

    // ─────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────

    private static string GenerateFingerprint()
    {
        var bytes = new byte[4];
        Random.Shared.NextBytes(bytes);
        string hex = BitConverter.ToString(bytes).Replace("-", "");
        return $"{hex[..4]}·{hex[4..]}";
    }

    private static void ApplyRoundedRegion(Control ctrl, int radius)
    {
        void Update()
        {
            var path = new GraphicsPath();
            path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
            path.AddArc(ctrl.Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
            path.AddArc(ctrl.Width - radius * 2, ctrl.Height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(0, ctrl.Height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            ctrl.Region = new Region(path);
        }
        ctrl.Resize += (_, _) => Update();
        if (ctrl.Width > 0 && ctrl.Height > 0) Update();
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
