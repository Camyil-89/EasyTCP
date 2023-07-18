using EasyTCP;
using EasyTCP.Firewall;
using EasyTCP.Packets;
using System.Net.Sockets;

namespace Test
{
	internal class Firewall : IFirewall
	{
		private Dictionary<string, List<DateTime>> Clients { get; set; } = new Dictionary<string, List<DateTime>>();
		public Dictionary<string, DateTime> BannedIP { get; set; } = new Dictionary<string, DateTime>();

		public int TimeBanned { get; set; } = 300; // seconds
		public int MaxPerMinuteConnection { get; set; } = 0;
		public int MaxSizePacket { get; set; } = 1024 * 1024 * 25; // 25 mb
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

		public string ValidateConnectAnswer(ServerClient client)
		{
			throw new NotImplementedException();
		}

		public bool ValidateHeader(HeaderPacket header)
		{
			return header.DataSize < 1024 * 25;
		}

		public string ValidateHeaderAnswer(HeaderPacket header)
		{
			return $"Bad size packet ({header.DataSize} \\ {1024 * 25})";
		}

		public bool ValidatePacket(BasePacket packet)
		{
			return true;
		}

		public string ValidatePacketAnswer(BasePacket packet)
		{
			return "Bad type packet";
		}

		public bool ValidateRaw(byte[] data)
		{
			return data.Length < 1024 * 25;
		}

		public string ValidateRawAnswer(byte[] data)
		{
			return $"Bad size raw data ({data.Length} \\ {1024 * 25})";
		}
	}
	internal class Program
	{
		static void Main(string[] args)
		{
			EasyTCP.Server server = new EasyTCP.Server();
			server.Start(2020, new Firewall());
			server.CallbackReceiveEvent += RServer;
			server.CallbackConnectClientEvent += Server_CallbackConnectClientEvent;
			server.CallbackDisconnectClientEvent += Server_CallbackDisconnectClientEvent;

			Console.WriteLine("[END]");
			Console.Read();
		}

		private static void Server_CallbackDisconnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] DISCONNECT {client.TCP.Client.RemoteEndPoint}");
		}

		private static void Server_CallbackConnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] CONNECT {client.TCP.Client.RemoteEndPoint}");
		}
		private static void RServer(BasePacket packet)
		{
			Console.WriteLine($"[SERVER] {packet}");
			packet.Answer(packet);
		}
	}
}