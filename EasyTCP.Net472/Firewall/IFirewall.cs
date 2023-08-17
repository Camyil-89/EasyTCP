using EasyTCP.Packets;
using System.Net.Sockets;

namespace EasyTCP.Firewall
{
	public interface IFirewall
	{
		PacketFirewall ValidateHeaderAnswer(HeaderPacket header);
		PacketFirewall ValidateRawAnswer(byte[] data);
		PacketFirewall ValidateConnectAnswer(ServerClient client);
		bool ValidateHeader(HeaderPacket header);
		bool ValidateRaw(byte[] data);
		bool ValidateConnect(ServerClient client);
	}
}
