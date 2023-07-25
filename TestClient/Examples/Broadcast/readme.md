# Broadcast
Пример бродкаст рассылки.
![image](https://github.com/Camyil-89/EasyTCP/assets/76705837/4994f1af-0747-40b2-8d3d-776e9c14b012)

# Клиент
```C#
[Serializable]
	public class ChatMessagePacket
	{
		public string Message { get; set; }
		public string FromName { get; set; }
		public override string ToString()
		{
			return base.ToString() + $" | [{FromName}] {Message}";
		}
	}
	[Serializable]
	public class ChatConnectPacket
	{
		public string Name { get; set; }
		public override string ToString()
		{
			return base.ToString() + $" | [{Name}]";
		}
	}
	internal class example_client
	{
		string Name = "";
		string ChatHistory = "";
		public void Example()
		{
			EasyTCP.Client client = new EasyTCP.Client();

			client.PacketEntityManager.RegistrationPacket<ChatMessagePacket>(1).CallbackReceiveEvent += Example_client_CallbackReceiveEvent;
			client.PacketEntityManager.RegistrationPacket<ChatConnectPacket>(2).CallbackReceiveEvent += Example_client_CallbackReceiveEvent1;

			client.Connect("localhost", 2020);

			Console.Write($"Enter name: ");
			Name = Console.ReadLine();
			Console.Title = Name;
			client.Send(new ChatConnectPacket() { Name = Name });

			while (true)
			{
				Console.Write("[Enter message] ");
				var message = Console.ReadLine();
				Console.WriteLine();
				client.Send(new ChatMessagePacket() { FromName = Name, Message = message });
			}
		}

		private void Example_client_CallbackReceiveEvent1(object packet, EasyTCP.Packets.Packet rawPacket)
		{
			ChatConnectPacket chatConnect = (packet as ChatConnectPacket);
			ChatHistory += $"[Chat info] user join: {chatConnect.Name}\n";
			Console.Clear();
			Console.WriteLine(ChatHistory);
			Console.Write("[Enter message] ");
		}

		private void Example_client_CallbackReceiveEvent(object packet, EasyTCP.Packets.Packet rawPacket)
		{
			ChatMessagePacket chatMessage = (packet as ChatMessagePacket);
			ChatHistory += $"[Chat message From {chatMessage.FromName}] {chatMessage.Message}\n";
			Console.Clear();
			Console.WriteLine(ChatHistory);
			Console.Write("[Enter message] ");
		}
	}
```
# Сервер
```C#
internal class example_server
	{
		EasyTCP.Server server = new EasyTCP.Server();
		public void Example()
		{
			server.CallbackConnectClientEvent += Server_CallbackConnectClientEvent;
			server.CallbackDisconnectClientEvent += Server_CallbackDisconnectClientEvent;

			server.PacketEntityManager.RegistrationPacket<ChatMessagePacket>(1).CallbackReceiveEvent += Example_server_CallbackReceiveEvent1;
			server.PacketEntityManager.RegistrationPacket<ChatConnectPacket>(2).CallbackReceiveEvent += Example_server_CallbackReceiveEvent2;

			server.Start(2020);
		}

		private void Example_server_CallbackReceiveEvent2(object packet, EasyTCP.Packets.Packet rawPacket)
		{
			Console.WriteLine($"[SERVER] {packet}");
			server.AnswerBroadcast(packet, new List<ServerClient>() { rawPacket.Client }); // не отправляем человеку который подключился к чату.
		}

		private void Example_server_CallbackReceiveEvent1(object packet, EasyTCP.Packets.Packet rawPacket)
		{
			Console.WriteLine($"[SERVER] {packet}");
			server.AnswerBroadcast(packet);
		}

		private void Server_CallbackDisconnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] Disconnect: {client.IpPort}");
		}

		private void Server_CallbackConnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] Connect: {client.IpPort}");
		}
	}
```
