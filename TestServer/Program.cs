using EasyTCP;
using EasyTCP.Firewall;
using EasyTCP.Packets;
using EasyTCP.Serialize;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace TestServer
{
    internal class Firewall : IFirewall
	{
		private Dictionary<string, List<DateTime>> Clients { get; set; } = new Dictionary<string, List<DateTime>>();
		public Dictionary<string, DateTime> BannedIP { get; set; } = new Dictionary<string, DateTime>();

		public int TimeBanned { get; set; } = 300; // seconds
		public int MaxPerMinuteConnection { get; set; } = 10;
		public int MaxSizePacket { get; set; } = 1024 * 1024 * 1; // 25 mb
		public bool ValidateConnect(TcpClient client)
		{
			var ip = client.Client.RemoteEndPoint.ToString().Split(":")[0];
			if (Clients.ContainsKey(ip))
			{
				Clients[ip].Add(DateTime.Now);
			}
			else
				Clients.Add(ip, new List<DateTime>() { DateTime.Now });
			CheckClients();
			return BannedIP.ContainsKey(ip) == false;
		}
		private void CheckClients()
		{
			foreach (var i in Clients)
			{
				DateTime currentTime = DateTime.Now;
				DateTime oldestAllowedTime = currentTime.AddSeconds(-60);
				Clients[i.Key].RemoveAll(time => time < oldestAllowedTime);

				if (Clients[i.Key].Count > MaxPerMinuteConnection)
				{
					if (BannedIP.ContainsKey(i.Key))
						BannedIP[i.Key] = DateTime.Now;
					else
						BannedIP.Add(i.Key, DateTime.Now);
				}
			}
			List<string> remove = new List<string>();
			foreach (var i in BannedIP)
			{
				if (i.Value < DateTime.Now.AddSeconds(-TimeBanned))
					remove.Add(i.Key);
			}
			foreach (var i in remove)
				BannedIP.Remove(i);
		}

		public bool ValidateHeader(HeaderPacket header)
		{
			Console.WriteLine(header.DataSize < MaxSizePacket);
			return header.DataSize < MaxSizePacket;
		}

		public bool ValidateRaw(byte[] data)
		{
			return data.Length < MaxSizePacket;
		}

		PacketFirewall IFirewall.ValidateHeaderAnswer(HeaderPacket header)
		{
			return new PacketFirewall() { Code = 1, Answer = $"Bad size packet ({header.DataSize} \\ {MaxSizePacket})" };
		}

		PacketFirewall IFirewall.ValidateRawAnswer(byte[] data)
		{
			return new PacketFirewall() { Code = 2, Answer = $"Bad size packet ({data.LongLength} \\ {MaxSizePacket})" };
		}

		PacketFirewall IFirewall.ValidateConnectAnswer(ServerClient client)
		{
			return new PacketFirewall() { Code = 3, Answer = "DDOS" };
		}
	}

	internal class Program
	{
		static void Main(string[] args)
		{
			//new Examples.EntityManager.example_server().Example();
			//new Examples.Simple.example_server().Example();
			//new Examples.Ssl.example_server().Example();
			new Examples.Serialize.example_server().Example();
			
			//EasyTCP.Server server = new EasyTCP.Server();
			//server.CallbackReceiveEvent += Server_CallbackReceiveEvent; ;
			//server.CallbackConnectClientEvent += Server_CallbackConnectClientEvent;
			//server.CallbackDisconnectClientEvent += Server_CallbackDisconnectClientEvent;
			//
			//server.PacketEntityManager.RegistrationPacket<TestClient.TestPacket>(1).CallbackReceiveEvent += Program_CallbackReceiveEvent1;
			//server.PacketEntityManager.RegistrationPacket<TestClient.Test1Packet>(2).CallbackReceiveEvent += Program_CallbackReceiveEvent; 
			//
			//server.Start(2020);

			//Console.WriteLine("[END]");
			Console.Read();
		}

		private static void Program_CallbackReceiveEvent(object Packet, Packet RawPacket)
		{
			Console.WriteLine($"[SERVER TestClient.Test1Packet] {Packet} | {Packet.GetType()} | {RawPacket.Client.TCP.Client.RemoteEndPoint}");
			RawPacket.Answer(RawPacket);
		}

		private static void Program_CallbackReceiveEvent1(object Packet, Packet RawPacket)
		{
			Console.WriteLine($"[SERVER TestClient.TestPacket] {Packet} | {Packet.GetType()} | {RawPacket.Client.TCP.Client.RemoteEndPoint}");
			RawPacket.Answer(RawPacket);
		}
		private static void Server_CallbackReceiveEvent(Packet packet)
		{
			Console.WriteLine($"[SERVER] {packet} {packet.Header.Type} {packet.Header.UID} {packet.Bytes.Length}");
			packet.Answer(packet);
		}

		private static void Server_CallbackDisconnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] DISCONNECT {client}");
		}

		private static void Server_CallbackConnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] CONNECT {client.TCP.Client.RemoteEndPoint}");
		}
	}
}