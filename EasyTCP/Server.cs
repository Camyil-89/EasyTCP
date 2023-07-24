using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EasyTCP.Packets;
using EasyTCP.Firewall;
using EasyTCP.Utilities;

namespace EasyTCP
{
	public class ServerClient
	{
		public Connection Connection { get; set; }
		public TcpClient TCP { get; set; }
	}
	public class Server
	{
		public Serialize.ISerialization Serialization { get; private set; } = new Serialize.StandardSerialize();
		public List<ServerClient> Clients { get; private set; } = new List<ServerClient>();
		public TcpListener TcpListener { get; private set; }
		public IFirewall Firewall { get; private set; }
		public PacketEntityManager PacketEntityManager { get; private set; } = new PacketEntityManager();

		public delegate void CallbackReceive(Packet packet);
		public event CallbackReceive CallbackReceiveEvent;

		public delegate void CallbackConnectClient(ServerClient client);
		public event CallbackConnectClient CallbackConnectClientEvent;

		public delegate void CallbackDisconnectClient(ServerClient client);
		public event CallbackDisconnectClient CallbackDisconnectClientEvent;
		public void Start(int socket, IFirewall firewall = null, Serialize.ISerialization serialization = null)
		{
			Firewall = firewall;
			if (serialization != null)
				Serialization = serialization;
			TcpListener = new TcpListener(IPAddress.Any, socket);
			TcpListener.Start();
			Task.Run(Listener);
		}
		public void Stop()
		{
			if (TcpListener != null)
				TcpListener.Stop();
			TcpListener = null;
			DisconnectAllClient();
		}
		public void DisconnectAllClient()
		{
			foreach (var i in Clients)
			{
				i.Connection.Abort();
			}
		}
		private void Listener()
		{
			while (TcpListener != null)
			{
				try
				{
					var client = TcpListener.AcceptTcpClient();
					if (Firewall != null && Firewall.ValidateConnect(client) == false)
					{
						client.Close();
						client.Dispose();
						continue;
					}
					Thread thread = new Thread(() => { HandlerClient(client); });
					thread.Start();
				}
				catch (Exception e) { }
			}
		}
		private void HandlerClient(TcpClient client)
		{
			ServerClient serverClient = new ServerClient();

			Connection connection = new Connection(client.GetStream(), TypeConnection.Server);
			connection.ServerClient = serverClient;
			connection.CallbackReceiveEvent += Receive;
			connection.Firewall = Firewall;
			connection.Serialization = Serialization;
			connection.Init();

			serverClient.Connection = connection;
			serverClient.TCP = client;

			Clients.Add(serverClient);
			CallbackConnectClientEvent?.Invoke(serverClient);
			while (client != null && client.Connected && serverClient.Connection != null && serverClient.Connection.NetworkStream != null)
			{
				Thread.Sleep(250);
			}
			client.Close();
			client.Dispose();
			CallbackDisconnectClientEvent?.Invoke(serverClient);
			Clients.Remove(serverClient);
		}
		public void Answer(Packet packet, object obj)
		{
			packet.Header.TypePacket = PacketEntityManager.IsEntity(obj.GetType());

			packet.Bytes = Serialization.Raw(obj);

			packet.Answer(packet);
		}
		private void Receive(Packet packet)
		{
			if (packet.Header.Type == PacketType.None &&
				PacketEntityManager.IsEntity(packet.Header.TypePacket) != 0)
			{
				PacketEntityManager.ReceivePacket(packet, Serialization);
				return;
			}
			CallbackReceiveEvent?.Invoke(packet);
		}
	}
}
