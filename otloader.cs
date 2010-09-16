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

		string prevPatchedServer = "";
		bool isClientPatched = false;

		List<string> clientServerList;
		List<string> clientRSAKeys;

		string otservKey;

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
			if(!System.IO.File.Exists(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), Settings.SettingFilename)))
			{
				if(Properties.Settings.Default.SettingPath.Length > 0)
				{
					System.IO.Directory.SetCurrentDirectory(Properties.Settings.Default.SettingPath);
				}
			}

			if(!settings.Load()){
				FolderBrowserDialog dlg = new FolderBrowserDialog();
				dlg.SelectedPath = Application.StartupPath;
				dlg.ShowNewFolderButton = false;
				dlg.Description = "Please locate the settings.xml, it should be located in the same folder as the program.";

				do{
					DialogResult res = dlg.ShowDialog();
					if(res != DialogResult.OK){
						Application.Exit();
					}

					System.IO.Directory.SetCurrentDirectory(dlg.SelectedPath);
				}while(!settings.Load());

				Properties.Settings.Default.SettingPath = dlg.SelectedPath;
			}

			clientServerList = settings.GetClientServerList();
			clientRSAKeys = settings.GetRSAKeys();
			otservKey = settings.OtservRSAKey;
			checkBoxAutoAdd.Checked = settings.AutoAddServer;
			storedServers = settings.GetServerList();
			UpdateServerList();

			if (listBoxServers.Items.Count > 0)
			{
				listBoxServers.SelectedIndex = 0;
			}
		}

		private void FormOtloader_FormClosing(object sender, FormClosingEventArgs e)
		{
			settings.UpdateServerList(storedServers);
			settings.AutoAddServer = checkBoxAutoAdd.Checked;
			settings.Save();

			Properties.Settings.Default.Location = base.DesktopBounds.Location;
			Properties.Settings.Default.Save();
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

			if (!isClientPatched)
			{
				bool patchedClientRSA = false;
				foreach (string RSAKey in clientRSAKeys)
				{
					if (Utils.PatchClientRSAKey(RSAKey, otservKey))
					{
						patchedClientRSA = true;
						break;
					}
				}

				if (!patchedClientRSA)
				{
					return PatchResult.CouldNotPatchRSA;
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
			}
			else
			{
				//Client is already patched, so just replace the previous server host and port
				if (Utils.PatchClientServer(prevPatchedServer, editServer.Text, Convert.ToUInt16(editPort.Text)))
				{
					return PatchResult.Success;
				}
			}

			return PatchResult.Success;
		}

		private void btnLoad_Click(object sender, EventArgs e)
		{
			if (checkBoxAutoAdd.Checked)
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

				if(!isStored)
				{
					Server server = new Server();
					server.name = editServer.Text;
					server.port = Convert.ToUInt16(editPort.Text);
					storedServers.Add(server);
					UpdateServerList();
				}
			}

			PatchResult result = patchClient();

			switch (result)
			{
				case PatchResult.CouldNotFindClient: MessageBox.Show("Could not find client!"); break;
				case PatchResult.CouldNotPatchRSA: MessageBox.Show("Could not patch RSA key!"); break;
				case PatchResult.CouldNotPatchServerList: MessageBox.Show("Could not patch server list!"); break;
				case PatchResult.Success:
					{
						prevPatchedServer = editServer.Text;
						isClientPatched = true;
						break;
					}
				default: break;
			}
		}

		private void editPort_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!Char.IsDigit(e.KeyChar) && !Char.IsControl(e.KeyChar))
			{
				e.Handled = true;
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
			if ((Keys)e.KeyValue == Keys.Delete)
			{
				if (listBoxServers.SelectedIndex != -1)
				{
					storedServers.RemoveAt(listBoxServers.SelectedIndex);
					UpdateServerList();
				}
			}
		}
	}
}
