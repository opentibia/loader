namespace otloader
{
    partial class FormOtloader
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
            this.btnLoad = new System.Windows.Forms.Button();
            this.editServer = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.editPort = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.checkBoxAutoAdd = new System.Windows.Forms.CheckBox();
            this.listBoxServers = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // btnLoad
            // 
            this.btnLoad.Location = new System.Drawing.Point(168, 50);
            this.btnLoad.Name = "btnLoad";
            this.btnLoad.Size = new System.Drawing.Size(89, 23);
            this.btnLoad.TabIndex = 2;
            this.btnLoad.Text = "Load";
            this.btnLoad.UseVisualStyleBackColor = true;
            this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
            // 
            // editServer
            // 
            this.editServer.Location = new System.Drawing.Point(59, 24);
            this.editServer.Name = "editServer";
            this.editServer.Size = new System.Drawing.Size(198, 20);
            this.editServer.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(41, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Server:";
            // 
            // editPort
            // 
            this.editPort.Location = new System.Drawing.Point(59, 50);
            this.editPort.Name = "editPort";
            this.editPort.Size = new System.Drawing.Size(56, 20);
            this.editPort.TabIndex = 1;
            this.editPort.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.editPort_KeyPress);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(24, 50);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Port:";
            // 
            // checkBoxAutoAdd
            // 
            this.checkBoxAutoAdd.AutoSize = true;
            this.checkBoxAutoAdd.Location = new System.Drawing.Point(138, 79);
            this.checkBoxAutoAdd.Name = "checkBoxAutoAdd";
            this.checkBoxAutoAdd.Size = new System.Drawing.Size(128, 17);
            this.checkBoxAutoAdd.TabIndex = 3;
            this.checkBoxAutoAdd.Text = "auto add new servers";
            this.checkBoxAutoAdd.UseVisualStyleBackColor = true;
            // 
            // listBoxServers
            // 
            this.listBoxServers.FormattingEnabled = true;
            this.listBoxServers.Location = new System.Drawing.Point(12, 102);
            this.listBoxServers.Name = "listBoxServers";
            this.listBoxServers.ScrollAlwaysVisible = true;
            this.listBoxServers.Size = new System.Drawing.Size(245, 95);
            this.listBoxServers.TabIndex = 4;
            this.listBoxServers.SelectedValueChanged += new System.EventHandler(this.listBoxServers_SelectedValueChanged);
            this.listBoxServers.KeyUp += new System.Windows.Forms.KeyEventHandler(this.listBoxServers_KeyUp);
            // 
            // FormOtloader
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(269, 214);
            this.Controls.Add(this.listBoxServers);
            this.Controls.Add(this.checkBoxAutoAdd);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.editPort);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.editServer);
            this.Controls.Add(this.btnLoad);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "FormOtloader";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "otloader";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormOtloader_FormClosing);
            this.Load += new System.EventHandler(this.FormOtloader_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.TextBox editServer;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox editPort;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox checkBoxAutoAdd;
        private System.Windows.Forms.ListBox listBoxServers;
    }
}

