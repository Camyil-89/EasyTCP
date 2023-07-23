
using System.Runtime.InteropServices;


namespace EasyTCP.Packets
{
	public enum PacketType : byte
	{
		None = 0,
		Ping = 1,
		RSTStopwatch = 2,
		ReceiveInfo = 3,
		FirewallBlock = 4,
		Serialize = 5,
	}
	public enum PacketMode: byte
	{
		Hidden = 0,
		Info = 1,
		ReceiveInfo = 2,
	}
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct HeaderPacket
	{
		public byte Version;
		public long DataSize;
		public int UID;
		public PacketMode Mode;
		public PacketType Type;
		public byte TypePacket;
		public static HeaderPacket Create(PacketType type = PacketType.None, PacketMode mode = PacketMode.Hidden)
		{
			var x = new HeaderPacket();
			x.Version = 1;
			x.Mode = mode;
			x.Type = type;
			x.TypePacket = 0;

			Guid guid = Guid.NewGuid();
			byte[] bytes = guid.ToByteArray();
			x.UID = BitConverter.ToInt32(bytes, 0);
			return x;
		}

		public static HeaderPacket CreateFirewallAnswer(int uid)
		{
			var x = Create(PacketType.None, PacketMode.Hidden);
			x.UID = uid;
			return x;
		}
	}
}
