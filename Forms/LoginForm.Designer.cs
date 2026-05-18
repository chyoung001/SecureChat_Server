#nullable enable
namespace SecureChat.Forms
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer? components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            // ── Musinsa palette ───────────────────────────────────
            var clrCanvas  = Color.White;                         // #ffffff  page canvas
            var clrSurface = Color.FromArgb(245, 245, 245);      // #f5f5f5  input bg / status bar
            var clrBorder  = Color.FromArgb(221, 221, 221);      // #dddddd  input border default
            var clrBody    = Color.FromArgb(34,  34,  34);       // #222222  input text
            var clrMeta    = Color.FromArgb(153, 153, 153);      // #999999  labels / placeholder
            var clrDisabled= Color.FromArgb(187, 187, 187);      // #bbbbbb  version / disabled
            var clrCta     = Color.Black;                         // #000000  primary CTA fill
            var clrCtaTxt  = Color.White;                         // #ffffff  CTA text

            var fontTitle = new Font("맑은 고딕", 20F, FontStyle.Bold);
            var fontBase  = new Font("맑은 고딕", 9.5F);
            var fontSmall = new Font("맑은 고딕", 8.5F);
            var fontBold  = new Font("맑은 고딕", 10F, FontStyle.Bold);

            picStatusIcon = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)(picStatusIcon)).BeginInit();
            this.SuspendLayout();

            // ── Title ─────────────────────────────────────────────
            lblTitle = new Label
            {
                AutoSize  = false,
                Font      = fontTitle,
                ForeColor = clrCta,
                BackColor = clrCanvas,
                Location  = new Point(0, 32),
                Size      = new Size(420, 36),
                TextAlign = ContentAlignment.MiddleCenter,
                Text      = "SecureChat"
            };

            // ── Subtitle ──────────────────────────────────────────
            lblSubtitle = new Label
            {
                AutoSize  = false,
                Font      = fontSmall,
                ForeColor = clrMeta,
                BackColor = clrCanvas,
                Location  = new Point(0, 72),
                Size      = new Size(420, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                Text      = "End-to-end 암호화 메신저"
            };

            // ── Tab switcher ──────────────────────────────────────
            pnlTabSwitch = new Panel
            {
                Location  = new Point(48, 108),
                Size      = new Size(324, 36),
                BackColor = clrSurface
            };

            btnTabLogin = new Button
            {
                Location  = new Point(0, 0),
                Size      = new Size(162, 36),
                Text      = "로그인",
                Font      = fontBase,
                FlatStyle = FlatStyle.Flat,
                BackColor = clrCta,
                ForeColor = clrCtaTxt,
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnTabLogin.FlatAppearance.BorderSize = 0;

            btnTabSignUp = new Button
            {
                Location  = new Point(162, 0),
                Size      = new Size(162, 36),
                Text      = "회원가입",
                Font      = fontBase,
                FlatStyle = FlatStyle.Flat,
                BackColor = clrSurface,
                ForeColor = clrMeta,
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnTabSignUp.FlatAppearance.BorderSize = 0;

            pnlTabSwitch.Controls.Add(btnTabLogin);
            pnlTabSwitch.Controls.Add(btnTabSignUp);

            // ── Username ──────────────────────────────────────────
            lblUsernameHint = new Label
            {
                AutoSize  = true,
                Font      = fontSmall,
                ForeColor = clrMeta,
                BackColor = clrCanvas,
                Location  = new Point(48, 162),
                Text      = "사용자명"
            };

            pnlUsernameBg = new Panel
            {
                Location  = new Point(48, 180),
                Size      = new Size(324, 42),
                BackColor = clrBorder
            };
            var pnlUsernameBody = new Panel
            {
                Location  = new Point(1, 1),
                Size      = new Size(322, 40),
                BackColor = clrCanvas
            };
            txtUsername = new TextBox
            {
                Location    = new Point(10, 9),
                Size        = new Size(300, 22),
                Font        = fontBase,
                BorderStyle = BorderStyle.None,
                BackColor   = clrCanvas,
                ForeColor   = clrBody
            };
            pnlUsernameBody.Controls.Add(txtUsername);
            pnlUsernameBg.Controls.Add(pnlUsernameBody);

            // ── Password ──────────────────────────────────────────
            lblPasswordHint = new Label
            {
                AutoSize  = true,
                Font      = fontSmall,
                ForeColor = clrMeta,
                BackColor = clrCanvas,
                Location  = new Point(48, 232),
                Text      = "비밀번호"
            };

            pnlPasswordBg = new Panel
            {
                Location  = new Point(48, 250),
                Size      = new Size(324, 42),
                BackColor = clrBorder
            };
            var pnlPasswordBody = new Panel
            {
                Location  = new Point(1, 1),
                Size      = new Size(322, 40),
                BackColor = clrCanvas
            };
            txtPassword = new TextBox
            {
                Location              = new Point(10, 9),
                Size                  = new Size(258, 22),
                Font                  = fontBase,
                BorderStyle           = BorderStyle.None,
                BackColor             = clrCanvas,
                ForeColor             = clrBody,
                UseSystemPasswordChar = true
            };
            btnShowPass = new Button
            {
                Location  = new Point(270, 7),
                Size      = new Size(44, 26),
                Text      = "표시",
                Font      = fontSmall,
                FlatStyle = FlatStyle.Flat,
                BackColor = clrCanvas,
                ForeColor = clrMeta,
                Cursor    = Cursors.Hand,
                TabStop   = false
            };
            btnShowPass.FlatAppearance.BorderSize = 0;
            pnlPasswordBody.Controls.Add(txtPassword);
            pnlPasswordBody.Controls.Add(btnShowPass);
            pnlPasswordBg.Controls.Add(pnlPasswordBody);

            // ── Password confirm (signup only) ────────────────────
            lblPasswordConfirm = new Label
            {
                AutoSize  = true,
                Font      = fontSmall,
                ForeColor = clrMeta,
                BackColor = clrCanvas,
                Location  = new Point(48, 302),
                Text      = "비밀번호 확인",
                Visible   = false
            };

            pnlPasswordConfirmBg = new Panel
            {
                Location  = new Point(48, 320),
                Size      = new Size(324, 42),
                BackColor = clrBorder,
                Visible   = false
            };
            var pnlPasswordConfirmBody = new Panel
            {
                Location  = new Point(1, 1),
                Size      = new Size(322, 40),
                BackColor = clrCanvas
            };
            txtPasswordConfirm = new TextBox
            {
                Location              = new Point(10, 9),
                Size                  = new Size(300, 22),
                Font                  = fontBase,
                BorderStyle           = BorderStyle.None,
                BackColor             = clrCanvas,
                ForeColor             = clrBody,
                UseSystemPasswordChar = true
            };
            pnlPasswordConfirmBody.Controls.Add(txtPasswordConfirm);
            pnlPasswordConfirmBg.Controls.Add(pnlPasswordConfirmBody);

            // ── Remember / Forgot (login only) ────────────────────
            chkRemember = new CheckBox
            {
                Location  = new Point(48, 304),
                Size      = new Size(160, 20),
                Text      = "로그인 상태 유지",
                Font      = fontSmall,
                ForeColor = clrMeta,
                BackColor = clrCanvas,
                Cursor    = Cursors.Hand
            };

            lnkForgot = new LinkLabel
            {
                AutoSize        = false,
                Location        = new Point(280, 304),
                Size            = new Size(92, 20),
                Text            = "비밀번호 잊음",
                Font            = fontSmall,
                TextAlign       = ContentAlignment.MiddleRight,
                LinkColor       = clrMeta,
                ActiveLinkColor = clrBody,
                BackColor       = clrCanvas
            };

            // ── Server ────────────────────────────────────────────
            lblServer = new Label
            {
                AutoSize  = true,
                Font      = fontSmall,
                ForeColor = clrMeta,
                BackColor = clrCanvas,
                Location  = new Point(48, 372),
                Text      = "서버"
            };

            pnlServerBg = new Panel
            {
                Location  = new Point(48, 390),
                Size      = new Size(324, 42),
                BackColor = clrBorder
            };
            var pnlServerBody = new Panel
            {
                Location  = new Point(1, 1),
                Size      = new Size(322, 40),
                BackColor = clrCanvas
            };
            cmbServer = new ComboBox
            {
                Location      = new Point(8, 7),
                Size          = new Size(306, 26),
                Font          = fontBase,
                FlatStyle     = FlatStyle.Flat,
                BackColor     = clrCanvas,
                ForeColor     = clrBody,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            cmbServer.Items.AddRange(new object[] { "https://chat.example.com" });
            pnlServerBody.Controls.Add(cmbServer);
            pnlServerBg.Controls.Add(pnlServerBody);

            // ── Primary CTA ───────────────────────────────────────
            btnConnect = new Button
            {
                Location  = new Point(48, 450),
                Size      = new Size(324, 44),
                Text      = "로그인",
                Font      = fontBold,
                FlatStyle = FlatStyle.Flat,
                BackColor = clrCta,
                ForeColor = clrCtaTxt,
                Cursor    = Cursors.Hand
            };
            btnConnect.FlatAppearance.BorderSize = 0;

            // ── Status bar ────────────────────────────────────────
            pnlStatusBar = new Panel
            {
                Location  = new Point(0, 520),
                Size      = new Size(420, 32),
                BackColor = clrSurface
            };

            picStatusIcon.Location  = new Point(16, 10);
            picStatusIcon.Size      = new Size(12, 12);
            picStatusIcon.BackColor = clrDisabled;
            picStatusIcon.SizeMode  = PictureBoxSizeMode.Normal;

            lblStatus = new Label
            {
                AutoSize  = true,
                Location  = new Point(34, 8),
                Font      = fontSmall,
                ForeColor = clrMeta,
                BackColor = clrSurface,
                Text      = "준비"
            };

            lblVersion = new Label
            {
                AutoSize  = false,
                Location  = new Point(302, 8),
                Size      = new Size(102, 16),
                Font      = fontSmall,
                ForeColor = clrDisabled,
                BackColor = clrSurface,
                TextAlign = ContentAlignment.MiddleRight,
                Text      = "v1.0.0"
            };

            pnlStatusBar.Controls.Add(picStatusIcon);
            pnlStatusBar.Controls.Add(lblStatus);
            pnlStatusBar.Controls.Add(lblVersion);

            // ── Wire events ───────────────────────────────────────
            btnConnect.Click   += new EventHandler(this.btnConnect_Click);
            btnTabLogin.Click  += new EventHandler(this.BtnTabLogin_Click);
            btnTabSignUp.Click += new EventHandler(this.BtnTabSignUp_Click);
            btnShowPass.Click  += new EventHandler(this.BtnShowPass_Click);

            // ── Form ──────────────────────────────────────────────
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode       = AutoScaleMode.Font;
            this.ClientSize          = new Size(420, 552);
            this.BackColor           = clrCanvas;
            this.FormBorderStyle     = FormBorderStyle.FixedSingle;
            this.MaximizeBox         = false;
            this.StartPosition       = FormStartPosition.CenterScreen;
            this.AcceptButton        = btnConnect;
            this.Name                = "LoginForm";
            this.Text                = "SecureChat";

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblSubtitle);
            this.Controls.Add(pnlTabSwitch);
            this.Controls.Add(lblUsernameHint);
            this.Controls.Add(pnlUsernameBg);
            this.Controls.Add(lblPasswordHint);
            this.Controls.Add(pnlPasswordBg);
            this.Controls.Add(lblPasswordConfirm);
            this.Controls.Add(pnlPasswordConfirmBg);
            this.Controls.Add(chkRemember);
            this.Controls.Add(lnkForgot);
            this.Controls.Add(lblServer);
            this.Controls.Add(pnlServerBg);
            this.Controls.Add(btnConnect);
            this.Controls.Add(pnlStatusBar);

            ((System.ComponentModel.ISupportInitialize)(picStatusIcon)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private Label      lblTitle             = null!;
        private Label      lblSubtitle          = null!;
        private Panel      pnlTabSwitch         = null!;
        private Button     btnTabLogin          = null!;
        private Button     btnTabSignUp         = null!;
        private Label      lblUsernameHint      = null!;
        private Panel      pnlUsernameBg        = null!;
        private TextBox    txtUsername          = null!;
        private Label      lblPasswordHint      = null!;
        private Panel      pnlPasswordBg        = null!;
        private TextBox    txtPassword          = null!;
        private Button     btnShowPass          = null!;
        private Label      lblPasswordConfirm   = null!;
        private Panel      pnlPasswordConfirmBg = null!;
        private TextBox    txtPasswordConfirm   = null!;
        private CheckBox   chkRemember          = null!;
        private LinkLabel  lnkForgot            = null!;
        private Label      lblServer            = null!;
        private Panel      pnlServerBg          = null!;
        private ComboBox   cmbServer            = null!;
        private Button     btnConnect           = null!;
        private Panel      pnlStatusBar         = null!;
        private PictureBox picStatusIcon        = null!;
        private Label      lblStatus            = null!;
        private Label      lblVersion           = null!;
    }
}
