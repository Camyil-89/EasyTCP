

namespace EasyTCP.Packets
{
	[Serializable]
	public class PacketReceiveInfo: BasePacket
	{
		public PacketReceiveInfo()
		{
			Type = TypePacket.ReceiveInfo;
		}
		public long Receive { get; set; } = 0;
		public long TotalNeedReceive { get; set; } = 0;
	}
}
