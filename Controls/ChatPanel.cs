using System.Drawing.Drawing2D;
using System.Drawing.Text;
using SecureChat.Forms;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Controls;

/// <summary>
/// Embedded chat view shown in the MainForm right panel.
/// </summary>
public class ChatPanel : UserControl
{
    // ── Musinsa design tokens ──────────────────────────────
    private static readonly Color Canvas   = Color.White;
    private static readonly Color Surface  = Color.FromArgb(245, 245, 245);  // #f5f5f5
    private static readonly Color Hairline = Color.FromArgb(238, 238, 238);  // #eeeeee
    private static readonly Color Body     = Color.FromArgb(34,  34,  34);   // #222222
    private static readonly Color Meta     = Color.FromArgb(153, 153, 153);  // #999999

    private static readonly int[] TtlValues = { 0, 5, 30, 60, 300, 3600 };

    private readonly string           _roomId;
    private readonly IChatService     _chatService;
    private readonly IMessageStore    _messageStore;
    private readonly IAuthService     _authService;
    private readonly IRoomService     _roomService;
    private readonly IContactService  _contactService;
    private readonly Dictionary<string, MessageBubbleControl> _bubbles = [];

    private ChatRoom?       _room;
    private RoomInfoPanel?  _roomInfoPanel;
    private EventHandler?              _roomInfoResizer;
    private EventHandler?              _roomInfoCloseHandler;
    private EventHandler?              _roomInfoInviteHandler;
    private EventHandler?              _roomInfoLeaveHandler;
    private EventHandler<string>?      _roomInfoTransferHandler;

    // ── Controls ────────────────────────────────────────────
    private Panel          pnlHeader     = null!;
    private Label          lblRoomName   = null!;
    private Button         btnRoomSettings = null!;
    private Button         btnClose      = null!;
    private FlowLayoutPanel flpMessages  = null!;
    private Panel          pnlInfoBar    = null!;
    private Label          lblTtlLabel   = null!;
    private ComboBox       cmbTtl        = null!;
    private Label          lblHmac       = null!;
    private Label          lblCrypto     = null!;
    private Panel          pnlInput      = null!;
    private TextBox        txtInput      = null!;
    private Button         btnSend       = null!;

    public string RoomId { get; }
    public event EventHandler? CloseRequested;

    public ChatPanel(
        string roomId,
        IChatService chatService,
        IMessageStore messageStore,
        IAuthService authService,
        IRoomService roomService,
        IContactService contactService)
    {
        RoomId          = roomId;
        _roomId         = roomId;
        _chatService    = chatService;
        _messageStore   = messageStore;
        _authService    = authService;
        _roomService    = roomService;
        _contactService = contactService;

        Dock           = DockStyle.Fill;
        BackColor      = Canvas;
        DoubleBuffered = true;

        BuildUI();
        WireEvents();
        _ = LoadAsync();
    }

    // ─────────────────────────────────────────────────────────
    //  UI construction
    // ─────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // ── Header ───────────────────────────────────────────
        pnlHeader = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 54,
            BackColor = Canvas
        };
        pnlHeader.Paint += PnlHeader_Paint;

        lblRoomName = new Label
        {
            Text      = "방 이름",
            Font      = new Font("맑은 고딕", 11F, FontStyle.Bold),
            ForeColor = Color.Black,
            BackColor = Color.Transparent,
            Location  = new Point(16, 16),
            AutoSize  = true
        };

        btnRoomSettings = MakeIconButton("≡", 14F, Color.FromArgb(51, 51, 51), Canvas);
        btnRoomSettings.Visible = false;   // LoadAsync 후 그룹방에만 표시
        btnClose        = MakeIconButton("✕", 11F, Meta, Canvas);
        btnClose.Click        += (_, _) => CloseRequested?.Invoke(this, EventArgs.Empty);
        btnRoomSettings.Click += (_, _) => _ = ToggleRoomInfoAsync();

        pnlHeader.Controls.AddRange([lblRoomName, btnRoomSettings, btnClose]);
        pnlHeader.Resize += (_, _) => PositionHeaderButtons();
        PositionHeaderButtons();

        // ── Message list ─────────────────────────────────────
        flpMessages = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents  = false,
            AutoScroll    = false,
            AutoSize      = false,
            BackColor     = Canvas,
            Padding       = new Padding(0, 8, 0, 8)
        };
        // 세로 스크롤만 허용, 가로 스크롤바는 완전히 숨김
        flpMessages.HorizontalScroll.Maximum = 0;
        flpMessages.HorizontalScroll.Enabled = false;
        flpMessages.HorizontalScroll.Visible = false;
        flpMessages.VerticalScroll.Enabled   = true;
        flpMessages.AutoScroll               = true;

        flpMessages.SizeChanged += (_, _) =>
        {
            int w = Math.Max(1, flpMessages.ClientSize.Width);
            foreach (MessageBubbleControl b in flpMessages.Controls)
            {
                b.Width        = w;
                b.MinimumSize  = new Size(w, 0);
                b.MaximumSize  = new Size(w, 0);
            }
        };

        // ── Info bar (TTL + 암호화 정보) ────────────────────
        pnlInfoBar = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 40,
            BackColor = Surface
        };
        pnlInfoBar.Paint += PnlInfoBar_Paint;

        lblTtlLabel = new Label
        {
            Text      = "TTL:",
            Font      = new Font("맑은 고딕", 8.5F),
            ForeColor = Meta,
            BackColor = Color.Transparent,
            Location  = new Point(12, 10),
            AutoSize  = true
        };

        cmbTtl = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location      = new Point(44, 8),
            Size          = new Size(72, 22),
            Font          = new Font("맑은 고딕", 8.5F),
            BackColor     = Canvas,
            ForeColor     = Body,
            FlatStyle     = FlatStyle.Flat
        };
        cmbTtl.Items.AddRange(["끄기", "5초", "30초", "1분", "5분", "1시간"]);
        cmbTtl.SelectedIndex = 0;

        lblHmac = new Label
        {
            Text      = "HMAC-SHA256",
            Font      = new Font("맑은 고딕", 8.5F),
            ForeColor = Meta,
            BackColor = Color.Transparent,
            AutoSize  = true
        };

        lblCrypto = new Label
        {
            Text      = "AES-256 · RSA-OAEP",
            Font      = new Font("맑은 고딕", 8F),
            ForeColor = Meta,
            BackColor = Color.Transparent,
            TextAlign = ContentAlignment.MiddleRight,
            AutoSize  = true
        };

        pnlInfoBar.Resize += (_, _) => PositionInfoBarControls();
        pnlInfoBar.Controls.AddRange([lblTtlLabel, cmbTtl, lblHmac, lblCrypto]);

        // ── Input area ───────────────────────────────────────
        pnlInput = new Panel
        {
            Dock      = DockStyle.Bottom,
            Height    = 72,
            BackColor = Canvas
        };
        pnlInput.Paint  += PnlInput_Paint;
        pnlInput.Resize += (_, _) => PositionInputControls();

        txtInput = new TextBox
        {
            Multiline       = true,
            AcceptsReturn   = false,
            BorderStyle     = BorderStyle.None,
            Font            = new Font("맑은 고딕", 10F),
            BackColor       = Surface,
            ForeColor       = Body,
            PlaceholderText = "메시지 입력..."
        };
        txtInput.KeyDown += txtInput_KeyDown;

        // Primary CTA — black bg, white text, 4px radius
        btnSend = new Button
        {
            Text      = "전송",
            Font      = new Font("맑은 고딕", 9F, FontStyle.Bold),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Black,
            ForeColor = Color.White,
            Cursor    = Cursors.Hand
        };
        btnSend.FlatAppearance.BorderSize = 0;
        btnSend.FlatAppearance.MouseOverBackColor = Color.FromArgb(34, 34, 34);
        btnSend.Click += async (_, _) => await SendMessageAsync();

        pnlInput.Controls.AddRange([txtInput, btnSend]);
        PositionInputControls();

        // ── Add to panel ─────────────────────────────────────
        Controls.Add(flpMessages);
        Controls.Add(pnlInput);
        Controls.Add(pnlInfoBar);
        Controls.Add(pnlHeader);
    }

    protected override void OnLayout(LayoutEventArgs e)
    {
        base.OnLayout(e);
        if (pnlHeader  != null) PositionHeaderButtons();
        if (pnlInput   != null) PositionInputControls();
        if (pnlInfoBar != null) PositionInfoBarControls();
    }

    private void PositionInfoBarControls()
    {
        if (pnlInfoBar.Width <= 0) return;
        int cy = (pnlInfoBar.Height - cmbTtl.Height) / 2;

        lblTtlLabel.Location = new Point(12, (pnlInfoBar.Height - lblTtlLabel.Height) / 2);
        cmbTtl.Location      = new Point(lblTtlLabel.Right + 4, cy);

        lblCrypto.Location = new Point(pnlInfoBar.Width - lblCrypto.Width - 12,
                                       (pnlInfoBar.Height - lblCrypto.Height) / 2);

        int mid = (cmbTtl.Right + lblCrypto.Left) / 2;
        lblHmac.Location = new Point(mid - lblHmac.Width / 2,
                                     (pnlInfoBar.Height - lblHmac.Height) / 2);
    }

    private void PositionHeaderButtons()
    {
        btnClose.Size        = new Size(32, 32);
        btnClose.Location    = new Point(pnlHeader.Width - 36, 11);
        btnRoomSettings.Size = new Size(32, 32);
        btnRoomSettings.Location = new Point(pnlHeader.Width - 72, 11);
    }

    private void PositionInputControls()
    {
        if (pnlInput.Width <= 0) return;

        // Send button — right-anchored, 4px radius
        int sendW = 56, sendH = 36;
        int sendX = pnlInput.Width - 12 - sendW;
        int sendY = (pnlInput.Height - sendH) / 2;
        btnSend.Bounds = new Rectangle(sendX, sendY, sendW, sendH);
        ApplyRoundedRegion(btnSend, 4);

        // Input field — fills remaining space
        int inputX = 12;
        int inputW = sendX - inputX - 8;
        int inputY = (pnlInput.Height - 38) / 2;
        txtInput.Bounds = new Rectangle(inputX, inputY, inputW, 38);
    }

    // ─────────────────────────────────────────────────────────
    //  Paint events
    // ─────────────────────────────────────────────────────────

    private void PnlHeader_Paint(object? sender, PaintEventArgs e)
    {
        // Hairline #eeeeee bottom border
        using var pen = new Pen(Hairline);
        e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
    }

    private void PnlInfoBar_Paint(object? sender, PaintEventArgs e)
    {
        // Hairline #eeeeee top border
        using var pen = new Pen(Hairline);
        e.Graphics.DrawLine(pen, 0, 0, pnlInfoBar.Width, 0);
    }

    private void PnlInput_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Canvas);

        // #f5f5f5 input box, 4px radius
        var box = new RectangleF(10, 8, pnlInput.Width - 20 - 68, pnlInput.Height - 16);
        using var path = RoundedRect(box, 4f);
        g.FillPath(new SolidBrush(Surface), path);
    }

    // ─────────────────────────────────────────────────────────
    //  Load & event wiring
    // ─────────────────────────────────────────────────────────

    private void WireEvents()
    {
        _chatService.MessageReceived    += OnMessageReceived;
        _chatService.RoomMemberChanged  += OnRoomMemberChanged;
        _chatService.AdminTransferred   += OnRoomMemberChanged;
        _messageStore.MessageRemoved    += OnMessageRemoved;

        if (_chatService is SignalRChatService sr)
        {
            sr.MessageStatusUpdated  += OnMessageStatusUpdated;
            sr.ReadReceiptReceived   += OnReadReceiptReceived;
            sr.OwnMessageConfirmed   += OnOwnMessageConfirmed;
        }
    }

    private async Task LoadAsync()
    {
        await _roomService.JoinRoomAsync(_roomId).ConfigureAwait(false);
        var room = await _roomService.GetRoomAsync(_roomId).ConfigureAwait(false);

        // D13: 서버에서 최근 메시지 히스토리 로딩 (방 진입 시 1회)
        if (_chatService is SignalRChatService sr)
            await sr.LoadHistoryAsync(Guid.Parse(_roomId), limit: 50).ConfigureAwait(false);

        if (IsDisposed || !IsHandleCreated) return;

        Invoke(() =>
        {
            if (IsDisposed) return;

            if (room != null)
            {
                _room = room;
                var name = room.IsDirectMessage ? room.Name : $"{room.Name} ({room.MemberIds.Count}명)";
                lblRoomName.Text        = name;
                btnRoomSettings.Visible = !room.IsDirectMessage;
            }

            foreach (var m in _messageStore.GetMessagesForRoom(_roomId)
                                           .OrderBy(m => m.StoreSeq))
                AddMessageBubble(m);

            ScrollToBottom();
            txtInput.Focus();

            // D10: 진입 시 가장 마지막 메시지를 읽음으로 표시
            MarkLatestAsRead();
        });
    }

    private void OnReadReceiptReceived(object? sender, ReadReceiptUpdate update)
    {
        if (update.RoomId != _roomId || IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) { BeginInvoke(() => OnReadReceiptReceived(sender, update)); return; }

        // lastReadMessageId 이하(시간 기준)의 내 메시지를 모두 Read로 업데이트
        if (!Guid.TryParse(update.LastReadMessageId, out _)) return;

        var readAt = update.ReadAt;
        foreach (var bubble in _bubbles.Values)
        {
            if (bubble.Message.IsMine
                && bubble.Message.SentAt <= readAt
                && bubble.Message.Status != MessageStatus.Read)
            {
                bubble.UpdateStatus(MessageStatus.Read);
            }
        }
    }

    private void OnOwnMessageConfirmed(object? sender, (string LocalId, ChatMessage ServerMessage) e)
    {
        if (e.ServerMessage.RoomId != _roomId || IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) { BeginInvoke(() => OnOwnMessageConfirmed(sender, e)); return; }

        // 버블 딕셔너리: 로컬 UUID 키 → 서버 UUID 키로 교체
        if (_bubbles.TryGetValue(e.LocalId, out var bubble))
        {
            _bubbles.Remove(e.LocalId);
            _bubbles[e.ServerMessage.MessageId] = bubble;
            // Message 객체의 ID도 교체 (MarkAsRead 등이 올바른 ID를 참조하도록)
            bubble.Message.MessageId = e.ServerMessage.MessageId;
        }
    }

    private void OnMessageStatusUpdated(object? sender, MessageStatusUpdate update)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke(() => UpdateMessageStatus(update.MessageId, update.Status));
        else UpdateMessageStatus(update.MessageId, update.Status);
    }

    // D10: 채팅창 활성 시 상대방 메시지 중 가장 최신 것을 읽음으로 표시.
    // 로컬에서 생성한 내 메시지 UUID는 서버 DB에 없으므로 반드시 IsMine=false인 메시지만 사용.
    private void MarkLatestAsRead()
    {
        if (_chatService is not SignalRChatService sr) return;
        if (!Guid.TryParse(_roomId, out var roomGuid)) return;

        var msgs = _messageStore.GetMessagesForRoom(_roomId);
        // 상대방 메시지만 — 서버가 발행한 ID이므로 FK 오류 없음
        var last = msgs
            .Where(m => !m.IsMine && Guid.TryParse(m.MessageId, out _))
            .OrderBy(m => m.SentAt)
            .LastOrDefault();
        if (last is null) return;

        _ = sr.MarkAsReadAsync(roomGuid, Guid.Parse(last.MessageId));
    }

    private void OnMessageReceived(object? sender, ChatMessage message)
    {
        if (message.RoomId != _roomId || IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke(() => { AddMessageBubble(message); ScrollToBottom(); MarkLatestAsRead(); });
        else { AddMessageBubble(message); ScrollToBottom(); MarkLatestAsRead(); }
    }

    private void OnRoomMemberChanged(object? sender, string changedRoomId)
    {
        // 현재 방이 아니면 무시
        if (changedRoomId != _roomId || IsDisposed || !IsHandleCreated) return;

        if (InvokeRequired) { BeginInvoke(() => OnRoomMemberChanged(sender, changedRoomId)); return; }

        // RoomInfoPanel이 열려있으면 서버 재조회 후 Refresh
        if (_roomInfoPanel is null) return;
        _ = RefreshRoomInfoAsync();
    }

    private async Task RefreshRoomInfoAsync()
    {
        var updated = await _roomService.GetRoomAsync(_roomId).ConfigureAwait(false);
        if (updated is null || IsDisposed || !IsHandleCreated) return;

        _room = updated;
        Invoke(() =>
        {
            // 헤더 멤버 수 갱신
            var name = _room.IsDirectMessage
                ? _room.Name
                : $"{_room.Name} ({_room.Members.Count}명)";
            lblRoomName.Text = name;

            _roomInfoPanel?.Refresh(_room);
        });
    }

    private void OnMessageRemoved(object? sender, string messageId)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke(() => RemoveBubble(messageId));
        else RemoveBubble(messageId);
    }

    // ─────────────────────────────────────────────────────────
    //  Message handling
    // ─────────────────────────────────────────────────────────

    private void AddMessageBubble(ChatMessage message)
    {
        if (_bubbles.ContainsKey(message.MessageId)) return;

        int w = flpMessages.ClientSize.Width;
        if (w <= 0) w = flpMessages.Width;
        if (w <= 0) w = 400;

        // 직전 메시지와 같은 발신자(5분 이내)면 아바타·이름 생략
        bool hideHeader = false;
        if (flpMessages.Controls.Count > 0)
        {
            var prev = flpMessages.Controls[flpMessages.Controls.Count - 1] as MessageBubbleControl;
            if (prev is not null
                && prev.Message.SenderId == message.SenderId
                && !message.IsMine
                && (message.SentAt - prev.Message.SentAt).TotalMinutes < 5)
                hideHeader = true;
        }

        var bubble = new MessageBubbleControl(message, hideHeader)
        {
            Width       = w,
            MinimumSize = new Size(w, 0),
            MaximumSize = new Size(w, 0),
        };
        bubble.Expired += (_, m) =>
        {
            _messageStore.RemoveMessage(m.MessageId);
            _bubbles.Remove(m.MessageId);
        };
        flpMessages.Controls.Add(bubble);
        _bubbles[message.MessageId] = bubble;
    }

    private void RemoveBubble(string messageId)
    {
        if (_bubbles.TryGetValue(messageId, out var b))
        {
            flpMessages.Controls.Remove(b);
            _bubbles.Remove(messageId);
            b.Dispose();
        }
    }

    private void UpdateMessageStatus(string messageId, MessageStatus status)
    {
        if (_bubbles.TryGetValue(messageId, out var b))
            b.UpdateStatus(status);
    }

    private void ScrollToBottom()
    {
        if (flpMessages.Controls.Count == 0) return;
        flpMessages.PerformLayout();
        flpMessages.ScrollControlIntoView(flpMessages.Controls[flpMessages.Controls.Count - 1]);
    }

    // ─────────────────────────────────────────────────────────
    //  Send
    // ─────────────────────────────────────────────────────────

    private void txtInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            _ = SendMessageAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        var text = txtInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        btnSend.Enabled = false;
        int ttl = TtlValues[Math.Max(0, cmbTtl.SelectedIndex)];

        var msg = new ChatMessage
        {
            MessageId  = Guid.NewGuid().ToString(),
            RoomId     = _roomId,
            SenderId   = _authService.CurrentUser?.UserId ?? "me",
            SenderName = _authService.CurrentUser?.DisplayName ?? "나",
            PlainText  = text,
            SentAt     = DateTime.Now,
            TtlSeconds = ttl,
            ExpiresAt  = ttl > 0 ? DateTime.Now.AddSeconds(ttl) : null,
            IsMine     = true,
            Status     = MessageStatus.Sending
        };

        _messageStore.AddMessage(msg);
        AddMessageBubble(msg);
        ScrollToBottom();
        txtInput.Clear();

        try
        {
            await _chatService.SendMessageAsync(_roomId, text, ttl, msg.MessageId);
            UpdateMessageStatus(msg.MessageId, MessageStatus.Sent);
        }
        catch (Exception ex)
        {
            UpdateMessageStatus(msg.MessageId, MessageStatus.Failed);
            MessageBox.Show(ParentForm, $"전송 실패: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            btnSend.Enabled = true;
            txtInput.Focus();
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Room info overlay
    // ─────────────────────────────────────────────────────────

    private async Task ToggleRoomInfoAsync()
    {
        if (_roomInfoPanel != null)
        {
            Resize -= _roomInfoResizer;
            _roomInfoResizer = null;
            _roomInfoPanel.CloseRequested        -= _roomInfoCloseHandler;
            _roomInfoPanel.InviteRequested       -= _roomInfoInviteHandler;
            _roomInfoPanel.LeaveRoomConfirmed    -= _roomInfoLeaveHandler;
            _roomInfoPanel.TransferAdminRequested -= _roomInfoTransferHandler;
            _roomInfoCloseHandler = _roomInfoInviteHandler = _roomInfoLeaveHandler = null;
            _roomInfoTransferHandler = null;
            Controls.Remove(_roomInfoPanel);
            _roomInfoPanel.Dispose();
            _roomInfoPanel = null;
            return;
        }

        // 패널 열 때마다 항상 서버에서 최신 멤버 정보 재조회
        _room = await _roomService.GetRoomAsync(_roomId);

        if (_room == null) return;

        _roomInfoPanel = new RoomInfoPanel(_room, _authService, _roomService);
        _roomInfoPanel.Location = new Point(0, pnlHeader.Height);
        _roomInfoPanel.Size     = new Size(Width, Height - pnlHeader.Height);

        _roomInfoResizer = (_, _) =>
        {
            if (_roomInfoPanel != null)
            {
                _roomInfoPanel.Location = new Point(0, pnlHeader.Height);
                _roomInfoPanel.Size     = new Size(Width, Height - pnlHeader.Height);
            }
        };
        Resize += _roomInfoResizer;

        _roomInfoCloseHandler  = (_, _) => _ = ToggleRoomInfoAsync();
        _roomInfoInviteHandler = async (_, _) => await ShowInviteDialogAsync();
        _roomInfoLeaveHandler    = async (_, _) => await LeaveRoomAsync();
        _roomInfoTransferHandler = async (_, targetId) => await DoTransferAdminAsync(targetId);
        _roomInfoPanel.CloseRequested        += _roomInfoCloseHandler;
        _roomInfoPanel.InviteRequested       += _roomInfoInviteHandler;
        _roomInfoPanel.LeaveRoomConfirmed    += _roomInfoLeaveHandler;
        _roomInfoPanel.TransferAdminRequested += _roomInfoTransferHandler;
        Controls.Add(_roomInfoPanel);
        _roomInfoPanel.BringToFront();
    }

    private async Task LeaveRoomAsync()
    {
        // 방장이고 다른 멤버가 있으면 먼저 위임해야 함
        var myId = _authService.CurrentUser?.UserId;
        var latest = await _roomService.GetRoomAsync(_roomId);
        if (latest is not null && !latest.IsDirectMessage)
        {
            bool iAmAdmin = latest.Members.Any(m => m.UserId == myId && m.IsAdmin);
            var others    = latest.Members.Where(m => m.UserId != myId).ToList();

            if (iAmAdmin && others.Count > 0)
            {
                // 위임할 멤버를 고르는 다이얼로그
                var chosen = ShowTransferBeforeLeaveDialog(others);
                if (chosen is null) return;  // 취소

                var ok = await _roomService.TransferAdminAsync(_roomId, chosen.UserId);
                if (!ok)
                {
                    MessageBox.Show(ParentForm, "방장 위임에 실패했습니다. 다시 시도해주세요.",
                        "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

        try
        {
            if (_chatService is SignalRChatService sr)
                await sr.LeaveRoomHubAsync(Guid.Parse(_roomId));

            await _roomService.LeaveRoomAsync(_roomId);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ParentForm, $"방 나가기 실패: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    // 방 나가기 전 위임 대상을 선택하는 인라인 다이얼로그
    private Models.RoomMember? ShowTransferBeforeLeaveDialog(List<Models.RoomMember> candidates)
    {
        using var dlg   = new Form
        {
            Text            = "방장 위임 후 나가기",
            Size            = new Size(360, 260),
            StartPosition   = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox     = false, MinimizeBox = false,
            BackColor       = Color.White,
        };

        var lbl = new Label
        {
            Text      = "방을 나가기 전 새 방장을 선택하세요.",
            Font      = new Font("맑은 고딕", 9.5f),
            Location  = new Point(16, 16),
            AutoSize  = true,
        };

        var listBox = new ListBox
        {
            Location      = new Point(16, 44),
            Size          = new Size(dlg.ClientSize.Width - 32, 120),
            Font          = new Font("맑은 고딕", 9.5f),
            BorderStyle   = BorderStyle.FixedSingle,
        };
        foreach (var m in candidates)
            listBox.Items.Add(m.DisplayName);
        if (listBox.Items.Count > 0) listBox.SelectedIndex = 0;

        var btnOk = new Button
        {
            Text      = "위임 후 나가기",
            DialogResult = DialogResult.OK,
            Location  = new Point(dlg.ClientSize.Width - 230, 180),
            Size      = new Size(104, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(231, 76, 60),
            ForeColor = Color.White,
            Font      = new Font("맑은 고딕", 9f, FontStyle.Bold),
        };
        btnOk.FlatAppearance.BorderSize = 0;

        var btnCancel = new Button
        {
            Text         = "취소",
            DialogResult = DialogResult.Cancel,
            Location     = new Point(dlg.ClientSize.Width - 120, 180),
            Size         = new Size(104, 32),
            FlatStyle    = FlatStyle.Flat,
            BackColor    = Color.FromArgb(245, 245, 245),
            Font         = new Font("맑은 고딕", 9f),
        };
        btnCancel.FlatAppearance.BorderSize = 0;

        dlg.Controls.AddRange([lbl, listBox, btnOk, btnCancel]);
        dlg.AcceptButton = btnOk;
        dlg.CancelButton = btnCancel;

        if (dlg.ShowDialog(ParentForm) != DialogResult.OK) return null;
        int idx = listBox.SelectedIndex;
        return idx >= 0 ? candidates[idx] : null;
    }

    private async Task DoTransferAdminAsync(string targetUserId)
    {
        var ok = await _roomService.TransferAdminAsync(_roomId, targetUserId);
        if (!ok)
        {
            MessageBox.Show(ParentForm, "방장 위임에 실패했습니다.", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // 서버 재조회 후 패널 갱신
        var updated = await _roomService.GetRoomAsync(_roomId);
        if (updated is not null)
        {
            _room = updated;
            if (_roomInfoPanel is not null)
                Invoke(() => _roomInfoPanel.Refresh(_room));
        }
    }

    private async Task ShowInviteDialogAsync()
    {
        var contacts = await _contactService.GetContactsAsync();
        var existingIds = new HashSet<string>(_room?.MemberIds ?? []);

        using var dlg = new InviteMemberDialog(
            _room ?? new ChatRoom { RoomId = _roomId, Name = "방" },
            contacts,
            existingIds);

        if (dlg.ShowDialog(ParentForm) == DialogResult.OK && dlg.SelectedContacts.Count > 0)
        {
            foreach (var contact in dlg.SelectedContacts)
            {
                var ok = await _roomService.InviteMemberAsync(_roomId, contact.UserId);
                if (!ok)
                    MessageBox.Show(
                        $"{contact.DisplayName} 초대에 실패했습니다.",
                        "초대 오류",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
            }

            // 서버 재조회 후 RoomInfoPanel 즉시 갱신
            _room = await _roomService.GetRoomAsync(_roomId);
            if (_room is not null && _roomInfoPanel is not null)
                _roomInfoPanel.Refresh(_room);
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Cleanup
    // ─────────────────────────────────────────────────────────

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _chatService.MessageReceived   -= OnMessageReceived;
            _chatService.RoomMemberChanged -= OnRoomMemberChanged;
            _chatService.AdminTransferred  -= OnRoomMemberChanged;
            _messageStore.MessageRemoved   -= OnMessageRemoved;
            if (_chatService is SignalRChatService sr)
            {
                sr.MessageStatusUpdated -= OnMessageStatusUpdated;
                sr.ReadReceiptReceived  -= OnReadReceiptReceived;
                sr.OwnMessageConfirmed  -= OnOwnMessageConfirmed;
            }
        }
        base.Dispose(disposing);
    }

    // ─────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────

    private static Button MakeIconButton(string text, float fontSize, Color fore, Color back)
    {
        var btn = new Button
        {
            Text      = text,
            Font      = new Font("맑은 고딕", fontSize, FontStyle.Regular, GraphicsUnit.Point),
            FlatStyle = FlatStyle.Flat,
            BackColor = back,
            ForeColor = fore,
            Cursor    = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 245, 245);
        return btn;
    }

    private static void ApplyRoundedRegion(Control ctrl, int radius)
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
