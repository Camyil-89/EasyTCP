using EasyTCP;
using EasyTCP.Firewall;
using EasyTCP.Packets;
using EasyTCP.Serialize;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace Test
{
	internal class Firewall : IFirewall
	{
		private Dictionary<string, List<DateTime>> Clients { get; set; } = new Dictionary<string, List<DateTime>>();
		public Dictionary<string, DateTime> BannedIP { get; set; } = new Dictionary<string, DateTime>();

		public int TimeBanned { get; set; } = 300; // seconds
		public int MaxPerMinuteConnection { get; set; } = 10;
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
			var sec = new SecureSerialize();
			sec.PrivateKey = new byte[] { 48, 130, 2, 92, 2, 1, 0, 2, 129, 129, 0, 190, 198, 163, 196, 175, 81, 66, 182, 20, 140, 114, 1, 108, 35, 34, 183, 218, 29, 147, 84, 95, 129, 32, 77, 234, 131, 103, 224, 174, 210, 156, 62, 121, 169, 42, 139, 229, 117, 179, 137, 199, 184, 98, 212, 254, 93, 198, 65, 195, 56, 185, 142, 90, 195, 163, 27, 196, 227, 202, 183, 245, 124, 115, 171, 137, 131, 195, 253, 46, 188, 83, 252, 138, 9, 9, 130, 83, 98, 202, 240, 91, 111, 71, 30, 107, 81, 196, 10, 173, 178, 17, 1, 207, 157, 148, 187, 147, 154, 111, 103, 80, 106, 138, 36, 167, 133, 146, 165, 95, 201, 248, 218, 242, 212, 75, 37, 77, 70, 46, 39, 21, 26, 126, 213, 209, 170, 229, 57, 2, 3, 1, 0, 1, 2, 129, 128, 127, 160, 220, 135, 4, 210, 212, 82, 131, 196, 193, 176, 121, 235, 183, 154, 79, 237, 97, 87, 28, 221, 130, 3, 30, 84, 242, 245, 185, 127, 100, 207, 215, 12, 121, 78, 70, 32, 76, 16, 108, 240, 202, 13, 188, 110, 119, 232, 30, 246, 160, 12, 192, 100, 9, 134, 214, 93, 158, 141, 27, 74, 59, 6, 234, 179, 213, 150, 185, 113, 190, 178, 179, 174, 161, 51, 87, 185, 55, 62, 159, 70, 66, 193, 241, 21, 243, 33, 251, 220, 159, 56, 0, 38, 87, 146, 161, 225, 95, 221, 202, 148, 253, 253, 24, 198, 14, 130, 68, 94, 153, 32, 23, 181, 113, 71, 143, 132, 219, 50, 244, 217, 232, 43, 140, 126, 212, 209, 2, 65, 0, 239, 230, 245, 70, 52, 155, 112, 38, 26, 10, 187, 240, 189, 162, 141, 37, 159, 26, 228, 193, 104, 93, 165, 199, 40, 179, 187, 150, 149, 115, 241, 132, 206, 60, 72, 244, 190, 164, 31, 188, 129, 22, 61, 74, 29, 225, 164, 47, 236, 191, 217, 86, 171, 104, 126, 173, 184, 45, 254, 145, 48, 95, 122, 55, 2, 65, 0, 203, 147, 202, 97, 8, 203, 2, 199, 247, 108, 161, 31, 142, 207, 163, 129, 152, 67, 118, 132, 153, 158, 234, 124, 35, 161, 229, 216, 136, 231, 97, 40, 167, 222, 164, 98, 172, 98, 85, 119, 212, 52, 123, 78, 172, 228, 233, 127, 70, 34, 151, 157, 41, 29, 140, 5, 126, 242, 163, 57, 10, 181, 36, 15, 2, 65, 0, 159, 190, 67, 126, 95, 19, 77, 167, 33, 90, 26, 113, 32, 100, 247, 229, 160, 63, 49, 41, 148, 12, 31, 146, 49, 9, 21, 21, 29, 41, 90, 30, 27, 145, 202, 230, 165, 118, 245, 230, 248, 113, 205, 151, 231, 179, 211, 55, 82, 71, 33, 58, 115, 226, 157, 207, 161, 63, 135, 46, 56, 110, 171, 27, 2, 64, 105, 10, 21, 151, 33, 169, 86, 3, 5, 136, 56, 78, 135, 42, 93, 204, 37, 91, 81, 208, 179, 79, 10, 224, 8, 166, 165, 104, 167, 162, 243, 63, 189, 246, 35, 205, 129, 242, 174, 244, 200, 58, 88, 17, 77, 38, 67, 208, 86, 200, 204, 127, 219, 210, 18, 8, 87, 235, 44, 10, 231, 154, 117, 67, 2, 64, 106, 25, 143, 111, 46, 221, 234, 113, 85, 15, 122, 7, 194, 149, 96, 115, 102, 167, 202, 183, 59, 105, 197, 123, 240, 195, 112, 193, 204, 120, 189, 88, 94, 126, 248, 245, 75, 58, 21, 93, 120, 108, 141, 80, 69, 176, 14, 165, 154, 249, 131, 77, 67, 212, 176, 50, 105, 223, 137, 71, 118, 61, 219, 222 };

			server.Start(2020, new Firewall(), sec);
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