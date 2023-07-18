

namespace EasyTCP.Packets
{
	public enum TypePacket: byte
	{
		None = 0,
		Ping = 1,
		RSTStopwatch = 2,
		ReceiveInfo = 3,
		FirewallBlock = 4,
		Serialize = 5,
	}
}
