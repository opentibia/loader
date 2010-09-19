using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace otloader
{
	public partial class FormOtloader : Form
	{
		private otloader.Settings settings = new otloader.Settings();
		private Timer timer = new Timer();

		private Server prevPatchedServer = new Server("", 0);
		private UInt32 prevPatchedClient;
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
			checkBoxMultiClientPatch.Checked = settings.MultiClientPatch;

			storedServers = settings.GetServerList();
			UpdateServerList();

			if (listBoxServers.Items.Count > 0)
			{
				listBoxServers.SelectedIndex = 0;
			}
		}

		private void FormOtloader_FormClosing(object sender, FormClosingEventArgs e)
		{
			if(e.CloseReason != CloseReason.ApplicationExitCall)
			{
				settings.UpdateServerList(storedServers);
				settings.AutoAddServer = checkBoxAutoAdd.Checked;
				settings.MultiClientPatch = checkBoxMultiClientPatch.Checked;
				settings.Save();

				Properties.Settings.Default.Location = base.DesktopBounds.Location;
				Properties.Settings.Default.Save();
			}
		}

		private void btnLoad_Click(object sender, EventArgs e)
		{
			PatchResult result = PatchClient();
			switch (result)
			{
				case PatchResult.CouldNotFindClient: toolStripStatusLabel1.Text = "Could not find client!"; break;
				case PatchResult.CouldNotFindRSA: toolStripStatusLabel1.Text = "Could not find RSA key!"; break;
				case PatchResult.CouldNotPatchRSA: toolStripStatusLabel1.Text = "Could not patch RSA key!"; break;
				case PatchResult.CouldNotPatchServerList: toolStripStatusLabel1.Text = "Could not patch server list!"; break;
				case PatchResult.CouldNotPatchServer: toolStripStatusLabel1.Text = "Could not patch new server!"; break;
				case PatchResult.AlreadyPatched: toolStripStatusLabel1.Text = "Client already patched!"; break;
				case PatchResult.AlreadyPatchedNotOwned:
					{
						toolStripStatusLabel1.Text = "Client already patched by another instance!";
						toolTip.Show("To use otloader you must restart the client.", btnLoad, 5000);
						break;
					}
				case PatchResult.Success:
					{
						toolStripStatusLabel1.Text = "Client patched.";
						if (checkBoxAutoAdd.Checked)
						{
							AddFavorite(true);
						}

						isClientPatched = true;
						break;
					}
				default: break;
			}


		}

		private void btnFavorite_Click(object sender, EventArgs e)
		{
			AddFavorite(false);
		}

		private void editPort_KeyPress(object sender, KeyPressEventArgs e)
		{
			toolTip.Hide(editPort);
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
						toolTip.Show("Value must be a valid port number.", editPort, 2000);
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
							storedServers.RemoveAt(listBoxServers.SelectedIndex);
							UpdateServerList();

							editServer.Text = editPort.Text = "";
							toolStripStatusLabel1.Text = "Favorite removed.";
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
			/*
			if (this.WindowState == FormWindowState.Minimized)
			{
				this.ShowInTaskbar = false;
				this.notifyIcon.Visible = true;
				this.Opacity = 0.0;
			}
			*/
		}

		private void notifyIcon_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				this.ShowInTaskbar = true;
				this.notifyIcon.Visible = false;

				this.WindowState = FormWindowState.Normal;
				this.Opacity = 1.0;
			}
			else if (e.Button == MouseButtons.Right)
			{
				this.WindowState = FormWindowState.Normal; //window has to be in normal state to save a proper location
				this.Close();
			}
		}

		private void toolStripStatusLabel1_TextChanged(object sender, EventArgs e)
		{
			if(timer.Enabled)
				timer.Stop();

			Wait(timer, 10000, (o, a) => toolStripStatusLabel1.Text = "");
		}

		private void UpdateServerList()
		{
			listBoxServers.Items.Clear();
			foreach (Server server in storedServers)
			{
				listBoxServers.Items.Add(server.name + ":" + server.port);
			}
		}

		private PatchResult PatchClient()
		{
			UInt32 client = Utils.GetClientProcessId();
			if (client == 0) 
			{
				return PatchResult.CouldNotFindClient;
			}

			if (client != prevPatchedClient)
			{
				isClientPatched = false;
			}

			UInt16 port = UInt16.Parse(editPort.Text);
			if (!isClientPatched)
			{
				//MC patch
				if (checkBoxMultiClientPatch.Checked)
				{
					Utils.PatchMultiClient();
				}

				PatchResult patchResult = PatchResult.Dummy;
				foreach (string RSAKey in clientRSAKeys)
				{
					if ((patchResult = Utils.PatchClientRSAKey(RSAKey, otservKey)) == PatchResult.Success)
					{
						break;
					}
				}

				if (patchResult == PatchResult.AlreadyPatched)
				{
					return PatchResult.AlreadyPatchedNotOwned;
				}
				else if (patchResult != PatchResult.Success)
				{
					return PatchResult.CouldNotPatchRSA;
				}

				bool patched = false;
				foreach (string server in clientServerList)
				{
					if(Utils.PatchClientServer(server, editServer.Text, port) == PatchResult.Success)
					{
						patched = true;
					}
				}

				if (!patched)
				{
					return PatchResult.CouldNotPatchServerList;
				}
			}
			else if(editServer.Text == prevPatchedServer.name && port == prevPatchedServer.port)
			{
				return PatchResult.AlreadyPatched;
			}
			else if(Utils.PatchClientServer(prevPatchedServer.name, editServer.Text, port) != PatchResult.Success)
			{
				return PatchResult.CouldNotPatchServer;
			}

			prevPatchedServer.name = editServer.Text;
			prevPatchedServer.port = port;
			prevPatchedClient = client;

			isClientPatched = true;
			return PatchResult.Success;
		}

		private void AddFavorite(bool patching)
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
				Server server = new Server(editServer.Text, UInt16.Parse(editPort.Text));
				storedServers.Add(server);

				UpdateServerList();
				if(!patching)
				{
					toolStripStatusLabel1.Text = "Favorite added.";
				}
			}
			else if(!patching)
			{
				toolStripStatusLabel1.Text = "Server is already on favorite list!";
			}
		}

		static void Wait(Timer t, Int32 interval, EventHandler action)
		{
			t.Interval = interval;
			t.Tick += new EventHandler((o, e) => t.Enabled = false);
			t.Tick += action;
			t.Enabled = true;
		}
	}
}
