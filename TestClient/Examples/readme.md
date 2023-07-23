# Simple
Пример простого TCP соединение между клиентом и сервером.
## Клиент
![image](https://github.com/Camyil-89/EasyTCP/assets/76705837/ed5a9589-fbba-49bb-a188-cd542bf39cd5)
```C#
[Serializable]
	internal class MyPacket : EasyTCP.Packets.BasePacket
	{
		public string Message = "Hello server!";
		public override string ToString()
		{
			return $"{base.ToString()} | {Message}";
		}
	}

	[Serializable]
	internal class BigPacket : BasePacket
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
```
## Сервер
![image](https://github.com/Camyil-89/EasyTCP/assets/76705837/8cee742c-5dad-485e-b003-556688c6dca4)
```C#
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
```