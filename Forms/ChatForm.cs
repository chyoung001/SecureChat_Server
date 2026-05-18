using SecureChat.Controls;
using SecureChat.Models;
using SecureChat.Services;

namespace SecureChat.Forms;

public partial class ChatForm : Form
{
    private readonly string _roomId;
    private readonly IChatService _chatService;
    private readonly IMessageStore _messageStore;
    private readonly IAuthService _authService;
    private readonly IRoomService _roomService;
    private readonly Dictionary<string, MessageBubbleControl> _bubbles = new();
    private static readonly int[] TtlValues = { 0, 5, 30, 60, 300, 3600 };

    public ChatForm(
        string roomId,
        IChatService chatService,
        IMessageStore messageStore,
        IAuthService authService,
        IRoomService roomService)
    {
        _roomId = roomId;
        _chatService = chatService;
        _messageStore = messageStore;
        _authService = authService;
        _roomService = roomService;

        InitializeComponent();
        AcceptButton = null;
    }

    private async void ChatForm_Load(object? sender, EventArgs e)
    {
        try
        {
            var room = await _roomService.GetRoomAsync(_roomId);
            if (room != null)
            {
                lblRoomName.Text = room.IsDirectMessage
                    ? room.Name
                    : $"{room.Name} ({room.MemberIds.Count}명)";
                Text = $"SecureChat - {room.Name}";
            }

            await _roomService.JoinRoomAsync(_roomId);

            foreach (var m in _messageStore.GetMessagesForRoom(_roomId))
                AddMessageBubble(m);

            ScrollToBottom();

            _chatService.MessageReceived += OnMessageReceived;
            _messageStore.MessageRemoved += OnMessageRemoved;

            txtInput.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"채팅방을 열 수 없습니다.\n{ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Close();
        }
    }

    private void ChatForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _chatService.MessageReceived -= OnMessageReceived;
        _messageStore.MessageRemoved -= OnMessageRemoved;
    }

    // Hairline #eeeeee bottom border on white header
    private void PnlHeader_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(238, 238, 238));
        e.Graphics.DrawLine(pen, 0, pnlHeader.Height - 1, pnlHeader.Width, pnlHeader.Height - 1);
    }

    // Hairline #eeeeee top border on white input panel
    private void PnlInput_Paint(object? sender, PaintEventArgs e)
    {
        using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(238, 238, 238));
        e.Graphics.DrawLine(pen, 0, 0, pnlInput.Width, 0);
    }

    private void btnBack_Click(object? sender, EventArgs e) => Close();

    private async void btnSend_Click(object? sender, EventArgs e)
    {
        var text = txtInput.Text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        btnSend.Enabled = false;
        var ttl = TtlValues[Math.Max(0, cmbTtl.SelectedIndex)];

        var tempMessage = new ChatMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            RoomId = _roomId,
            SenderId = _authService.CurrentUser?.UserId ?? "me",
            SenderName = _authService.CurrentUser?.DisplayName ?? "나",
            PlainText = text,
            SentAt = DateTime.Now,
            TtlSeconds = ttl,
            ExpiresAt = ttl > 0 ? DateTime.Now.AddSeconds(ttl) : null,
            IsMine = true,
            Status = MessageStatus.Sending
        };

        _messageStore.AddMessage(tempMessage);
        AddMessageBubble(tempMessage);
        ScrollToBottom();
        txtInput.Clear();

        try
        {
            await _chatService.SendMessageAsync(_roomId, text, ttl);
            UpdateMessageStatus(tempMessage.MessageId, MessageStatus.Sent);
        }
        catch (Exception ex)
        {
            UpdateMessageStatus(tempMessage.MessageId, MessageStatus.Failed);
            MessageBox.Show(this, $"전송 실패: {ex.Message}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            btnSend.Enabled = true;
            txtInput.Focus();
        }
    }

    private void txtInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter && !e.Shift)
        {
            e.SuppressKeyPress = true;
            btnSend_Click(sender, EventArgs.Empty);
        }
    }

    private void OnMessageReceived(object? sender, ChatMessage message)
    {
        if (message.RoomId != _roomId) return;
        if (IsDisposed || !IsHandleCreated) return;

        if (InvokeRequired)
        {
            BeginInvoke(new Action(() =>
            {
                AddMessageBubble(message);
                ScrollToBottom();
            }));
        }
        else
        {
            AddMessageBubble(message);
            ScrollToBottom();
        }
    }

    private void OnMessageRemoved(object? sender, string messageId)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => RemoveBubble(messageId)));
        }
        else
        {
            RemoveBubble(messageId);
        }
    }

    private void RemoveBubble(string messageId)
    {
        if (_bubbles.TryGetValue(messageId, out var bubble))
        {
            flpMessages.Controls.Remove(bubble);
            _bubbles.Remove(messageId);
            bubble.Dispose();
        }
    }

    private void AddMessageBubble(ChatMessage message)
    {
        if (_bubbles.ContainsKey(message.MessageId)) return;

        var bubble = new MessageBubbleControl(message)
        {
            Width = flpMessages.ClientSize.Width - 4
        };
        bubble.Expired += (_, m) =>
        {
            _messageStore.RemoveMessage(m.MessageId);
            _bubbles.Remove(m.MessageId);
        };
        flpMessages.Controls.Add(bubble);
        _bubbles[message.MessageId] = bubble;
    }

    private void UpdateMessageStatus(string messageId, MessageStatus status)
    {
        if (_bubbles.TryGetValue(messageId, out var bubble))
            bubble.UpdateStatus(status);
    }

    private void ScrollToBottom()
    {
        if (flpMessages.Controls.Count == 0) return;
        var last = flpMessages.Controls[flpMessages.Controls.Count - 1];
        flpMessages.ScrollControlIntoView(last);
    }
}
