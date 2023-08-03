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
using EasyTCP.Serialize;

namespace EasyTCP
{
	public class ServerClient
	{
		public Client Client { get; set; }
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
		public PacketEntityManager PacketEntityManager { get; set; } = new PacketEntityManager();

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
				serverClient.Connection = new Connection(client.GetStream(), TypeConnection.Server);
				if (IsSsl)
				{
					serverClient.Connection.EnableSsl(Certificate, IsCheckCert);
				}
				serverClient.Connection.ServerClient = serverClient;
				serverClient.Connection.BlockSizeForSendInfoReceive = BlockSizeForSendInfoReceive;
				serverClient.Connection.Firewall = Firewall;
				serverClient.Connection.CallbackReceiveEvent += Receive;

				var packet_client = serverClient.Connection.WaitPacketConnection().Result;

				serverClient.Connection.Init();
				if (Firewall != null && Firewall.ValidateConnect(serverClient) == false)
				{
					var connect_packet = serverClient.Connection.Serialization.FromRaw<PacketConnection>(packet_client.Bytes);
					connect_packet.Firewall = Firewall.ValidateConnectAnswer(serverClient);
					connect_packet.Type = PacketConnectionType.Abort;
					connect_packet.BlockSize = BlockSizeForSendInfoReceive;
					packet_client.Bytes = serverClient.Connection.Serialization.Raw(connect_packet);
					serverClient.Connection.WriteStream(packet_client.Bytes, packet_client.Header).Wait();
					Thread.Sleep(3000);
					serverClient.Connection.Abort();
					client.Close();
					client.Dispose();
				}
				else
				{
					serverClient.Connection.WriteStream(packet_client.Bytes, packet_client.Header).Wait();
					
					serverClient.Connection.Serialization = Serialization;
					serverClient.Connection.InitSerialization();
					serverClient.Client = new Client();
					serverClient.Client.TCPClient = serverClient.TCP;
					serverClient.Client.Connection = serverClient.Connection;
					serverClient.Client.PacketEntityManager = PacketEntityManager;
					Clients.Add(serverClient);
					CallbackConnectClientEvent?.Invoke(serverClient);
					while (client != null && client.Connected && serverClient.Connection != null && serverClient.Connection.IsWork == true)
					{
						Thread.Sleep(250);
					}
				}
			}
			catch (Exception e) { Console.WriteLine(e); }
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
