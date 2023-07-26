using EasyTCP;
using EasyTCP.Firewall;
using EasyTCP.Packets;
using EasyTCP.Serialize;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;

namespace TestServer
{

	internal class Program
	{
		static void Main(string[] args)
		{
			//new Examples.Simple.example_server().Example();
			//new Examples.EntityManager.example_server().Example();
			//new Examples.Ssl.example_server().Example();
			//new Examples.Serialize.example_server().Example();
			//new Examples.Broadcast.example_server().Example();
			//new Examples.Firewall.example_server().Example();

			//EasyTCP.Server server = new EasyTCP.Server();
			//server.CallbackReceiveEvent += Server_CallbackReceiveEvent; ;
			//server.CallbackConnectClientEvent += Server_CallbackConnectClientEvent;
			//server.CallbackDisconnectClientEvent += Server_CallbackDisconnectClientEvent;
			//
			//server.PacketEntityManager.RegistrationPacket<TestClient.TestPacket>(1).CallbackReceiveEvent += Program_CallbackReceiveEvent1;
			//server.PacketEntityManager.RegistrationPacket<TestClient.Test1Packet>(2).CallbackReceiveEvent += Program_CallbackReceiveEvent; 
			//
			//server.Start(2020);

			//Console.WriteLine("[END]");
			Console.Read();
		}

		private static void Program_CallbackReceiveEvent(object Packet, Packet RawPacket)
		{
			Console.WriteLine($"[SERVER TestClient.Test1Packet] {Packet} | {Packet.GetType()} | {RawPacket.Client.TCP.Client.RemoteEndPoint}");
			RawPacket.Answer(RawPacket);
		}

		private static void Program_CallbackReceiveEvent1(object Packet, Packet RawPacket)
		{
			Console.WriteLine($"[SERVER TestClient.TestPacket] {Packet} | {Packet.GetType()} | {RawPacket.Client.TCP.Client.RemoteEndPoint}");
			RawPacket.Answer(RawPacket);
		}
		private static void Server_CallbackReceiveEvent(Packet packet)
		{
			Console.WriteLine($"[SERVER] {packet} {packet.Header.Type} {packet.Header.UID} {packet.Bytes.Length}");
			packet.Answer(packet);
		}

		private static void Server_CallbackDisconnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] DISCONNECT {client}");
		}

		private static void Server_CallbackConnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] CONNECT {client.TCP.Client.RemoteEndPoint}");
		}
	}
}