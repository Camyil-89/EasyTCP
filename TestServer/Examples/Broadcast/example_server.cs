using EasyTCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestClient.Examples.Broadcast;
using TestClient.Examples.Serialize;

namespace TestServer.Examples.Broadcast
{
	internal class example_server
	{
		EasyTCP.Server server = new EasyTCP.Server();
		public void Example()
		{
			server.CallbackConnectClientEvent += Server_CallbackConnectClientEvent;
			server.CallbackDisconnectClientEvent += Server_CallbackDisconnectClientEvent;

			server.PacketEntityManager.RegistrationPacket<ChatMessagePacket>(1).CallbackReceiveEvent += Example_server_CallbackReceiveEvent1;
			server.PacketEntityManager.RegistrationPacket<ChatConnectPacket>(2).CallbackReceiveEvent += Example_server_CallbackReceiveEvent2;

			server.Start(2020);
		}

		private void Example_server_CallbackReceiveEvent2(object packet, EasyTCP.Packets.Packet rawPacket)
		{
			Console.WriteLine($"[SERVER] {packet}");
			server.AnswerBroadcast(packet, new List<ServerClient>() { rawPacket.Client }); // не отправляем человеку который подключился к чату.
		}

		private void Example_server_CallbackReceiveEvent1(object packet, EasyTCP.Packets.Packet rawPacket)
		{
			Console.WriteLine($"[SERVER] {packet}");
			server.AnswerBroadcast(packet);
		}

		private void Server_CallbackDisconnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] Disconnect: {client.IpPort}");
		}

		private void Server_CallbackConnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] Connect: {client.IpPort}");
		}
	}
}
