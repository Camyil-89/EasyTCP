using EasyTCP.Firewall;
using EasyTCP.Packets;

namespace Test
{
	internal class Firewall : IFirewall
	{
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