using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestClient.Examples.Serialize;

namespace TestClient.Examples.Firewall
{
	[Serializable]
	public class BigPacket
	{
		public byte[] Bytes { get; set; }
	}
	internal class example_client
	{
		public void Example()
		{
			EasyTCP.Client client = new EasyTCP.Client();

			client.CallbackReceiveFirewallEvent += Client_CallbackReceiveFirewallEvent;

			client.Connect("localhost", 2020);


			try
			{
				Console.WriteLine(client.SendAndWaitResponse<BigPacket>(new BigPacket() { Bytes = new byte[700] }));
			}
			catch (Exception ex) { Console.WriteLine(ex); }
			
			try
			{
				Console.WriteLine(client.SendAndWaitResponse<BigPacket>(new BigPacket() { Bytes = new byte[1023] }));
			}
			catch (Exception ex) { Console.WriteLine(ex); }
			
			for (int i = 0; i < 10; i++)
			{
				EasyTCP.Client client1 = new EasyTCP.Client();
				client1.CallbackReceiveFirewallEvent += Client_CallbackReceiveFirewallEvent;
			
				client1.Connect("localhost", 2020);
			}
		}

		private void Client_CallbackReceiveFirewallEvent(EasyTCP.Packets.PacketFirewall packet)
		{
			Console.WriteLine($"[CLIENT FIREWALL BLOCK] {packet.Code} | {packet.Answer}");
		}
	}
}
