namespace RICommonWinUtility
{
    partial class DataContractPropertyGridForm
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
            this.menuStrip_main = new System.Windows.Forms.MenuStrip();
            this.ToolStripMenuItem_file = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_read = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_write = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.menuStrip_main.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip_main
            // 
            this.menuStrip_main.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_file});
            this.menuStrip_main.Location = new System.Drawing.Point(0, 0);
            this.menuStrip_main.Name = "menuStrip_main";
            this.menuStrip_main.Size = new System.Drawing.Size(428, 26);
            this.menuStrip_main.TabIndex = 0;
            this.menuStrip_main.Text = "menuStrip1";
            // 
            // ToolStripMenuItem_file
            // 
            this.ToolStripMenuItem_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ToolStripMenuItem_read,
            this.ToolStripMenuItem_write});
            this.ToolStripMenuItem_file.Name = "ToolStripMenuItem_file";
            this.ToolStripMenuItem_file.Size = new System.Drawing.Size(68, 22);
            this.ToolStripMenuItem_file.Text = "ファイル";
            // 
            // ToolStripMenuItem_read
            // 
            this.ToolStripMenuItem_read.Name = "ToolStripMenuItem_read";
            this.ToolStripMenuItem_read.Size = new System.Drawing.Size(152, 22);
            this.ToolStripMenuItem_read.Text = "読み込み";
            this.ToolStripMenuItem_read.Click += new System.EventHandler(this.ToolStripMenuItem_read_Click);
            // 
            // ToolStripMenuItem_write
            // 
            this.ToolStripMenuItem_write.Name = "ToolStripMenuItem_write";
            this.ToolStripMenuItem_write.Size = new System.Drawing.Size(152, 22);
            this.ToolStripMenuItem_write.Text = "書き込み";
            this.ToolStripMenuItem_write.Click += new System.EventHandler(this.ToolStripMenuItem_write_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid1.Location = new System.Drawing.Point(0, 26);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(428, 289);
            this.propertyGrid1.TabIndex = 2;
            // 
            // DataContractPropertyGridForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(428, 315);
            this.Controls.Add(this.propertyGrid1);
            this.Controls.Add(this.menuStrip_main);
            this.MainMenuStrip = this.menuStrip_main;
            this.Name = "DataContractPropertyGridForm";
            this.Text = "DataContractPropertyGridForm";
            this.menuStrip_main.ResumeLayout(false);
            this.menuStrip_main.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip_main;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_file;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_read;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_write;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
    }
}