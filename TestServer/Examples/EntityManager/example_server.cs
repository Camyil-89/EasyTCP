using EasyTCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestServer.Examples.EntityManager
{
	public class example_server
	{
		EasyTCP.Server server = new EasyTCP.Server();
		public void Example()
		{
			server.CallbackConnectClientEvent += Server_CallbackConnectClientEvent;
			server.CallbackDisconnectClientEvent += Server_CallbackDisconnectClientEvent;
			server.CallbackReceiveEvent += Server_CallbackReceiveEvent;

			server.PacketEntityManager.RegistrationPacket<TestClient.Examples.EntityManager.ConnectPacket>(1).CallbackReceiveEvent += ConnectPacket_CallbackReceiveEvent;
			server.PacketEntityManager.RegistrationPacket<TestClient.Examples.EntityManager.ConnectionStatusPacket>(2);
			server.PacketEntityManager.RegistrationPacket<TestClient.Examples.EntityManager.MessagePacket>(3).CallbackReceiveEvent += MessagePacket_CallbackReceiveEvent;

			server.Start(2020);
		}

		/// <summary>
		/// Сюда попадают все пакеты которые не были зарегистрированы.
		/// </summary>
		/// <param name="packet"></param>
		private void Server_CallbackReceiveEvent(EasyTCP.Packets.Packet packet)
		{
			Console.WriteLine($"RAW packet: {packet} | {server.Serialization.FromRaw(packet.Bytes)}");
		}

		private void MessagePacket_CallbackReceiveEvent(object Packet, EasyTCP.Packets.Packet RawPacket)
		{
			Console.WriteLine($"Message from client: {Packet}");
		}

		private void ConnectPacket_CallbackReceiveEvent(object Packet, EasyTCP.Packets.Packet RawPacket)
		{
			Console.WriteLine($"Connect request: {Packet}");
			server.Answer(RawPacket, new TestClient.Examples.EntityManager.ConnectionStatusPacket() { Status = "OK" });

			RawPacket.Header.NewPacket(); // генерируем новый UID чтобы клиент случайно не принял не тот пакет.
			server.Answer(RawPacket, new TestClient.Examples.EntityManager.MessagePacket { Message = "Welcome to server!"});
		}

		private void Server_CallbackDisconnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] Disconnect: {client}");
		}

		private void Server_CallbackConnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] Connect: {client.TCP.Client.RemoteEndPoint}");
		}
	}
}
