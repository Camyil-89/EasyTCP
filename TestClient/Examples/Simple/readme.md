# Simple
Пример простого TCP соединение между клиентом и сервером.
## Клиент
```C#
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
		public void Example()
		{
			EasyTCP.Client client = new EasyTCP.Client();
			client.Connect("localhost", 2020);
			var answer = client.SendAndWaitResponse<MyPacket>(new MyPacket(), 1000);
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
			Console.WriteLine(client.Connection.Statistics);
			Console.WriteLine(client.Connection.Statistics.ReceivedBytes - client.Connection.Statistics.SentBytes); // выводит накладные расходы.
		}
	}
```
## Сервер
```C#
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
```
