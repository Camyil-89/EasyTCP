
using System.Runtime.InteropServices;


namespace EasyTCP.Packets
{
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
		public static HeaderPacket Create(long data_size, int uid, PacketMode mode = PacketMode.Hidden)
		{
			var x = new HeaderPacket();
			x.DataSize = data_size;
			x.Version = 1;
			x.Mode = mode;

			x.UID = uid;
			return x;
		}
	}
}
