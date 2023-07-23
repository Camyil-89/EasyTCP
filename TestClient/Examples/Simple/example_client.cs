﻿
using EasyTCP.Packets;

namespace TestClient.Examples.Simple
{
	[Serializable]
	internal class MyPacket
	{
		public string Message = "Hello server!";
		public override string ToString()
		{
			return $"{base.ToString()} | {Message}";
		}
	}

	[Serializable]
	internal class BigPacket
	{
		public byte[] Bytes = new byte[1024 * 1024 * 25]; // 25 mb

		public override string ToString()
		{
			return $"{base.ToString()} | {Bytes.Length}";
		}
	}
	internal class example_client
	{
		public void Example()
		{
			EasyTCP.Client client = new EasyTCP.Client();
			client.Connect("localhost", 2020);
			var answer = client.SendAndWaitResponse(new MyPacket());
			Console.WriteLine($"[FROM SERVER] {answer}");

			foreach (var response_from_server in client.SendAndReceiveInfo(new BigPacket()))
			{
				if (response_from_server.Packet != null)
				{
					Console.WriteLine($"[PACKET FROM SERVER] {response_from_server.Packet}");
				}
				else if (response_from_server.ReceiveFromServer)
					Console.WriteLine($"[RECEIVE FROM SERVER] progress: {response_from_server.Info.Receive} \\ {response_from_server.Info.TotalNeedReceive} bytes");
				else
					Console.WriteLine($"[SEND TO SERVER] progress: {response_from_server.Info.Receive} \\ {response_from_server.Info.TotalNeedReceive} bytes");
			}

		}
	}
}