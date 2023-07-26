# IFirewall
IFirewall - интерфейс для валидации получаемых данных.
# Клиент
```C#
[Serializable]
	public class BigPacket
	{
		public byte[] Bytes { get; set; }
	}
	internal class example_client
	{
		public void Example()
		{
			EasyTCP.Client client = new EasyTCP.Client();

			client.CallbackReceiveFirewallEvent += Client_CallbackReceiveFirewallEvent;

			client.Connect("localhost", 2020);


			try
			{
				Console.WriteLine(client.SendAndWaitResponse<BigPacket>(new BigPacket() { Bytes = new byte[700] }));
			}
			catch (Exception ex) { Console.WriteLine(ex); }
			
			try
			{
				Console.WriteLine(client.SendAndWaitResponse<BigPacket>(new BigPacket() { Bytes = new byte[1023] }));
			}
			catch (Exception ex) { Console.WriteLine(ex); }
			
			for (int i = 0; i < 10; i++)
			{
				EasyTCP.Client client1 = new EasyTCP.Client();
				client1.CallbackReceiveFirewallEvent += Client_CallbackReceiveFirewallEvent;
			
				client1.Connect("localhost", 2020);
			}
		}

		private void Client_CallbackReceiveFirewallEvent(EasyTCP.Packets.PacketFirewall packet)
		{
			Console.WriteLine($"[CLIENT FIREWALL BLOCK] {packet.Code} | {packet.Answer}");
		}
	}
```
# Сервер
```C#
public class example_server
	{
		EasyTCP.Server server = new EasyTCP.Server();
		public void Example()
		{
			server.CallbackConnectClientEvent += Server_CallbackConnectClientEvent;
			server.CallbackDisconnectClientEvent += Server_CallbackDisconnectClientEvent;
			server.CallbackReceiveEvent += Server_CallbackReceiveEvent;

			server.Start(2020, new Firewall());
		}

		private void Server_CallbackReceiveEvent(Packet packet)
		{
			Console.WriteLine($"[SERVER] {packet}");
			packet.Answer(packet);
		}

		private void Server_CallbackDisconnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] Disconnect: {client}");
		}

		private void Server_CallbackConnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] Connect: {client.IpPort}");
		}
	}
```
# IFirewall
```C#
internal class Firewall : IFirewall
	{
		private Dictionary<string, List<DateTime>> Clients { get; set; } = new Dictionary<string, List<DateTime>>();
		public Dictionary<string, DateTime> BannedIP { get; set; } = new Dictionary<string, DateTime>();

		public int TimeBanned { get; set; } = 300; // seconds
		public int MaxPerMinuteConnection { get; set; } = 3;
		public int MaxSizePacket { get; set; } = 1024;
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

		public bool ValidateConnect(ServerClient client)
		{
			var ip = client.TCP.Client.RemoteEndPoint.ToString().Split(":")[0];
			if (Clients.ContainsKey(ip))
			{
				Clients[ip].Add(DateTime.Now);
			}
			else
				Clients.Add(ip, new List<DateTime>() { DateTime.Now });
			CheckClients();
			return BannedIP.ContainsKey(ip) == false;
		}
	}
```
