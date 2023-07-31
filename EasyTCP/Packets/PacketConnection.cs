using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EasyTCP.Packets
{
	public enum PacketConnectionType: byte
	{
		Abort = 0,
		OK = 1,
	}
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct PacketConnection
	{
		public PacketFirewall Firewall;
		public PacketConnectionType Type;
		public int BlockSize;

		public static PacketConnection Create(PacketConnectionType type)
		{
			var connection = new PacketConnection();
			connection.Type = type;
			return connection;
		}
		public static PacketConnection Create(PacketConnectionType type, PacketFirewall firewall)
		{
			var connection = new PacketConnection();
			connection.Type = type;
			connection.Firewall = firewall;
			return connection;
		}
	}
}
