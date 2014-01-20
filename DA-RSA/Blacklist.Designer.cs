namespace DA_RSA
{
    partial class Blacklist
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
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.button_hin = new System.Windows.Forms.Button();
            this.button_sp = new System.Windows.Forms.Button();
            this.button_abb = new System.Windows.Forms.Button();
            this.button_del = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(12, 64);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(155, 238);
            this.listBox1.TabIndex = 0;
            // 
            // button_hin
            // 
            this.button_hin.Location = new System.Drawing.Point(11, 35);
            this.button_hin.Name = "button_hin";
            this.button_hin.Size = new System.Drawing.Size(75, 23);
            this.button_hin.TabIndex = 1;
            this.button_hin.Text = "hinzufügen";
            this.button_hin.UseVisualStyleBackColor = true;
            this.button_hin.Click += new System.EventHandler(this.button_hin_Click);
            // 
            // button_sp
            // 
            this.button_sp.Location = new System.Drawing.Point(11, 305);
            this.button_sp.Name = "button_sp";
            this.button_sp.Size = new System.Drawing.Size(75, 23);
            this.button_sp.TabIndex = 2;
            this.button_sp.Text = "speichern";
            this.button_sp.UseVisualStyleBackColor = true;
            this.button_sp.Click += new System.EventHandler(this.button_sp_Click);
            // 
            // button_abb
            // 
            this.button_abb.Location = new System.Drawing.Point(92, 305);
            this.button_abb.Name = "button_abb";
            this.button_abb.Size = new System.Drawing.Size(75, 23);
            this.button_abb.TabIndex = 3;
            this.button_abb.Text = "abbrechen";
            this.button_abb.UseVisualStyleBackColor = true;
            this.button_abb.Click += new System.EventHandler(this.button_abb_Click);
            // 
            // button_del
            // 
            this.button_del.Location = new System.Drawing.Point(92, 35);
            this.button_del.Name = "button_del";
            this.button_del.Size = new System.Drawing.Size(75, 23);
            this.button_del.TabIndex = 4;
            this.button_del.Text = "löschen";
            this.button_del.UseVisualStyleBackColor = true;
            this.button_del.Click += new System.EventHandler(this.button_del_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(11, 9);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(156, 20);
            this.textBox1.TabIndex = 5;
            // 
            // Blacklist
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(188, 335);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.button_del);
            this.Controls.Add(this.button_abb);
            this.Controls.Add(this.button_sp);
            this.Controls.Add(this.button_hin);
            this.Controls.Add(this.listBox1);
            this.Name = "Blacklist";
            this.Text = "Blacklist";
            this.Load += new System.EventHandler(this.Blacklist_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Button button_hin;
        private System.Windows.Forms.Button button_sp;
        private System.Windows.Forms.Button button_abb;
        private System.Windows.Forms.Button button_del;
        private System.Windows.Forms.TextBox textBox1;
    }
}