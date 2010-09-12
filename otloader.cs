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


            clientServerList = settings.GetClientServerList();
            clientRSAKeys = settings.GetRSAKeys();
            otservKey = settings.GetOtservRSAKey();
            checkBoxAutoAdd.Checked = settings.GetAutoAddServer();
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
            settings.UpdateAutoSaveServer(checkBoxAutoAdd.Checked);
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

        private bool patchClient()
        {
            if (!Utils.IsClientRunning())
            {
                MessageBox.Show("Could not find client!");
                return false;
            }

            bool isPatched = false;
            foreach (string RSAKey in clientRSAKeys)
            {
                if (Utils.PatchClientRSAKey(RSAKey, otservKey))
                {
                    isPatched = true;
                    break;
                }
            }

            if (!isPatched)
            {
                MessageBox.Show("Could not replace RSA key!");
                return false;
            }

            isPatched = false;
            foreach (string server in clientServerList)
            {
                if (Utils.PatchClientServer(server, editServer.Text, Convert.ToInt16(editPort.Text)))
                {
                    isPatched = true;
                }
            }

            if (!isPatched)
            {
                MessageBox.Show("Could not patch client!");
                return false;
            }

            return true;
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

            patchClient();
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
            if (listBoxServers.SelectedIndex != -1)
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
