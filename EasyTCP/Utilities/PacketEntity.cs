using EasyTCP.Packets;
using EasyTCP.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTCP.Utilities
{
	public class PacketEntity
	{
		public byte Type { get; init; } = 0;
		public Type ObjType { get; init; }
		public delegate void CallbackReceive(object packet, Packet rawPacket);
		public event CallbackReceive CallbackReceiveEvent;

		public void Call(ISerialization serialization, Packet packet)
		{
			CallbackReceiveEvent?.Invoke(serialization.FromRaw(packet.Bytes, ObjType), packet);
		}
	}
}
