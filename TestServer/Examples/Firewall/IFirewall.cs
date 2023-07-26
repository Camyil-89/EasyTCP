using EasyTCP.Firewall;
using EasyTCP.Packets;
using EasyTCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServer.Examples.Firewall
{
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
}
