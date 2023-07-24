# PacketEntityManager
PacketEntityManager - класс позволяющий регистрировать пакеты (любые типы данных) как у клиента, так и у сервера. При получении пакета, он будет проверяться в PacketEntityManager если он зарегистрирован, тогда будет вызвана функция и в нее передасться полученный пакет.
## Клиент

![image](https://github.com/Camyil-89/EasyTCP/assets/76705837/a316cccb-e50a-42e8-9acc-d3470e014b9f)

```C#
[Serializable]
	public class ConnectPacket
	{
		public string Login { get; set; }
		public string Password { get; set; }

		public override string ToString()
		{
			return $"Login: {Login} | Password: {Password}";
		}
	}

	[Serializable]
	public class ConnectionStatusPacket
	{
		public string Status { get; set; }
		public override string ToString()
		{
			return $"Status: {Status}";
		}
	}

	[Serializable]
	public class MessagePacket
	{
		public string Message { get; set; }
		public override string ToString()
		{
			return $"Message: {Message}";
		}
	}

	[Serializable]
	public class NotRegistrationPacket
	{
		public string Message { get; set; }
		public override string ToString()
		{
			return $"NotRegMessage: {Message}";
		}
	}


	public class example_client
	{
		public void Example()
		{
			EasyTCP.Client client = new EasyTCP.Client();

			client.PacketEntityManager.RegistrationPacket<ConnectPacket>(1); // id должен совпадать как и на клиенте так и на сервере.
			client.PacketEntityManager.RegistrationPacket<ConnectionStatusPacket>(2).CallbackReceiveEvent += ConnectionStatusPacket_CallbackReceiveEvent;
			client.PacketEntityManager.RegistrationPacket<MessagePacket>(3).CallbackReceiveEvent += MessagePacket_CallbackReceiveEvent;

			client.Connect("localhost", 2020);

			Console.WriteLine(client.SendAndWaitResponse<ConnectionStatusPacket>(new ConnectPacket() { Login = "Admin", Password = "123" }));
			client.Send(new ConnectPacket() { Login = "Ivan", Password = "321" }); // отправляет пакет и не ждет ответа
			client.Send(new MessagePacket() { Message = "Disconnect, bye!" });
			client.Send(new NotRegistrationPacket() { Message = "Примет, я не зарешестрированный класс!" });
			Thread.Sleep(3000); // ждем все ответы от сервера.
			client.Close();
			Console.WriteLine(client.Connection.Statistics); // статистика отправки и принятие пакетов и количества байт.
		}

		private void ConnectionStatusPacket_CallbackReceiveEvent(object Packet, EasyTCP.Packets.Packet RawPacket)
		{
			Console.WriteLine($"Connection status: {Packet}");
		}

		private void MessagePacket_CallbackReceiveEvent(object Packet, EasyTCP.Packets.Packet RawPacket)
		{
			Console.WriteLine($"Message from server: {Packet}");
		}
	}
```

## Сервер

![image](https://github.com/Camyil-89/EasyTCP/assets/76705837/0c4dce2b-0607-477a-89b0-038afdcc9c72)

```C#
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
```
