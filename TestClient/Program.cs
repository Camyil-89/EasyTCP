﻿using EasyTCP;
using EasyTCP.Packets;
using EasyTCP.Serialize;

namespace TestClient
{
	[Serializable]
	internal class MyPacket: BasePacket
	{
		public string Message = "Hello, world!";

		public override string ToString()
		{
			return $"{base.ToString()} | {Message}";
		}
	}

	[Serializable]
	internal class BigPacket : BasePacket
	{
		public byte[] Bytes = new byte[1024 * 1024 * 25]; // 1 mb

		public override string ToString()
		{
			return $"{base.ToString()} | {Bytes.Length}";
		}
	}
	internal class Program
	{
		static void Main(string[] args)
		{
			new Examples.Simple.example_client().Example();
			//EasyTCP.Client client = new EasyTCP.Client();
			//var sec = new SecureSerialize();
			//sec.PublicKey = new byte[] { 48, 129, 137, 2, 129, 129, 0, 190, 198, 163, 196, 175, 81, 66, 182, 20, 140, 114, 1, 108, 35, 34, 183, 218, 29, 147, 84, 95, 129, 32, 77, 234, 131, 103, 224, 174, 210, 156, 62, 121, 169, 42, 139, 229, 117, 179, 137, 199, 184, 98, 212, 254, 93, 198, 65, 195, 56, 185, 142, 90, 195, 163, 27, 196, 227, 202, 183, 245, 124, 115, 171, 137, 131, 195, 253, 46, 188, 83, 252, 138, 9, 9, 130, 83, 98, 202, 240, 91, 111, 71, 30, 107, 81, 196, 10, 173, 178, 17, 1, 207, 157, 148, 187, 147, 154, 111, 103, 80, 106, 138, 36, 167, 133, 146, 165, 95, 201, 248, 218, 242, 212, 75, 37, 77, 70, 46, 39, 21, 26, 126, 213, 209, 170, 229, 57, 2, 3, 1, 0, 1 };
			//client.Connect("localhost", 2020, sec);
			//client.Connection.CallbackReceiveEvent += Connection_CallbackReceiveEvent;
			//while (sec.Initial == false)
			//{
			//	Console.WriteLine("WAIT sec");
			//	Thread.Sleep(250);
			//}
			//
			//Console.WriteLine(client.SendAndWaitResponse(new MyPacket()));
			//try
			//{
			//	foreach (var i in client.SendAndReceiveInfo(new BigPacket()))
			//	{
			//		Console.WriteLine($"{i.Packet} | {i.Info.Receive} \\ {i.Info.TotalNeedReceive}");
			//	}
			//} catch (Exception ex) { Console.WriteLine(ex); }
			//while (true)
			//{
			//	Thread.Sleep(1);
			//	Console.WriteLine($"{client.SendAndWaitResponse(new MyPacket())}");
			//}

			Console.WriteLine("[END]");
			Console.Read();
		}

		private static void Connection_CallbackReceiveEvent(EasyTCP.Packets.BasePacket packet)
		{
			Console.WriteLine($"[CLIENT] {packet}");
		}
	}
}