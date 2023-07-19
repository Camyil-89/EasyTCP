using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test.Examples.Simple
{
	internal class example_server
	{

		public void Example()
		{
			EasyTCP.Server server = new EasyTCP.Server();
			server.Start(2020);
			server.CallbackConnectClientEvent += Server_CallbackConnectClientEvent;
			server.CallbackDisconnectClientEvent += Server_CallbackDisconnectClientEvent;
			server.CallbackReceiveEvent += Server_CallbackReceiveEvent;
		}

		private void Server_CallbackReceiveEvent(EasyTCP.Packets.BasePacket packet)
		{
			Console.WriteLine($"[SERVER RECEIVE] {packet}");
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
