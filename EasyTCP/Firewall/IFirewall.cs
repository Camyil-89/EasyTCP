using EasyTCP.Packets;
using System.Net.Sockets;

namespace EasyTCP.Firewall
{
    public interface IFirewall
	{
		public PacketFirewall ValidateHeaderAnswer(HeaderPacket header);
		public PacketFirewall ValidateRawAnswer(byte[] data);
		public PacketFirewall ValidateConnectAnswer(ServerClient client);
		public bool ValidateHeader(HeaderPacket header);
		public bool ValidateRaw(byte[] data);
		public bool ValidateConnect(TcpClient client);
	}
}
