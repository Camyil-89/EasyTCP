using EasyTCP;
using EasyTCP.Packets;

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
			EasyTCP.Client client = new EasyTCP.Client();
			client.Connect("localhost", 2020);
			client.Connection.CallbackReceiveEvent += Connection_CallbackReceiveEvent;

			Console.WriteLine(client.SendAndWaitResponse(new MyPacket()));
			try
			{
				foreach (var i in client.SendAndReceiveInfo(new BigPacket()))
				{
					Console.WriteLine($"{i.Packet} | {i.Info.Receive} \\ {i.Info.TotalNeedReceive}");
				}
			} catch (Exception ex) { Console.WriteLine(ex); }
			while (true)
			{
				Thread.Sleep(1000);
			}

			Console.WriteLine("[END]");
			Console.Read();
		}

		private static void Connection_CallbackReceiveEvent(EasyTCP.Packets.BasePacket packet)
		{
			Console.WriteLine($"[CLIENT] {packet}");
		}
	}
}