using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace otloader
{
	enum PatchResult
	{
		CouldNotFindClient,
		CouldNotPatchRSA,
		CouldNotPatchServerList,
		Success
	};

	public partial class FormOtloader : Form
	{
		private otloader.Settings settings = new otloader.Settings();

		private System.Windows.Forms.Timer timer = new Timer();
		private bool closing = true;
		private Rectangle bounds;

		private string prevPatchedServer;
		private bool isClientPatched = false;

		private List<string> clientServerList;
		private List<string> clientRSAKeys;
		private string otservKey;

		private List<Server> storedServers;
		public FormOtloader()
		{
			InitializeComponent();
		}

		private void FormOtloader_Load(object sender, EventArgs e)
		{
			if (Properties.Settings.Default.Location.X != 0)
			{
				base.Location = Properties.Settings.Default.Location;
			}

			System.IO.Directory.SetCurrentDirectory(Application.StartupPath);
			if (!System.IO.File.Exists(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), Settings.SettingFilename)))
			{
				if (Properties.Settings.Default.SettingPath.Length > 0)
				{
					System.IO.Directory.SetCurrentDirectory(Properties.Settings.Default.SettingPath);
				}
			}

			if (!settings.Load())
			{
				FolderBrowserDialog dlg = new FolderBrowserDialog();
				dlg.SelectedPath = Application.StartupPath;
				dlg.ShowNewFolderButton = false;
				dlg.Description = "Please locate the settings.xml file, it should be located in the same directory as the program.";

				do
				{
					DialogResult res = dlg.ShowDialog();
					if (res != DialogResult.OK)
					{
						Application.Exit();
					}

					System.IO.Directory.SetCurrentDirectory(dlg.SelectedPath);
				}
				while (!settings.Load());
				Properties.Settings.Default.SettingPath = dlg.SelectedPath;
			}

			clientServerList = settings.GetClientServerList();
			clientRSAKeys = settings.GetRSAKeys();
			otservKey = settings.OtservRSAKey;

			checkBoxAutoAdd.Checked = settings.AutoAddServer;
			storedServers = settings.GetServerList();
			UpdateServerList();

			bounds = this.Bounds;
			if (listBoxServers.Items.Count > 0)
			{
				listBoxServers.SelectedIndex = 0;
			}
		}

		private void FormOtloader_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (!closing)
			{
				e.Cancel = true;
				timer.Tick += new EventHandler(timer_Tick);
				timer.Interval = 10;
				timer.Enabled = true;
			}
			else if(e.CloseReason != CloseReason.ApplicationExitCall)
			{
				settings.UpdateServerList(storedServers);
				settings.AutoAddServer = checkBoxAutoAdd.Checked;
				settings.Save();

				Properties.Settings.Default.Location = base.DesktopBounds.Location;
				Properties.Settings.Default.Save();
			}
		}

		private void timer_Tick(object sender, EventArgs e)
		{
			Rectangle workingArea = Screen.GetWorkingArea(this.Bounds);
			this.Bounds = new Rectangle(
				Math.Max(workingArea.Width - this.Width, this.Left + 10),
				Math.Max(workingArea.Height - this.Height, this.Top + 10),
				Math.Max(0, this.Width - 10),
				Math.Max(0, this.Height - 10));

			if (this.Bounds.Bottom >= workingArea.Bottom)
			{
				timer.Tick -= new EventHandler(timer_Tick);
				timer.Enabled = false;

				closing = true;
                this.Close();
            }
        }

		private void UpdateServerList()
		{
			listBoxServers.Items.Clear();
			foreach (Server server in storedServers)
			{
				listBoxServers.Items.Add(server.name + ":" + server.port);
			}
		}

		private PatchResult patchClient()
		{
			if (!Utils.IsClientRunning()) 
			{
				return PatchResult.CouldNotFindClient;
			}

			bool patchedClientRSA = false;
			foreach (string RSAKey in clientRSAKeys)
			{
				if (Utils.PatchClientRSAKey(RSAKey, otservKey))
				{
					patchedClientRSA = true;
					break;
				}
			}

			if (!patchedClientRSA && !isClientPatched)
			{
				return PatchResult.CouldNotPatchRSA;
			}

			if(isClientPatched && !patchedClientRSA)
			{
				if (Utils.PatchClientServer(prevPatchedServer, editServer.Text, Convert.ToUInt16(editPort.Text)))
				{
					return PatchResult.Success;
				}
			}

			bool patchedClientServer = false;
			foreach (string server in clientServerList)
			{
				if (Utils.PatchClientServer(server, editServer.Text, Convert.ToUInt16(editPort.Text)))
				{
					patchedClientServer = true;
				}
			}

			if (!patchedClientServer)
			{
				return PatchResult.CouldNotPatchServerList;
			}

			return PatchResult.Success;
		}

		private void btnLoad_Click(object sender, EventArgs e)
		{
			PatchResult result = patchClient();
			switch (result)
			{
				case PatchResult.CouldNotFindClient: MessageBox.Show("Could not find client!"); break;
				case PatchResult.CouldNotPatchRSA: MessageBox.Show("Could not patch RSA key!"); break;
				case PatchResult.CouldNotPatchServerList: MessageBox.Show("Could not patch server list!"); break;
				case PatchResult.Success:
					{
						if (checkBoxAutoAdd.Checked)
						{
							btnFavorite_Click(sender, e);
						}

						prevPatchedServer = editServer.Text;
						isClientPatched = true;
						break;
					}
				default: break;
			}


		}

		private void btnFavorite_Click(object sender, EventArgs e)
		{
			bool isStored = false;
			foreach (Server server in storedServers)
			{
				if (server.name == editServer.Text && server.port.ToString() == editPort.Text)
				{
					isStored = true;
					break;
				}
			}

			if (!isStored)
			{
				Server server = new Server();
				server.name = editServer.Text;
				server.port = Convert.ToUInt16(editPort.Text);

				storedServers.Add(server);
				UpdateServerList();
			}
		}

		private void editPort_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!Char.IsControl(e.KeyChar))
			{
				if (Char.IsDigit(e.KeyChar))
				{
					try
					{
						UInt16.Parse(editPort.Text + e.KeyChar);
					}
					catch
					{
						//TODO: some tooltip in here
						e.Handled = true;
					}
				}
				else
				{
					e.Handled = true;
				}
			}
		}

		private void listBoxServers_SelectedValueChanged(object sender, EventArgs e)
		{
			if (listBoxServers.SelectedIndex != -1 &&
				listBoxServers.SelectedIndex < storedServers.Count)
			{
				Server server = storedServers[listBoxServers.SelectedIndex];
				editServer.Text = server.name;
				editPort.Text = server.port.ToString();
			}
		}

		private void listBoxServers_KeyUp(object sender, KeyEventArgs e)
		{
			if (listBoxServers.SelectedIndex != -1)
			{
				switch ((Keys)e.KeyValue)
				{
					case Keys.Delete:
						{
							Server server = storedServers[listBoxServers.SelectedIndex];
							storedServers.RemoveAt(listBoxServers.SelectedIndex);
							UpdateServerList();

							editServer.Text = "";
							editPort.Text = "";
							break;
						}

					case Keys.Enter:
						{
							//TODO
							break;
						}

					default: break;
				}
			}
		}

		private void FormOtloader_SizeChanged(object sender, EventArgs e)
		{
			if (this.WindowState == FormWindowState.Minimized)
			{
				this.ShowInTaskbar = false;
				this.notifyIcon.Visible = true;

				closing = false;
				this.Close();
			}
		}

		private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				this.ShowInTaskbar = true;
				this.notifyIcon.Visible = false;

				//remove the timer event
				timer.Tick -= new EventHandler(timer_Tick);
				timer.Enabled = false;

				this.Bounds = bounds; //bring original bounds
				closing = true;

				this.WindowState = FormWindowState.Normal;
				this.Activate();
			}
			else if (e.Button == MouseButtons.Right)
			{
				//remove the timer event
				timer.Tick -= new EventHandler(timer_Tick);
				timer.Enabled = false;

				this.Bounds = bounds; //bring original bounds
				closing = true;

				this.WindowState = FormWindowState.Normal; //need the window back to save a proper location
				this.Close();
			}
		}
	}
}
