using System;
using System.Xml;
using System.Windows.Forms;
using System.Collections.Generic;

namespace otloader
{
	public class Server
	{
		public Server()
		{
		}

		public Server(string _name)
		{
			name = _name;
		}

		public Server(string _name, UInt16 _port)
		{
			name = _name;
			port = _port;
		}

		public Server(string _name, string _port)
		{
			name = _name;
			try
			{
				port = UInt16.Parse(_port);
			}
			catch
			{
			}
		}

		public string name = "127.0.0.1";
		public UInt16 port = 7171;
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
					try
					{
						return Int32.Parse(node.InnerText) == 1;
					}
					catch
					{
					}
				}

				return false;
			}

			set
			{
				XmlNode node = (XmlNode)makeXPath(xmlDocument, "/settings/autoaddserver");
				node.InnerText = (value ? "1" : "0");
			}
		}

		public bool MultiClientPatch
		{
			get
			{
				XmlNode node = xmlDocument.SelectSingleNode("/settings/multiclientpatch");
				if (node != null)
				{
					try
					{
						return Int32.Parse(node.InnerText) == 1;
					}
					catch
					{
					}
				}

				return false;
			}

			set
			{
				XmlNode node = (XmlNode)makeXPath(xmlDocument, "/settings/multiclientpatch");
				node.InnerText = (value ? "1" : "0");
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
					Server server = new Server(node.InnerText);
					XmlAttribute attribute = node.Attributes["port"];
					if (attribute != null)
					{
						try
						{
							server.port = UInt16.Parse(attribute.Value);
						}
						catch
						{
						}
					}

					list.Add(server);
				}
			}

			return list;
		}

		public void UpdateServerList(List<Server> servers)
		{
			XmlNode node = xmlDocument.SelectSingleNode("/settings/servers");
			if(node != null)
			{
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

		//Helper functions
		static private XmlNode makeXPath(XmlDocument doc, string xpath)
		{
			return makeXPath(doc, doc as XmlNode, xpath);
		}

		static private XmlNode makeXPath(XmlDocument doc, XmlNode parent, string xpath)
		{
			// grab the next node name in the xpath; or return parent if empty
			string[] partsOfXPath = xpath.Trim('/').Split('/');
			string nextNodeInXPath = partsOfXPath[0];
			if (string.IsNullOrEmpty(nextNodeInXPath))
				return parent;

			// get or create the node from the name
			XmlNode node = parent.SelectSingleNode(nextNodeInXPath);
			if (node == null)
				node = parent.AppendChild(doc.CreateElement(nextNodeInXPath));

			// rejoin the remainder of the array as an xpath expression and recurse
			string rest = "";
			for(int i = 1; i < partsOfXPath.Length; ++i){
				rest += (i == 1 ? "" : "/") + partsOfXPath[i];
			}

			return makeXPath(doc, node, rest);
		}
	}
}

