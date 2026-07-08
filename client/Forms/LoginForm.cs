using Microsoft.Extensions.DependencyInjection;
using SecureChat.Services;

namespace SecureChat.Forms;

public partial class LoginForm : Form
{
    private readonly IAuthService _authService;
    private readonly IChatService _chatService;
    private int  _currentTab      = 0; // 0 = login, 1 = signup
    private bool _passwordVisible = false;

    // Tana palette — tab switcher state
    private static readonly Color ClrGreen   = Color.FromArgb(225, 240, 189); // Lush Meadow  — active tab / CTA
    private static readonly Color ClrSurface = Color.FromArgb(14,  14,  14);  // Graphite Base — inactive tab bg
    private static readonly Color ClrSilver  = Color.FromArgb(179, 179, 179); // Muted Silver  — inactive tab text
    private static readonly Color ClrCanvas  = Color.FromArgb(0,   0,   0);   // Deep Midnight — text on Lush Meadow

    // Tana palette — status indicator
    private static readonly Color ClrText    = Color.FromArgb(240, 237, 237); // Cloud Whisper — success / connected
    private static readonly Color ClrStone   = Color.FromArgb(128, 128, 128); // Stone Gray    — loading
    private static readonly Color ClrAsh     = Color.FromArgb(96,  96,  96);  // Soft Ash      — idle
    private static readonly Color ClrErr     = Color.FromArgb(176, 72,  72);  // muted red     — error

    public LoginForm(IAuthService authService, IChatService chatService)
    {
        _authService = authService;
        _chatService = chatService;

        InitializeComponent();

        if (cmbServer.Items.Count > 0) cmbServer.SelectedIndex = 0;
        SetStatus("준비", ClrAsh);
        ApplyTabState();
    }

    internal void BtnTabLogin_Click(object? sender, EventArgs e)
    {
        _currentTab = 0;
        ApplyTabState();
    }

    internal void BtnTabSignUp_Click(object? sender, EventArgs e)
    {
        _currentTab = 1;
        ApplyTabState();
    }

    internal void BtnShowPass_Click(object? sender, EventArgs e)
    {
        _passwordVisible = !_passwordVisible;
        txtPassword.UseSystemPasswordChar = !_passwordVisible;
        btnShowPass.Text = _passwordVisible ? "숨김" : "표시";
    }

    private void ApplyTabState()
    {
        bool isSignUp = _currentTab == 1;

        btnTabLogin.BackColor  = isSignUp ? ClrSurface : ClrGreen;
        btnTabLogin.ForeColor  = isSignUp ? ClrSilver  : ClrCanvas;
        btnTabSignUp.BackColor = isSignUp ? ClrGreen   : ClrSurface;
        btnTabSignUp.ForeColor = isSignUp ? ClrCanvas  : ClrSilver;

        lblPasswordConfirm.Visible   = isSignUp;
        pnlPasswordConfirmBg.Visible = isSignUp;

        chkRemember.Visible = !isSignUp;
        lnkForgot.Visible   = !isSignUp;

        btnConnect.Text = isSignUp ? "회원가입 완료" : "로그인";
    }

    private async void btnConnect_Click(object? sender, EventArgs e)
    {
        var username  = txtUsername.Text.Trim();
        var password  = txtPassword.Text;
        var server    = cmbServer.Text.Trim();
        bool isSignUp = _currentTab == 1;

        if (string.IsNullOrWhiteSpace(username))
        {
            SetStatus("사용자명을 입력하세요", ClrErr);
            txtUsername.Focus();
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            SetStatus("비밀번호를 입력하세요", ClrErr);
            txtPassword.Focus();
            return;
        }
        if (isSignUp && password != txtPasswordConfirm.Text)
        {
            SetStatus("비밀번호가 일치하지 않습니다", ClrErr);
            txtPasswordConfirm.Focus();
            return;
        }
        if (string.IsNullOrWhiteSpace(server))
        {
            SetStatus("서버 주소를 입력하세요", ClrErr);
            cmbServer.Focus();
            return;
        }

        btnConnect.Enabled = false;
        SetStatus("연결 중...", ClrStone);

        try
        {
            bool ok = isSignUp
                ? await _authService.SignUpAsync(username, password, server)
                : await _authService.LoginAsync(username, password, server);

            if (!ok)
            {
                SetStatus(isSignUp ? "회원가입 실패" : "로그인 실패", ClrErr);
                btnConnect.Enabled = true;
                return;
            }

            SetStatus("서버 연결 중...", ClrStone);
            await _chatService.ConnectAsync();

            SetStatus("연결됨", ClrText);

            var mainForm = Program.Services.GetRequiredService<MainForm>();
            mainForm.FormClosed += (_, _) => Close();
            Hide();
            mainForm.Show();
        }
        catch (Exception ex)
        {
            SetStatus($"오류: {ex.Message}", ClrErr);
            btnConnect.Enabled = true;
        }
    }

    private void SetStatus(string text, Color color)
    {
        lblStatus.Text          = text;
        lblStatus.ForeColor     = color;
        picStatusIcon.BackColor = color;
    }
}
