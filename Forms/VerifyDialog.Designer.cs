#nullable enable
namespace SecureChat.Forms
{
    partial class VerifyDialog
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
            this.lblPlaceholder = new System.Windows.Forms.Label();
            this.SuspendLayout();

            // lblPlaceholder
            this.lblPlaceholder.Text = "키 검증 다이얼로그\n(Phase 2에서 구현)";
            this.lblPlaceholder.Font = new System.Drawing.Font("맑은 고딕", 11F);
            this.lblPlaceholder.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblPlaceholder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlaceholder.ForeColor = System.Drawing.Color.DimGray;

            // VerifyDialog
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(420, 320);
            this.Controls.Add(this.lblPlaceholder);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Name = "VerifyDialog";
            this.Text = "안전 코드 확인";
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Label lblPlaceholder = null!;
    }
}
