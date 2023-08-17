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
		public double InstanceSentBytesSpeed { get; set; } = 0;
		public double AverageSentBytesSpeed { get; set; } = 0;
		public double InstanceReceivedBytesSpeed { get; set; } = 0;
		public double AverageReceivedBytesSpeed { get; set; } = 0;

		private long lastSentBytes = 0;
		private long lastReceivedBytes = 0;
		private DateTime lastSentTime = DateTime.Now;
		private DateTime lastReceivedTime = DateTime.Now;
		private int count_sent = 0;
		private int count_received = 0;
		public override string ToString()
		{
			return string.Format("|   RX   |   TX   |RXPacket|TXPacket|\n" +
								 "|{0,8}|{1,8}|{2,8}|{3,8}|",
								 ReceivedBytes, SentBytes, ReceivedPackets, SentPackets);
		}
		public virtual void UpdateSent()
		{
			DateTime currentTime = DateTime.Now;

			long bytesTransferred = SentBytes - lastSentBytes;

			double timeElapsedSeconds = (currentTime - lastSentTime).TotalSeconds;

			InstanceSentBytesSpeed = bytesTransferred / timeElapsedSeconds;

			lastSentBytes = SentBytes;
			lastSentTime = currentTime;

			count_sent++;
			AverageSentBytesSpeed = (AverageSentBytesSpeed * (count_sent - 1) + InstanceSentBytesSpeed) / count_sent;
		}
		public virtual void UpdateReceived()
		{
			DateTime currentTime = DateTime.Now;

			long bytesTransferred = ReceivedBytes - lastReceivedBytes;

			double timeElapsedSeconds = (currentTime - lastReceivedTime).TotalSeconds;

			InstanceReceivedBytesSpeed = bytesTransferred / timeElapsedSeconds;

			lastReceivedBytes = ReceivedBytes;
			lastReceivedTime = currentTime;

			count_received++;
			AverageReceivedBytesSpeed = (AverageReceivedBytesSpeed * (count_received - 1) + InstanceReceivedBytesSpeed) / count_received;
		}
	}
}
