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
		private Dictionary<ushort, PacketEntity> PacketObservers = new Dictionary<ushort, PacketEntity>();
		private Dictionary<Type, ushort> PacketObserversTypes = new Dictionary<Type, ushort>();
		public PacketEntity RegistrationPacket<T>(ushort id)
		{
			if (id == 0)
				throw new Exception("0 is reserved");
			var pe = new PacketEntity() { Type = id, ObjType = typeof(T) };
			PacketObservers.Add(id, pe);
			PacketObserversTypes.Add(typeof(T), id);
			return pe;
		}
		public ushort IsEntity(ushort type)
		{
			if (PacketObservers.ContainsKey(type))
				return type;
			else
				return 0;
		}
		public ushort IsEntity(Type type)
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
