#nullable enable
using System.Drawing.Drawing2D;

namespace SecureChat.Forms
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer? components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            // ── Left sidebar ──────────────────────────────────────
            this.pnlLeft        = new System.Windows.Forms.Panel();
            this.pnlHeader      = new System.Windows.Forms.Panel();
            this.pnlAvatar      = new System.Windows.Forms.Panel();
            this.lblDisplayName = new System.Windows.Forms.Label();
            this.lblUsername    = new System.Windows.Forms.Label();
            this.btnAddContact    = new System.Windows.Forms.Button();
            this.btnSettings      = new System.Windows.Forms.Button();
            this.pnlNotifBtn      = new System.Windows.Forms.Panel();
            this.pnlSearch      = new System.Windows.Forms.Panel();
            this.txtSearch      = new System.Windows.Forms.TextBox();
            this.lblCtrlK       = new System.Windows.Forms.Label();
            this.pnlFilterTabs  = new System.Windows.Forms.Panel();
            this.btnNewRoom     = new System.Windows.Forms.Button();
            this.flpRoomList    = new SecureChat.Controls.RoomListPanel();
            this.statusBar      = new System.Windows.Forms.StatusStrip();
            this.lblConnectionStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblFingerprint      = new System.Windows.Forms.ToolStripStatusLabel();

            // ── Divider ───────────────────────────────────────────
            this.pnlDivider = new System.Windows.Forms.Panel();

            // ── Right panel ───────────────────────────────────────
            this.pnlRight          = new System.Windows.Forms.Panel();
            this.pnlEmptyState     = new System.Windows.Forms.Panel();
            this.pnlShield         = new System.Windows.Forms.Panel();
            this.lblWelcomeTitle   = new System.Windows.Forms.Label();
            this.lblWelcomeSubtitle = new System.Windows.Forms.Label();
            this.btnCreateRoom     = new System.Windows.Forms.Button();
            this.btnAddContactRight = new System.Windows.Forms.Button();
            this.pnlTip            = new System.Windows.Forms.Panel();
            this.lblTip            = new System.Windows.Forms.Label();

            // ═══════════════════════════════════════════════════
            //  LEFT SIDEBAR
            // ═══════════════════════════════════════════════════

            // pnlLeft — white canvas
            this.pnlLeft.Width     = 380;
            this.pnlLeft.Dock      = System.Windows.Forms.DockStyle.Left;
            this.pnlLeft.BackColor = System.Drawing.Color.White;
            this.pnlLeft.Controls.Add(this.flpRoomList);
            this.pnlLeft.Controls.Add(this.statusBar);
            this.pnlLeft.Controls.Add(this.pnlFilterTabs);
            this.pnlLeft.Controls.Add(this.pnlSearch);
            this.pnlLeft.Controls.Add(this.pnlHeader);

            // ── Header — white with hairline bottom border ───────
            this.pnlHeader.Dock      = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height    = 90;
            this.pnlHeader.BackColor = System.Drawing.Color.White;
            this.pnlHeader.Controls.Add(this.pnlAvatar);
            this.pnlHeader.Controls.Add(this.lblDisplayName);
            this.pnlHeader.Controls.Add(this.lblUsername);
            this.pnlHeader.Controls.Add(this.btnAddContact);
            this.pnlHeader.Controls.Add(this.btnSettings);
            this.pnlHeader.Paint += new System.Windows.Forms.PaintEventHandler(this.PnlHeader_Paint);

            // pnlAvatar
            this.pnlAvatar.Size      = new System.Drawing.Size(46, 46);
            this.pnlAvatar.Location  = new System.Drawing.Point(16, 22);
            this.pnlAvatar.BackColor = System.Drawing.Color.Transparent;
            this.pnlAvatar.Paint    += new System.Windows.Forms.PaintEventHandler(this.PnlAvatar_Paint);

            // lblDisplayName — pure black heading
            this.lblDisplayName.Text      = "사용자";
            this.lblDisplayName.Font      = new System.Drawing.Font("맑은 고딕", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblDisplayName.ForeColor = System.Drawing.Color.Black;
            this.lblDisplayName.BackColor = System.Drawing.Color.Transparent;
            this.lblDisplayName.Location  = new System.Drawing.Point(72, 22);
            this.lblDisplayName.AutoSize  = true;

            // lblUsername — #999999 secondary text
            this.lblUsername.Text      = "@사용자";
            this.lblUsername.Font      = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblUsername.ForeColor = System.Drawing.Color.FromArgb(153, 153, 153);
            this.lblUsername.BackColor = System.Drawing.Color.Transparent;
            this.lblUsername.Location  = new System.Drawing.Point(73, 44);
            this.lblUsername.AutoSize  = true;

            // btnAddContact — ghost icon button
            this.btnAddContact.Text      = "+";
            this.btnAddContact.Font      = new System.Drawing.Font("맑은 고딕", 12F, System.Drawing.FontStyle.Bold);
            this.btnAddContact.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddContact.FlatAppearance.BorderSize          = 0;
            this.btnAddContact.FlatAppearance.MouseOverBackColor  = System.Drawing.Color.FromArgb(245, 245, 245);
            this.btnAddContact.BackColor = System.Drawing.Color.Transparent;
            this.btnAddContact.ForeColor = System.Drawing.Color.FromArgb(153, 153, 153);
            this.btnAddContact.Size      = new System.Drawing.Size(34, 34);
            this.btnAddContact.Location  = new System.Drawing.Point(296, 28);
            this.btnAddContact.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnAddContact.Click    += new System.EventHandler(this.btnAddContact_Click);

            // btnSettings — ghost icon button
            this.btnSettings.Text      = "=";
            this.btnSettings.Font      = new System.Drawing.Font("맑은 고딕", 13F);
            this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSettings.FlatAppearance.BorderSize          = 0;
            this.btnSettings.FlatAppearance.MouseOverBackColor  = System.Drawing.Color.FromArgb(245, 245, 245);
            this.btnSettings.BackColor = System.Drawing.Color.Transparent;
            this.btnSettings.ForeColor = System.Drawing.Color.FromArgb(153, 153, 153);
            this.btnSettings.Size      = new System.Drawing.Size(34, 34);
            this.btnSettings.Location  = new System.Drawing.Point(334, 28);
            this.btnSettings.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnSettings.Click    += new System.EventHandler(this.btnSettings_Click);

            // pnlNotifBtn — 친구 요청 알림 벨 버튼 (항상 표시, 배지는 내부 Paint에서 그림)
            this.pnlNotifBtn.Size        = new System.Drawing.Size(40, 40);
            this.pnlNotifBtn.Location    = new System.Drawing.Point(200, 8);
            this.pnlNotifBtn.BackColor   = System.Drawing.Color.Transparent;
            this.pnlNotifBtn.Cursor      = System.Windows.Forms.Cursors.Hand;
            this.pnlNotifBtn.Paint      += new System.Windows.Forms.PaintEventHandler(this.PnlNotifBtn_Paint);
            this.pnlNotifBtn.Click      += new System.EventHandler(this.PnlNotifBtn_Click);
            this.pnlNotifBtn.MouseEnter += new System.EventHandler(this.PnlNotifBtn_MouseEnter);
            this.pnlNotifBtn.MouseLeave += new System.EventHandler(this.PnlNotifBtn_MouseLeave);

            // ── Search ──────────────────────────────────────────
            this.pnlSearch.Dock      = System.Windows.Forms.DockStyle.Top;
            this.pnlSearch.Height    = 56;
            this.pnlSearch.BackColor = System.Drawing.Color.White;
            this.pnlSearch.Controls.Add(this.txtSearch);
            this.pnlSearch.Controls.Add(this.lblCtrlK);
            this.pnlSearch.Paint    += new System.Windows.Forms.PaintEventHandler(this.PnlSearch_Paint);

            // txtSearch — #f5f5f5 search bar
            this.txtSearch.BorderStyle     = System.Windows.Forms.BorderStyle.None;
            this.txtSearch.Font            = new System.Drawing.Font("맑은 고딕", 9.5F);
            this.txtSearch.BackColor       = System.Drawing.Color.FromArgb(245, 245, 245);
            this.txtSearch.ForeColor       = System.Drawing.Color.FromArgb(34, 34, 34);
            this.txtSearch.PlaceholderText = "대화 검색...";
            this.txtSearch.Location        = new System.Drawing.Point(44, 18);
            this.txtSearch.Size            = new System.Drawing.Size(288, 20);
            this.txtSearch.TextChanged    += new System.EventHandler(this.txtSearch_TextChanged);

            // lblCtrlK — #999999 meta
            this.lblCtrlK.Text      = "";
            this.lblCtrlK.Font      = new System.Drawing.Font("맑은 고딕", 7.5F);
            this.lblCtrlK.ForeColor = System.Drawing.Color.FromArgb(153, 153, 153);
            this.lblCtrlK.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.lblCtrlK.AutoSize  = true;
            this.lblCtrlK.Location  = new System.Drawing.Point(340, 19);

            // ── Filter tabs ─────────────────────────────────────
            this.pnlFilterTabs.Dock      = System.Windows.Forms.DockStyle.Top;
            this.pnlFilterTabs.Height    = 56;
            this.pnlFilterTabs.BackColor = System.Drawing.Color.White;
            this.pnlFilterTabs.Controls.Add(this.btnNewRoom);
            this.pnlFilterTabs.Controls.Add(this.pnlNotifBtn);
            this.pnlFilterTabs.Paint      += new System.Windows.Forms.PaintEventHandler(this.PnlFilterTabs_Paint);
            this.pnlFilterTabs.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PnlFilterTabs_MouseClick);

            // btnNewRoom — primary CTA: black bg, white text
            this.btnNewRoom.Text      = "+ 방 추가";
            this.btnNewRoom.Font      = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Bold);
            this.btnNewRoom.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNewRoom.FlatAppearance.BorderSize = 0;
            this.btnNewRoom.BackColor = System.Drawing.Color.Black;
            this.btnNewRoom.ForeColor = System.Drawing.Color.White;
            this.btnNewRoom.Size      = new System.Drawing.Size(100, 34);
            this.btnNewRoom.Location  = new System.Drawing.Point(268, 11);
            this.btnNewRoom.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnNewRoom.Click    += new System.EventHandler(this.btnNewRoom_Click);

            // ── Room list ───────────────────────────────────────
            this.flpRoomList.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.flpRoomList.BackColor = System.Drawing.Color.White;
            this.flpRoomList.Padding   = new System.Windows.Forms.Padding(0);

            // ── Status bar ──────────────────────────────────────
            this.statusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
                { this.lblConnectionStatus, this.lblFingerprint });
            this.statusBar.SizingGrip = false;
            this.statusBar.BackColor  = System.Drawing.Color.FromArgb(245, 245, 245);
            this.statusBar.Padding    = new System.Windows.Forms.Padding(4, 0, 4, 0);

            this.lblConnectionStatus.Text      = "● 연결 안 됨";
            this.lblConnectionStatus.ForeColor = System.Drawing.Color.FromArgb(153, 153, 153);

            this.lblFingerprint.Text      = "";
            this.lblFingerprint.ForeColor = System.Drawing.Color.FromArgb(153, 153, 153);
            this.lblFingerprint.Spring    = true;
            this.lblFingerprint.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

            // ═══════════════════════════════════════════════════
            //  DIVIDER — hairline #eeeeee
            // ═══════════════════════════════════════════════════
            this.pnlDivider.Width     = 1;
            this.pnlDivider.Dock      = System.Windows.Forms.DockStyle.Left;
            this.pnlDivider.BackColor = System.Drawing.Color.FromArgb(238, 238, 238);

            // ═══════════════════════════════════════════════════
            //  RIGHT PANEL
            // ═══════════════════════════════════════════════════
            this.pnlRight.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.pnlRight.BackColor = System.Drawing.Color.White;
            this.pnlRight.Controls.Add(this.pnlEmptyState);
            this.pnlRight.Resize   += new System.EventHandler(this.PnlRight_Resize);

            // pnlEmptyState (centered)
            this.pnlEmptyState.Size      = new System.Drawing.Size(360, 420);
            this.pnlEmptyState.BackColor = System.Drawing.Color.Transparent;
            this.pnlEmptyState.Controls.Add(this.pnlShield);
            this.pnlEmptyState.Controls.Add(this.lblWelcomeTitle);
            this.pnlEmptyState.Controls.Add(this.lblWelcomeSubtitle);
            this.pnlEmptyState.Controls.Add(this.btnCreateRoom);
            this.pnlEmptyState.Controls.Add(this.btnAddContactRight);
            this.pnlEmptyState.Controls.Add(this.pnlTip);

            // pnlShield
            this.pnlShield.Size      = new System.Drawing.Size(96, 96);
            this.pnlShield.Location  = new System.Drawing.Point(132, 20);
            this.pnlShield.BackColor = System.Drawing.Color.White;
            this.pnlShield.Paint    += new System.Windows.Forms.PaintEventHandler(this.PnlShield_Paint);

            // lblWelcomeTitle — black heading
            this.lblWelcomeTitle.Text      = "안전한 대화를 시작하세요";
            this.lblWelcomeTitle.Font      = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.lblWelcomeTitle.ForeColor = System.Drawing.Color.Black;
            this.lblWelcomeTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblWelcomeTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblWelcomeTitle.Size      = new System.Drawing.Size(360, 34);
            this.lblWelcomeTitle.Location  = new System.Drawing.Point(0, 130);

            // lblWelcomeSubtitle — #999999
            this.lblWelcomeSubtitle.Text      = "왼쪽에서 대화를 선택하거나, 새 방을 만들어\n친구와 E2E 암호화 메시지를 주고받으세요.";
            this.lblWelcomeSubtitle.Font      = new System.Drawing.Font("맑은 고딕", 9.5F);
            this.lblWelcomeSubtitle.ForeColor = System.Drawing.Color.FromArgb(153, 153, 153);
            this.lblWelcomeSubtitle.BackColor = System.Drawing.Color.Transparent;
            this.lblWelcomeSubtitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblWelcomeSubtitle.Size      = new System.Drawing.Size(360, 52);
            this.lblWelcomeSubtitle.Location  = new System.Drawing.Point(0, 172);

            // btnCreateRoom — primary CTA: black bg, white text
            this.btnCreateRoom.Text      = "+ 새 방 만들기";
            this.btnCreateRoom.Font      = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnCreateRoom.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCreateRoom.FlatAppearance.BorderSize = 0;
            this.btnCreateRoom.BackColor = System.Drawing.Color.Black;
            this.btnCreateRoom.ForeColor = System.Drawing.Color.White;
            this.btnCreateRoom.Size      = new System.Drawing.Size(160, 40);
            this.btnCreateRoom.Location  = new System.Drawing.Point(10, 244);
            this.btnCreateRoom.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnCreateRoom.Click    += new System.EventHandler(this.btnNewRoom_Click);

            // btnAddContactRight — secondary outline: black border/text, white bg
            this.btnAddContactRight.Text      = "친구 추가";
            this.btnAddContactRight.Font      = new System.Drawing.Font("맑은 고딕", 9.5F);
            this.btnAddContactRight.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAddContactRight.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.btnAddContactRight.FlatAppearance.BorderSize  = 1;
            this.btnAddContactRight.BackColor = System.Drawing.Color.White;
            this.btnAddContactRight.ForeColor = System.Drawing.Color.Black;
            this.btnAddContactRight.Size      = new System.Drawing.Size(160, 40);
            this.btnAddContactRight.Location  = new System.Drawing.Point(180, 244);
            this.btnAddContactRight.Cursor    = System.Windows.Forms.Cursors.Hand;
            this.btnAddContactRight.Click    += new System.EventHandler(this.btnAddContactRight_Click);

            // pnlTip — #f5f5f5 surface
            this.pnlTip.Size      = new System.Drawing.Size(330, 44);
            this.pnlTip.Location  = new System.Drawing.Point(10, 310);
            this.pnlTip.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.pnlTip.Controls.Add(this.lblTip);
            this.pnlTip.Paint    += new System.Windows.Forms.PaintEventHandler(this.PnlTip_Paint);

            // lblTip — #999999 meta
            this.lblTip.Text      = "Ctrl+K 로 어디서든 대화 검색이 가능합니다.";
            this.lblTip.Font      = new System.Drawing.Font("맑은 고딕", 9F);
            this.lblTip.ForeColor = System.Drawing.Color.FromArgb(153, 153, 153);
            this.lblTip.BackColor = System.Drawing.Color.Transparent;
            this.lblTip.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblTip.Dock      = System.Windows.Forms.DockStyle.Fill;
            this.lblTip.Padding   = new System.Windows.Forms.Padding(12, 0, 0, 0);

            // ═══════════════════════════════════════════════════
            //  MAIN FORM
            // ═══════════════════════════════════════════════════
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize          = new System.Drawing.Size(1050, 700);
            this.MinimumSize         = new System.Drawing.Size(680, 500);
            this.BackColor           = System.Drawing.Color.White;
            this.Text                = "SecureChat";
            this.StartPosition       = System.Windows.Forms.FormStartPosition.CenterScreen;

            this.Controls.Add(this.pnlRight);
            this.Controls.Add(this.pnlDivider);
            this.Controls.Add(this.pnlLeft);

            this.Load        += new System.EventHandler(this.MainForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
        }

        #endregion

        private System.Windows.Forms.Panel            pnlLeft          = null!;
        private System.Windows.Forms.Panel            pnlHeader        = null!;
        private System.Windows.Forms.Panel            pnlAvatar        = null!;
        private System.Windows.Forms.Label            lblDisplayName   = null!;
        private System.Windows.Forms.Label            lblUsername      = null!;
        private System.Windows.Forms.Button           btnAddContact    = null!;
        private System.Windows.Forms.Button           btnSettings      = null!;
        private System.Windows.Forms.Panel            pnlSearch        = null!;
        private System.Windows.Forms.TextBox          txtSearch        = null!;
        private System.Windows.Forms.Label            lblCtrlK         = null!;
        private System.Windows.Forms.Panel            pnlFilterTabs    = null!;
        private System.Windows.Forms.Button           btnNewRoom       = null!;
        private SecureChat.Controls.RoomListPanel      flpRoomList      = null!;
        private System.Windows.Forms.StatusStrip      statusBar        = null!;
        private System.Windows.Forms.ToolStripStatusLabel lblConnectionStatus = null!;
        private System.Windows.Forms.ToolStripStatusLabel lblFingerprint      = null!;
        private System.Windows.Forms.Panel            pnlDivider       = null!;
        private System.Windows.Forms.Panel            pnlRight         = null!;
        private System.Windows.Forms.Panel            pnlEmptyState    = null!;
        private System.Windows.Forms.Panel            pnlShield        = null!;
        private System.Windows.Forms.Label            lblWelcomeTitle  = null!;
        private System.Windows.Forms.Label            lblWelcomeSubtitle = null!;
        private System.Windows.Forms.Button           btnCreateRoom      = null!;
        private System.Windows.Forms.Button           btnAddContactRight = null!;
        private System.Windows.Forms.Panel            pnlNotifBtn        = null!;
        private System.Windows.Forms.Panel            pnlTip           = null!;
        private System.Windows.Forms.Label            lblTip           = null!;
    }
}
