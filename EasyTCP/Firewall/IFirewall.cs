using EasyTCP.Packets;

namespace EasyTCP.Firewall
{
	public interface IFirewall
	{
		public string ValidateHeaderAnswer(HeaderPacket header);
		public string ValidateRawAnswer(byte[] data);
		public string ValidatePacketAnswer(BasePacket packet);
		public bool ValidateHeader(HeaderPacket header);
		public bool ValidateRaw(byte[] data);
		public bool ValidatePacket(BasePacket packet);
	}
}
