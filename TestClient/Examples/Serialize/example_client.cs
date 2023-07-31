using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestClient.Examples.EntityManager;

namespace TestClient.Examples.Serialize
{
	[Serializable]
	public class SecureSerializeTestPacket
	{
		public string Message { get; set; } = "SecureSerializeTestPacket is work!!!!";
		public override string ToString()
		{
			return $"{base.ToString()} | {Message}";
		}
	}
	internal class example_client
	{
		private byte[] public_key = new byte[] { 48, 129, 137, 2, 129, 129, 0, 190, 198, 163, 196, 175, 81, 66, 182, 20, 140, 114, 1, 108, 35, 34, 183, 218, 29, 147, 84, 95, 129, 32, 77, 234, 131, 103, 224, 174, 210, 156, 62, 121, 169, 42, 139, 229, 117, 179, 137, 199, 184, 98, 212, 254, 93, 198, 65, 195, 56, 185, 142, 90, 195, 163, 27, 196, 227, 202, 183, 245, 124, 115, 171, 137, 131, 195, 253, 46, 188, 83, 252, 138, 9, 9, 130, 83, 98, 202, 240, 91, 111, 71, 30, 107, 81, 196, 10, 173, 178, 17, 1, 207, 157, 148, 187, 147, 154, 111, 103, 80, 106, 138, 36, 167, 133, 146, 165, 95, 201, 248, 218, 242, 212, 75, 37, 77, 70, 46, 39, 21, 26, 126, 213, 209, 170, 229, 57, 2, 3, 1, 0, 1 };
		public void Example()
		{
			EasyTCP.Client client = new EasyTCP.Client();

			var sec = new SecureSerialize();
			sec.PublicKey = public_key;

			client.PacketEntityManager.RegistrationPacket<SecureSerializeTestPacket>(1);
			client.Connect("localhost", 2020, sec);
			Console.WriteLine("ASD");
			while (true)
			{
				Console.WriteLine(client.SendAndWaitResponse<SecureSerializeTestPacket>(new SecureSerializeTestPacket()));
				Thread.Sleep(1);
			}
		}
	}
}
