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
using System.Security.Cryptography.X509Certificates;

namespace EasyTCP
{
	public class ServerClient
	{
		public Connection Connection { get; set; }
		public TcpClient TCP { get; set; }
		public string IpPort { get; set; }
	}
	public class Server
	{
		private X509Certificate Certificate;
		private bool IsSsl = false;
		private bool IsCheckCert = true;

		public int BlockSizeForSendInfoReceive { get; set; } = 1024 * 1024;
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
		public void EnableSsl(X509Certificate certificate)
		{
			IsSsl = true;
			Certificate = certificate;
		}
		private void Listener()
		{
			while (TcpListener != null)
			{
				try
				{
					var client = TcpListener.AcceptTcpClient();
					Thread thread = new Thread(() => { HandlerClient(client); });
					thread.Start();
				}
				catch (Exception e) { }
			}
		}
		private void HandlerClient(TcpClient client)
		{
			ServerClient serverClient = new ServerClient();
			serverClient.TCP = client;
			serverClient.IpPort = client.Client.RemoteEndPoint.ToString();
			try
			{
				Connection connection = new Connection(client.GetStream(), TypeConnection.Server);
				if (IsSsl)
				{
					connection.EnableSsl(Certificate, IsCheckCert);
				}
				connection.ServerClient = serverClient;
				connection.CallbackReceiveEvent += Receive;
				connection.BlockSizeForSendInfoReceive = BlockSizeForSendInfoReceive;
				connection.Firewall = Firewall;

				serverClient.Connection = connection;
				var packet_client = connection.WaitPacketConnection().Result;

				connection.Init();
				if (Firewall != null && Firewall.ValidateConnect(serverClient) == false)
				{
					var connect_packet = Serialization.FromRaw<PacketConnection>(packet_client.Bytes);
					connect_packet.Firewall = Firewall.ValidateConnectAnswer(serverClient);
					connect_packet.Type = PacketConnectionType.Abort;
					connect_packet.BlockSize = BlockSizeForSendInfoReceive;
					packet_client.Bytes = Serialization.Raw(connect_packet);
					serverClient.Connection.WriteStream(packet_client.Bytes, packet_client.Header).Wait();
					Thread.Sleep(3000);
					serverClient.Connection.Abort();
					client.Close();
					client.Dispose();
				}
				else
				{
					serverClient.Connection.WriteStream(packet_client.Bytes, packet_client.Header).Wait();
					connection.Serialization = Serialization;
					serverClient.Connection.InitSerialization();
					Clients.Add(serverClient);
					CallbackConnectClientEvent?.Invoke(serverClient);
					while (client != null && client.Connected && serverClient.Connection != null && serverClient.Connection.IsWork == true)
					{
						Thread.Sleep(250);
					}
				}





			}
			catch (Exception e) { }
			client.Close();
			client.Dispose();
			CallbackDisconnectClientEvent?.Invoke(serverClient);
			Clients.Remove(serverClient);
		}
		public void AnswerBroadcast(object obj, List<ServerClient> blackList = null)
		{
			if (blackList == null)
				blackList = new List<ServerClient>();

			var header = HeaderPacket.Create();
			header.TypePacket = PacketEntityManager.IsEntity(obj.GetType());

			foreach (var client in Clients)
			{
				if (blackList.Contains(client))
					continue;
				client.Connection.Send(obj, header).Wait();
			}
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
