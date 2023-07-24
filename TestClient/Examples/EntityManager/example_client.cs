using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestClient.Examples.EntityManager
{
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
			client.Send(new ConnectPacket() { Login = "Ivan", Password = "321" });
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
}
