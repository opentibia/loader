using System;
using System.Xml;
using System.Windows.Forms;
using System.Collections.Generic;

namespace otloader
{
    public class Server
    {
        public string name;
        public UInt16 port;
    }

    public class Settings
    {
		public static string SettingFilename = "settings.xml";

		private XmlDocument xmlDocument = new XmlDocument();

        public Settings()
        {
        }

		public bool Load()
		{
	        string path = (System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), SettingFilename));
            try
            {
                xmlDocument.Load(path);
				return true;
            }
            catch
            {
                xmlDocument.LoadXml("<settings></settings>");
				return false;
            }
		}

        public void Save()
        {
            xmlDocument.Save(SettingFilename);
        }

		public string OtservRSAKey
		{
			get
			{
	            XmlNode node = xmlDocument.SelectSingleNode("/settings/otserv/publickey");
	            if (node != null)
	            {
	                return node.InnerText;
	            }

	            return "";
			}
		}

		public bool AutoAddServer
		{
			get
			{
	            XmlNode node = xmlDocument.SelectSingleNode("/settings/autoaddserver");
	            if (node != null)
	            {
	                return Convert.ToInt32(node.InnerText) == 1;
	            }

	            return false;
			}

			set
			{
	            XmlNode node = xmlDocument.SelectSingleNode("/settings/autoaddserver");
	            if (node != null)
	            {
	                node.InnerText = (value ? "1" : "0");
	            }
			}
		}

        public List<string> GetClientServerList()
        {
            List<string> list = new List<string>();

            XmlNodeList nodes = xmlDocument.SelectNodes("/settings/client/servers/server");
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    list.Add(node.InnerText);
                }
            }

            return list;
        }

        public List<string> GetRSAKeys()
        {
            List<string> list = new List<string>();

            XmlNodeList nodes = xmlDocument.SelectNodes("/settings/client/publickeys/publickey");
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    list.Add(node.InnerText);
                }
            }

            return list;
        }

        public List<Server> GetServerList()
        {
            List<Server> list = new List<Server>();

            XmlNodeList nodes = xmlDocument.SelectNodes("/settings/servers/server");
            if (nodes != null)
            {
                foreach (XmlNode node in nodes)
                {
                    Server server = new Server();
                    server.name = node.InnerText;
                    XmlAttribute attribute = node.Attributes["port"];
                    if (attribute != null)
                    {
                        server.port = Convert.ToUInt16(attribute.Value);
                    }
                    list.Add(server);
                }
            }

            return list;
        }

        public void UpdateServerList(List<Server> servers)
        {
            XmlNode node = xmlDocument.SelectSingleNode("/settings/servers");
            if(node != null){
                node.RemoveAll();
            }

            foreach(Server server in servers)
            {
                XmlNode serverNode = xmlDocument.CreateNode(XmlNodeType.Element, "server", "");
                serverNode.InnerText = server.name;
                XmlAttribute attribute = xmlDocument.CreateAttribute("port");
                attribute.Value = server.port.ToString();
                serverNode.Attributes.Append(attribute);

                node.AppendChild(serverNode);
            }            
        }
    }
}

