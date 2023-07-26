using EasyTCP;
using EasyTCP.Packets;
using EasyTCP.Serialize;
using System.Runtime.InteropServices;

namespace TestClient
{

	[Serializable]
	internal class BigPacket
	{
		public byte[] Bytes = new byte[1024 * 1024 * 25]; // 1 mb

		public override string ToString()
		{
			return $"{base.ToString()} | {Bytes.Length}";
		}
	}
	[Serializable]
	public class TestPacket
	{
		public string Name { get; set; } = "Test from client";
		public override string ToString()
		{
			return $"{Name}";
		}
	}
	[Serializable]
	public class Test1Packet
	{
		public string Name { get; set; } = "Test from client 2";
		public override string ToString()
		{
			return $"{Name}";
		}
	}
	internal class Program
	{
		static void Main(string[] args)
		{
			new Examples.Simple.example_client().Example();
			//new Examples.EntityManager.example_client().Example();
			//new Examples.Ssl.example_client().Example();
			//new Examples.Serialize.example_client().Example();
			//new Examples.Broadcast.example_client().Example();

			//EasyTCP.Client client = new EasyTCP.Client();
			//client.Connect("localhost", 2020);
			//client.PacketEntityManager.RegistrationPacket<TestPacket>(1).CallbackReceiveEvent += Program_CallbackReceiveEvent; 
			//client.PacketEntityManager.RegistrationPacket<Test1Packet>(2).CallbackReceiveEvent += Program_CallbackReceiveEvent1;
			//client.Send(new TestPacket() { Name = "Hello server"});
			//Console.WriteLine(client.SendAndWaitResponse(new TestPacket()));
			//Console.WriteLine(client.SendAndWaitResponse(new Test1Packet()));
			//foreach (var response_from_server in client.SendAndReceiveInfo<BigPacket>(new BigPacket()))
			//{
			//	if (response_from_server.Packet != null)
			//	{
			//		Console.WriteLine($"[PACKET FROM SERVER] {response_from_server.Packet}");
			//	}
			//	else if (response_from_server.ReceiveFromServer)
			//		Console.WriteLine($"[RECEIVE FROM SERVER] progress: {response_from_server.Info.Receive} \\ {response_from_server.Info.TotalNeedReceive} bytes");
			//	else
			//		Console.WriteLine($"[SEND TO SERVER] progress: {response_from_server.Info.Receive} \\ {response_from_server.Info.TotalNeedReceive} bytes");
			//}
			//
			//Console.WriteLine(client.Connection.Statistics);
			//Console.WriteLine($"{client.Connection.Statistics.SentBytes - client.Connection.Statistics.ReceivedBytes}");
			//client.Close();
			//Console.WriteLine("[END]");
			Console.Read();
		}

		private static void Program_CallbackReceiveEvent1(object Packet, Packet RawPacket)
		{
			Console.WriteLine($"[CLIENT Test1Packet] {Packet}");
		}

		private static void Program_CallbackReceiveEvent(object Packet, Packet RawPacket)
		{
			Console.WriteLine($"[CLIENT TestPacket] {Packet}");
		}

		private static void Connection_CallbackReceiveEvent(Packet packet)
		{
			Console.WriteLine($"[CLIENT] {packet}");
		}
	}
}