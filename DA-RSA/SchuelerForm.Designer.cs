namespace DA_RSA
{
    partial class SchuelerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SchuelerForm));
            this.notifyIcon_rsa = new System.Windows.Forms.NotifyIcon(this.components);
            this.SuspendLayout();
            // 
            // notifyIcon_rsa
            // 
            this.notifyIcon_rsa.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon_rsa.Icon")));
            this.notifyIcon_rsa.Text = "RSA";
            this.notifyIcon_rsa.Visible = true;
            this.notifyIcon_rsa.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // SchuelerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Name = "SchuelerForm";
            this.Text = "SchuelerForm";
            this.Load += new System.EventHandler(this.SchuelerForm_Load);
            this.Shown += new System.EventHandler(this.SchuelerForm_Shown);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon_rsa;
    }
}