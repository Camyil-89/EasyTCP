
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
		Abort = 6,
		InitConnection = 7,
	}
	/// <summary>
	/// на данный момент 01.08.2023 не используется и являеться "запасным" параметром. позже будет решаться нужен ли он или нет.
	/// </summary>
	public enum PacketMode: byte 
	{
		Hidden = 0,
		Info = 1,
	}
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct HeaderPacket
	{
		public byte Version { get; set; } // 1 byte
		public int DataSize { get; set; } // 4 byte
		public int UID { get; set; } // 4 byte
		public PacketMode Mode { get; set; } // 1 byte
		public PacketType Type { get; set; } // 1 byte
		public ushort TypePacket { get; set; } // 2 byte
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
			var x = Create(PacketType.FirewallBlock, PacketMode.Hidden);
			x.UID = uid;
			return x;
		}

		public void NewPacket()
		{
			Guid guid = Guid.NewGuid();
			byte[] bytes = guid.ToByteArray();
			UID = BitConverter.ToInt32(bytes, 0);
		}
	}
}
