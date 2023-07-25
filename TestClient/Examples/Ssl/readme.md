# Ssl
Пример использования Ssl.
# Клиент
```C#
[Serializable]
	public class SslPacket
	{
		public string Message { get; set; } = "Is SslPacket, it work!";

		public override string ToString()
		{
			return $"{base.ToString()} | {Message}";
		}
	}
	internal class example_client
	{
		public void Example()
		{
			EasyTCP.Client client = new EasyTCP.Client();
			client.EnableSsl(X509Certificate2.CreateFromCertFile("client.pfx"), false);

			client.PacketEntityManager.RegistrationPacket<SslPacket>(1);

			client.Connect("localhost", 2020);
			Console.WriteLine(client.SendAndWaitResponse<SslPacket>(new SslPacket()));
			
		}
	}
```
# Сервер
```C#
internal class example_server
	{
		public void Example()
		{
			EasyTCP.Server server = new EasyTCP.Server();
			server.CallbackConnectClientEvent += Server_CallbackConnectClientEvent;
			server.CallbackDisconnectClientEvent += Server_CallbackDisconnectClientEvent;
			server.CallbackReceiveEvent += Server_CallbackReceiveEvent;

			server.PacketEntityManager.RegistrationPacket<TestClient.Examples.Ssl.SslPacket>(1).CallbackReceiveEvent += Example_server_CallbackReceiveEvent;

			server.EnableSsl(X509Certificate2.CreateFromCertFile("server.pfx"));
			server.Start(2020);
		}

		private void Example_server_CallbackReceiveEvent(object Packet, EasyTCP.Packets.Packet RawPacket)
		{
			Console.WriteLine($"[SERVER {RawPacket.Client.IpPort}] {Packet}");
			RawPacket.Answer(RawPacket);
		}

		private void Server_CallbackReceiveEvent(EasyTCP.Packets.Packet packet)
		{
			Console.WriteLine($"[SERVER RECEIVE] {packet}");
			packet.Answer(packet);
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
