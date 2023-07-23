

namespace EasyTCP.Packets
{
	public class Packet
	{
		public HeaderPacket Header;
		public byte[] Bytes;

		public delegate void CallbackAnswer(Packet packet);
		public event CallbackAnswer CallbackAnswerEvent;
		public virtual void Answer(Packet packet)
		{
			packet.Header.UID = Header.UID;
			if (packet.Header.Type == PacketType.RSTStopwatch)
			{
				packet.Bytes = null;
				CallbackAnswerEvent?.Invoke(packet);
			}
			else
				CallbackAnswerEvent?.Invoke(packet);
		}
	}
}
