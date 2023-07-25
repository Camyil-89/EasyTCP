using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TestClient.Examples.Simple;

namespace TestClient.Examples.Ssl
{
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
}
