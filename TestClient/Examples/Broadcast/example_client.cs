using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using TestClient.Examples.EntityManager;

namespace TestClient.Examples.Broadcast
{
	[Serializable]
	public class ChatMessagePacket
	{
		public string Message { get; set; }
		public string FromName { get; set; }
		public override string ToString()
		{
			return base.ToString() + $" | [{FromName}] {Message}";
		}
	}
	[Serializable]
	public class ChatConnectPacket
	{
		public string Name { get; set; }
		public override string ToString()
		{
			return base.ToString() + $" | [{Name}]";
		}
	}
	internal class example_client
	{
		string Name = "";
		string ChatHistory = "";
		public void Example()
		{
			EasyTCP.Client client = new EasyTCP.Client();

			client.PacketEntityManager.RegistrationPacket<ChatMessagePacket>(1).CallbackReceiveEvent += Example_client_CallbackReceiveEvent;
			client.PacketEntityManager.RegistrationPacket<ChatConnectPacket>(2).CallbackReceiveEvent += Example_client_CallbackReceiveEvent1;

			client.Connect("localhost", 2020);

			Console.Write($"Enter name: ");
			Name = Console.ReadLine();
			Console.Title = Name;
			client.Send(new ChatConnectPacket() { Name = Name });

			while (true)
			{
				Console.Write("[Enter message] ");
				var message = Console.ReadLine();
				Console.WriteLine();
				client.Send(new ChatMessagePacket() { FromName = Name, Message = message });
			}
		}

		private void Example_client_CallbackReceiveEvent1(object packet, EasyTCP.Packets.Packet rawPacket)
		{
			ChatConnectPacket chatConnect = (packet as ChatConnectPacket);
			ChatHistory += $"[Chat info] user join: {chatConnect.Name}\n";
			Console.Clear();
			Console.WriteLine(ChatHistory);
			Console.Write("[Enter message] ");
		}

		private void Example_client_CallbackReceiveEvent(object packet, EasyTCP.Packets.Packet rawPacket)
		{
			ChatMessagePacket chatMessage = (packet as ChatMessagePacket);
			ChatHistory += $"[Chat message From {chatMessage.FromName}] {chatMessage.Message}\n";
			Console.Clear();
			Console.WriteLine(ChatHistory);
			Console.Write("[Enter message] ");
		}
	}
}
