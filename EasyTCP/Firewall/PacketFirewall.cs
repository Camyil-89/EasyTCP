using EasyTCP.Packets;

namespace EasyTCP.Firewall
{
	[Serializable]
	public class PacketFirewall : BasePacket
	{
		public PacketFirewall()
		{
			Type = TypePacket.FirewallBlock;
		}
		public string Answer { get; set; } = "";
		public override string ToString()
		{
			return $"{base.ToString()} | {Answer}";
		}
	}
}
