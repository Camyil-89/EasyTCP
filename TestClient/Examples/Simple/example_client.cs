
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
		public byte[] Bytes = new byte[2130702268]; // ~ 2 gb

		public override string ToString()
		{
			return $"{base.ToString()} | {Bytes.Length}";
		}
	}
	internal class example_client
	{
		internal string FormatBytesPerSecond(double bytesPerSecond)
		{
			string[] suffixes = { "B/s", "KB/s", "MB/s", "GB/s", "TB/s", "PB/s", "EB/s", "ZB/s", "YB/s" };
			int suffixIndex = 0;

			while (bytesPerSecond >= 1024 && suffixIndex < suffixes.Length - 1)
			{
				bytesPerSecond /= 1024;
				suffixIndex++;
			}

			return $"{bytesPerSecond:0.##} {suffixes[suffixIndex]}";
		}
		public void Example()
		{
			EasyTCP.Client client = new EasyTCP.Client();
			client.Connect("localhost", 2020);
			Console.WriteLine("Накладные расходы для подключения: ");
			Console.WriteLine(client.Connection.Statistics);
			var answer = client.SendAndWaitResponse<MyPacket>(new MyPacket(), 1000);
			Console.WriteLine($"[FROM SERVER] {answer}");

			foreach (var response_from_server in client.SendAndReceiveInfo(new BigPacket()))
			{
				if (response_from_server.Packet != null)
				{
					Console.WriteLine($"[PACKET FROM SERVER] {response_from_server.Packet}");
				}
				else if (response_from_server.ReceiveFromServer)
					Console.WriteLine($"[RECEIVE FROM SERVER ({FormatBytesPerSecond(client.Connection.Statistics.InstanceReceivedBytesSpeed)} [{FormatBytesPerSecond(client.Connection.Statistics.AverageReceivedBytesSpeed)}])] progress: {response_from_server.Info.Receive} \\ {response_from_server.Info.TotalNeedReceive} bytes");
				else
					Console.WriteLine($"[SEND TO SERVER ({FormatBytesPerSecond(client.Connection.Statistics.InstanceSentBytesSpeed)} [{FormatBytesPerSecond(client.Connection.Statistics.AverageSentBytesSpeed)}])] progress: {response_from_server.Info.Receive} \\ {response_from_server.Info.TotalNeedReceive} bytes");
			}
			Console.WriteLine(client.Connection.Statistics);
			Console.WriteLine(client.Connection.Statistics.ReceivedBytes - client.Connection.Statistics.SentBytes); // выводит накладные расходы.
		}
	}
}
