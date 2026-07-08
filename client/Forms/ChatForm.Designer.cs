#nullable enable
namespace SecureChat.Forms
{
    partial class ChatForm
    {
        private System.ComponentModel.IContainer? components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.btnBack = new System.Windows.Forms.Button();
            this.lblRoomName = new System.Windows.Forms.Label();
            this.btnSecurityInfo = new System.Windows.Forms.Button();
            this.btnRoomSettings = new System.Windows.Forms.Button();
            this.flpMessages = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlInput = new System.Windows.Forms.Panel();
            this.pnlInputTop = new System.Windows.Forms.Panel();
            this.lblTtl = new System.Windows.Forms.Label();
            this.cmbTtl = new System.Windows.Forms.ComboBox();
            this.btnAttach = new System.Windows.Forms.Button();
            this.txtInput = new System.Windows.Forms.TextBox();
            this.pnlInputBottom = new System.Windows.Forms.Panel();
            this.btnSend = new System.Windows.Forms.Button();
            this.pnlHeader.SuspendLayout();
            this.pnlInput.SuspendLayout();
            this.pnlInputTop.SuspendLayout();
            this.pnlInputBottom.SuspendLayout();
            this.SuspendLayout();

            // pnlHeader — white with hairline bottom border
            this.pnlHeader.Controls.Add(this.btnBack);
            this.pnlHeader.Controls.Add(this.lblRoomName);
            this.pnlHeader.Controls.Add(this.btnSecurityInfo);
            this.pnlHeader.Controls.Add(this.btnRoomSettings);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Height = 50;
            this.pnlHeader.BackColor = System.Drawing.Color.White;
            this.pnlHeader.Paint += new System.Windows.Forms.PaintEventHandler(this.PnlHeader_Paint);

            // btnBack — ghost, black text
            this.btnBack.Text = "<";
            this.btnBack.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.btnBack.ForeColor = System.Drawing.Color.Black;
            this.btnBack.BackColor = System.Drawing.Color.White;
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.FlatAppearance.BorderSize = 0;
            this.btnBack.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.btnBack.Location = new System.Drawing.Point(4, 6);
            this.btnBack.Size = new System.Drawing.Size(38, 38);
            this.btnBack.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);

            // lblRoomName — black heading
            this.lblRoomName.Text = "방 이름";
            this.lblRoomName.Font = new System.Drawing.Font("맑은 고딕", 11F, System.Drawing.FontStyle.Bold);
            this.lblRoomName.ForeColor = System.Drawing.Color.Black;
            this.lblRoomName.Location = new System.Drawing.Point(50, 14);
            this.lblRoomName.AutoSize = true;

            // btnSecurityInfo — ghost, #999 text
            this.btnSecurityInfo.Text = "보안";
            this.btnSecurityInfo.Font = new System.Drawing.Font("맑은 고딕", 8.5F);
            this.btnSecurityInfo.ForeColor = System.Drawing.Color.FromArgb(153, 153, 153);
            this.btnSecurityInfo.BackColor = System.Drawing.Color.White;
            this.btnSecurityInfo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSecurityInfo.FlatAppearance.BorderSize = 0;
            this.btnSecurityInfo.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.btnSecurityInfo.Location = new System.Drawing.Point(420, 6);
            this.btnSecurityInfo.Size = new System.Drawing.Size(36, 38);
            this.btnSecurityInfo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSecurityInfo.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;

            // btnRoomSettings — ghost, #999 text
            this.btnRoomSettings.Text = "=";
            this.btnRoomSettings.Font = new System.Drawing.Font("맑은 고딕", 13F);
            this.btnRoomSettings.ForeColor = System.Drawing.Color.FromArgb(153, 153, 153);
            this.btnRoomSettings.BackColor = System.Drawing.Color.White;
            this.btnRoomSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRoomSettings.FlatAppearance.BorderSize = 0;
            this.btnRoomSettings.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.btnRoomSettings.Location = new System.Drawing.Point(458, 6);
            this.btnRoomSettings.Size = new System.Drawing.Size(36, 38);
            this.btnRoomSettings.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRoomSettings.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;

            // flpMessages — white canvas
            this.flpMessages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpMessages.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpMessages.WrapContents = false;
            this.flpMessages.AutoScroll = true;
            this.flpMessages.BackColor = System.Drawing.Color.White;
            this.flpMessages.Padding = new System.Windows.Forms.Padding(0, 8, 0, 8);

            // pnlInput — white, hairline top border
            this.pnlInput.Controls.Add(this.txtInput);
            this.pnlInput.Controls.Add(this.pnlInputTop);
            this.pnlInput.Controls.Add(this.pnlInputBottom);
            this.pnlInput.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlInput.Height = 130;
            this.pnlInput.BackColor = System.Drawing.Color.White;
            this.pnlInput.Padding = new System.Windows.Forms.Padding(8);
            this.pnlInput.Paint += new System.Windows.Forms.PaintEventHandler(this.PnlInput_Paint);

            // pnlInputTop
            this.pnlInputTop.Controls.Add(this.lblTtl);
            this.pnlInputTop.Controls.Add(this.cmbTtl);
            this.pnlInputTop.Controls.Add(this.btnAttach);
            this.pnlInputTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlInputTop.Height = 32;
            this.pnlInputTop.BackColor = System.Drawing.Color.White;

            // lblTtl
            this.lblTtl.Text = "TTL";
            this.lblTtl.Font = new System.Drawing.Font("맑은 고딕", 8.5F);
            this.lblTtl.Location = new System.Drawing.Point(4, 8);
            this.lblTtl.AutoSize = true;
            this.lblTtl.ForeColor = System.Drawing.Color.FromArgb(153, 153, 153);

            // cmbTtl
            this.cmbTtl.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTtl.Items.AddRange(new object[] { "끄기", "5초", "30초", "1분", "5분", "1시간" });
            this.cmbTtl.SelectedIndex = 0;
            this.cmbTtl.Location = new System.Drawing.Point(36, 4);
            this.cmbTtl.Size = new System.Drawing.Size(88, 24);
            this.cmbTtl.Font = new System.Drawing.Font("맑은 고딕", 9F);

            // btnAttach — disabled placeholder
            this.btnAttach.Text = "+";
            this.btnAttach.Enabled = false;
            this.btnAttach.Location = new System.Drawing.Point(130, 4);
            this.btnAttach.Size = new System.Drawing.Size(32, 24);
            this.btnAttach.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAttach.FlatAppearance.BorderColor = System.Drawing.Color.FromArgb(221, 221, 221);
            this.btnAttach.ForeColor = System.Drawing.Color.FromArgb(187, 187, 187);

            // txtInput — #dddddd border via FixedSingle
            this.txtInput.Multiline = true;
            this.txtInput.AcceptsReturn = false;
            this.txtInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtInput.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.txtInput.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtInput.BackColor = System.Drawing.Color.White;
            this.txtInput.ForeColor = System.Drawing.Color.FromArgb(34, 34, 34);
            this.txtInput.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtInput_KeyDown);

            // pnlInputBottom
            this.pnlInputBottom.Controls.Add(this.btnSend);
            this.pnlInputBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlInputBottom.Height = 38;
            this.pnlInputBottom.BackColor = System.Drawing.Color.White;

            // btnSend — primary CTA: black bg, white text
            this.btnSend.Text = "전송";
            this.btnSend.Font = new System.Drawing.Font("맑은 고딕", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSend.Size = new System.Drawing.Size(96, 32);
            this.btnSend.Location = new System.Drawing.Point(388, 2);
            this.btnSend.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnSend.BackColor = System.Drawing.Color.Black;
            this.btnSend.ForeColor = System.Drawing.Color.White;
            this.btnSend.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSend.FlatAppearance.BorderSize = 0;
            this.btnSend.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);

            // ChatForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 700);
            this.MinimumSize = new System.Drawing.Size(380, 500);
            this.BackColor = System.Drawing.Color.White;
            this.Controls.Add(this.flpMessages);
            this.Controls.Add(this.pnlInput);
            this.Controls.Add(this.pnlHeader);
            this.Name = "ChatForm";
            this.Text = "ChatForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Load += new System.EventHandler(this.ChatForm_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ChatForm_FormClosing);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.pnlInput.ResumeLayout(false);
            this.pnlInputTop.ResumeLayout(false);
            this.pnlInputTop.PerformLayout();
            this.pnlInputBottom.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader = null!;
        private System.Windows.Forms.Button btnBack = null!;
        private System.Windows.Forms.Label lblRoomName = null!;
        private System.Windows.Forms.Button btnSecurityInfo = null!;
        private System.Windows.Forms.Button btnRoomSettings = null!;
        private System.Windows.Forms.FlowLayoutPanel flpMessages = null!;
        private System.Windows.Forms.Panel pnlInput = null!;
        private System.Windows.Forms.Panel pnlInputTop = null!;
        private System.Windows.Forms.Label lblTtl = null!;
        private System.Windows.Forms.ComboBox cmbTtl = null!;
        private System.Windows.Forms.Button btnAttach = null!;
        private System.Windows.Forms.TextBox txtInput = null!;
        private System.Windows.Forms.Panel pnlInputBottom = null!;
        private System.Windows.Forms.Button btnSend = null!;
    }
}
