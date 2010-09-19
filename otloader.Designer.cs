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
			this.components = new System.ComponentModel.Container();
			this.btnLoad = new System.Windows.Forms.Button();
			this.editServer = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.editPort = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.checkBoxAutoAdd = new System.Windows.Forms.CheckBox();
			this.listBoxServers = new System.Windows.Forms.ListBox();
			this.btnFavorite = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
			this.checkBoxMultiClientPatch = new System.Windows.Forms.CheckBox();
			this.checkBoxRSAPatch = new System.Windows.Forms.CheckBox();
			this.statusStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnLoad
			// 
			this.btnLoad.Location = new System.Drawing.Point(131, 38);
			this.btnLoad.Name = "btnLoad";
			this.btnLoad.Size = new System.Drawing.Size(60, 20);
			this.btnLoad.TabIndex = 2;
			this.btnLoad.Text = "Patch";
			this.btnLoad.UseVisualStyleBackColor = true;
			this.btnLoad.Click += new System.EventHandler(this.btnLoad_Click);
			// 
			// editServer
			// 
			this.editServer.Location = new System.Drawing.Point(59, 12);
			this.editServer.MaxLength = 255;
			this.editServer.Name = "editServer";
			this.editServer.Size = new System.Drawing.Size(198, 20);
			this.editServer.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(8, 15);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(41, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Server:";
			// 
			// editPort
			// 
			this.editPort.Location = new System.Drawing.Point(59, 38);
			this.editPort.MaxLength = 5;
			this.editPort.Name = "editPort";
			this.editPort.Size = new System.Drawing.Size(56, 20);
			this.editPort.TabIndex = 1;
			this.editPort.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.editPort_KeyPress);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(20, 40);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(29, 13);
			this.label2.TabIndex = 0;
			this.label2.Text = "Port:";
			// 
			// checkBoxAutoAdd
			// 
			this.checkBoxAutoAdd.AutoSize = true;
			this.checkBoxAutoAdd.Location = new System.Drawing.Point(12, 64);
			this.checkBoxAutoAdd.Name = "checkBoxAutoAdd";
			this.checkBoxAutoAdd.Size = new System.Drawing.Size(107, 17);
			this.checkBoxAutoAdd.TabIndex = 3;
			this.checkBoxAutoAdd.Text = "Auto add favorite";
			this.checkBoxAutoAdd.UseVisualStyleBackColor = true;
			// 
			// listBoxServers
			// 
			this.listBoxServers.FormattingEnabled = true;
			this.listBoxServers.Location = new System.Drawing.Point(11, 105);
			this.listBoxServers.Name = "listBoxServers";
			this.listBoxServers.Size = new System.Drawing.Size(246, 108);
			this.listBoxServers.TabIndex = 4;
			this.listBoxServers.SelectedValueChanged += new System.EventHandler(this.listBoxServers_SelectedValueChanged);
			this.listBoxServers.KeyUp += new System.Windows.Forms.KeyEventHandler(this.listBoxServers_KeyUp);
			// 
			// btnFavorite
			// 
			this.btnFavorite.Location = new System.Drawing.Point(197, 38);
			this.btnFavorite.Name = "btnFavorite";
			this.btnFavorite.Size = new System.Drawing.Size(60, 20);
			this.btnFavorite.TabIndex = 5;
			this.btnFavorite.Text = "Favorite";
			this.btnFavorite.UseVisualStyleBackColor = true;
			this.btnFavorite.Click += new System.EventHandler(this.btnFavorite_Click);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(8, 89);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(53, 13);
			this.label3.TabIndex = 6;
			this.label3.Text = "Favorites:";
			// 
			// notifyIcon
			// 
			this.notifyIcon.Text = "otloader";
			this.notifyIcon.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon_MouseClick);
			// 
			// statusStrip
			// 
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
			this.statusStrip.Location = new System.Drawing.Point(0, 217);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Size = new System.Drawing.Size(269, 22);
			this.statusStrip.SizingGrip = false;
			this.statusStrip.TabIndex = 7;
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			this.toolStripStatusLabel1.Size = new System.Drawing.Size(0, 17);
			this.toolStripStatusLabel1.TextChanged += new System.EventHandler(this.toolStripStatusLabel1_TextChanged);
			// 
			// checkBoxMultiClientPatch
			// 
			this.checkBoxMultiClientPatch.AutoSize = true;
			this.checkBoxMultiClientPatch.Location = new System.Drawing.Point(139, 82);
			this.checkBoxMultiClientPatch.Name = "checkBoxMultiClientPatch";
			this.checkBoxMultiClientPatch.Size = new System.Drawing.Size(106, 17);
			this.checkBoxMultiClientPatch.TabIndex = 8;
			this.checkBoxMultiClientPatch.Text = "Patch multi client";
			this.checkBoxMultiClientPatch.UseVisualStyleBackColor = true;
			// 
			// checkBoxRSAPatch
			// 
			this.checkBoxRSAPatch.AutoSize = true;
			this.checkBoxRSAPatch.Location = new System.Drawing.Point(139, 64);
			this.checkBoxRSAPatch.Name = "checkBoxRSAPatch";
			this.checkBoxRSAPatch.Size = new System.Drawing.Size(118, 17);
			this.checkBoxRSAPatch.TabIndex = 9;
			this.checkBoxRSAPatch.Text = "Patch RSA (< v7.7)";
			this.checkBoxRSAPatch.UseVisualStyleBackColor = true;
			// 
			// FormOtloader
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(269, 239);
			this.Controls.Add(this.checkBoxRSAPatch);
			this.Controls.Add(this.checkBoxMultiClientPatch);
			this.Controls.Add(this.statusStrip);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.btnFavorite);
			this.Controls.Add(this.listBoxServers);
			this.Controls.Add(this.checkBoxAutoAdd);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.editPort);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.editServer);
			this.Controls.Add(this.btnLoad);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "FormOtloader";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "otloader";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormOtloader_FormClosing);
			this.Load += new System.EventHandler(this.FormOtloader_Load);
			this.SizeChanged += new System.EventHandler(this.FormOtloader_SizeChanged);
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
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
		private System.Windows.Forms.Button btnFavorite;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.NotifyIcon notifyIcon;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.StatusStrip statusStrip;
		private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
		private System.Windows.Forms.CheckBox checkBoxMultiClientPatch;
		private System.Windows.Forms.CheckBox checkBoxRSAPatch;
	}
}

