using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServer.Examples.Simple
{
	internal class example_server
	{

		public void Example()
		{
			EasyTCP.Server server = new EasyTCP.Server();
			server.BlockSizeForSendInfoReceive = 1024 * 1024 * 64; // 64 mb
			server.CallbackConnectClientEvent += Server_CallbackConnectClientEvent;
			server.CallbackDisconnectClientEvent += Server_CallbackDisconnectClientEvent;
			server.CallbackReceiveEvent += Server_CallbackReceiveEvent;
			server.Start(2020);
		}

		private void Server_CallbackReceiveEvent(EasyTCP.Packets.Packet packet)
		{
			Console.WriteLine($"[SERVER RECEIVE] {packet}");

			Thread.Sleep(500); // проверка работы сброса таймера
			packet.ResetWatchdog();
			Thread.Sleep(500); // проверка работы сброса таймера
			packet.ResetWatchdog();
			Thread.Sleep(500); // проверка работы сброса таймера
			packet.ResetWatchdog();

			packet.Answer(packet);
		}

		private void Server_CallbackDisconnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] Disconnect: {client.TCP.Client.RemoteEndPoint}");
		}

		private void Server_CallbackConnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] Connect: {client.TCP.Client.RemoteEndPoint}");
		}
	}
}
