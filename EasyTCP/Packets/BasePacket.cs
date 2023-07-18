
namespace EasyTCP.Packets
{
	[Serializable]
	public class BasePacket
	{
		public BasePacket()
		{
			Guid guid = Guid.NewGuid();
			byte[] bytes = guid.ToByteArray();
			UID = BitConverter.ToInt32(bytes, 0);
		}
		public byte Version = 1;
		public int UID;
		public TypePacket Type = TypePacket.None;

		[field: NonSerialized]
		public delegate void CallbackAnswer(BasePacket packet);
		[field: NonSerialized]
		public event CallbackAnswer CallbackAnswerEvent;

		public virtual void Answer(BasePacket packet)
		{
			packet.UID = UID;
			if (packet.Type == TypePacket.RSTStopwatch)
			{
				CallbackAnswerEvent?.Invoke(new Packet() { Type = TypePacket.RSTStopwatch, UID = UID});
			}
			else
				CallbackAnswerEvent?.Invoke(packet);
		}
	}
}
