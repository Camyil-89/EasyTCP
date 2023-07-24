using EasyTCP.Packets;
using EasyTCP.Serialize;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace EasyTCP.Utilities
{
	public class PacketEntityManager
	{
		private Dictionary<byte, PacketEntity> PacketObservers = new Dictionary<byte, PacketEntity>();
		private Dictionary<Type, byte> PacketObserversTypes = new Dictionary<Type, byte>();
		public PacketEntity RegistrationPacket<T>(byte id)
		{
			if (id == 0)
				throw new Exception("0 is reserved");
			var pe = new PacketEntity() { Type = id, ObjType = typeof(T) };
			PacketObservers.Add(id, pe);
			PacketObserversTypes.Add(typeof(T), id);
			return pe;
		}
		public byte IsEntity(byte type)
		{
			if (PacketObservers.ContainsKey(type))
				return type;
			else
				return 0;
		}
		public byte IsEntity(Type type)
		{
			if (PacketObserversTypes.ContainsKey(type))
				return PacketObserversTypes[type];
			else
				return 0;
		}
		public void ReceivePacket(Packet packet, ISerialization serialization)
		{
			if (PacketObservers.ContainsKey(packet.Header.TypePacket))
			{
				var entity = ((PacketEntity)PacketObservers[packet.Header.TypePacket]);
				entity.Call(serialization, packet);
			}
		}
	}
}
