using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyTCP
{
	public class Statistics
	{
		public long ReceivedBytes { get; set; } = 0;
		public long SentBytes { get; set; } = 0;
		public int ReceivedPackets { get; set; } = 0;
		public int SentPackets { get; set; }
		public override string ToString()
		{
			return string.Format("|   RX   |   TX   |RXPacket|TXPacket|\n" +
								 "|{0,8}|{1,8}|{2,8}|{3,8}|",
								 ReceivedBytes, SentBytes, ReceivedPackets, SentPackets);
		}
	}
}
