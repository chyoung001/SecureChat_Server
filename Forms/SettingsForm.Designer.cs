#nullable enable
namespace SecureChat.Forms
{
    partial class SettingsForm
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
            _lblPlaceholder = new System.Windows.Forms.Label();
            SuspendLayout();

            // _lblPlaceholder
            _lblPlaceholder.Text      = "설정 (추후 구현)";
            _lblPlaceholder.Font      = new System.Drawing.Font("맑은 고딕", 11F);
            _lblPlaceholder.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            _lblPlaceholder.Dock      = System.Windows.Forms.DockStyle.Fill;
            _lblPlaceholder.ForeColor = System.Drawing.Color.FromArgb(153, 153, 153);

            // SettingsForm
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode       = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize          = new System.Drawing.Size(480, 360);
            BackColor           = System.Drawing.Color.White;
            FormBorderStyle     = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox         = false;
            MinimizeBox         = false;
            StartPosition       = System.Windows.Forms.FormStartPosition.CenterParent;
            Name                = "SettingsForm";
            Text                = "설정";
            Controls.Add(_lblPlaceholder);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label _lblPlaceholder = null!;
    }
}
