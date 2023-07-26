

using EasyTCP.Serialize;
using System.Net.Sockets;

namespace EasyTCP.Packets
{
	public class Packet
	{
		/// <summary>
		/// Только если вы сервер, тут будет клиент.
		/// </summary>
		public ServerClient Client { get; set; } = null;
		public HeaderPacket Header;
		public byte[] Bytes;

		public delegate void CallbackAnswer(Packet packet);
		public event CallbackAnswer CallbackAnswerEvent;
		/// <summary>
		/// Отправляет пакет с таким же UID чтобы клиент принял ожидаемый пакет.
		/// </summary>
		/// <param name="packet"></param>
		public void Answer(Packet packet)
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
		/// <summary>
		/// Сбрасывает таймер у клиента (таймер на ожидание пакета от сервера)
		/// </summary>
		public void ResetWatchdog()
		{
			var packet = new Packet();
			packet.Header = Header;
			packet.Header.Type = PacketType.RSTStopwatch;
			CallbackAnswerEvent?.Invoke(packet);
		}
		/// <summary>
		/// Отправляет пустой пакет только с заголовком.
		/// </summary>
		public void AnswerNull()
		{
			var packet = new Packet();
			packet.Header = Header;
			CallbackAnswerEvent?.Invoke(packet);
		}
	}
}
