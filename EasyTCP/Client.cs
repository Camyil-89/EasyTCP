using EasyTCP.Firewall;
using EasyTCP.Packets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EasyTCP
{
	public class ExceptionEasyTCPFirewall : Exception
	{
		public ExceptionEasyTCPFirewall(string message)
		: base(message)
		{
		}

		public ExceptionEasyTCPFirewall(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
	public class ExceptionEasyTCPAbortConnect : Exception
	{
		public ExceptionEasyTCPAbortConnect(string message)
		: base(message)
		{
		}

		public ExceptionEasyTCPAbortConnect(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
	public class ExceptionEasyTCPTimeout : Exception
	{
		public ExceptionEasyTCPTimeout(string message)
		: base(message)
		{
		}

		public ExceptionEasyTCPTimeout(string message, Exception inner)
			: base(message, inner)
		{
		}
	}
	public class ResponseInfo
	{
		public BasePacket Packet { get; set; } = null;

		public PacketReceiveInfo Info { get; set; }
	}

	public class Client
	{
		public Connection Connection { get; private set; }
		private TcpClient TCPClient { get; set; }
		public bool Connect(string host, int port)
		{
			try
			{
				TCPClient = new TcpClient();
				TCPClient.Connect(host, port);
				Connection = new Connection(TCPClient.GetStream(), 700, 700);
				Connection.Init();
				return true;
			}
			catch { return false; }
		}

		public IEnumerable<ResponseInfo> SendAndReceiveInfo(BasePacket packet, int timeout = int.MaxValue)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			var info = Connection.SendAndWaitUnlimited(packet, PacketMode.Info);
			PacketReceiveInfo last_rec_info = new PacketReceiveInfo() { Receive = 0, TotalNeedReceive = 0 };
			int count = 0;
			while (stopwatch.ElapsedMilliseconds < timeout)
			{
				if (TCPClient.Connected == false || Connection.NetworkStream == null)
				{
					throw new ExceptionEasyTCPAbortConnect("Lost connect with server!");
				}
				if (info.Packet != null)
				{
					if (info.Packet is PacketFirewall)
						throw new ExceptionEasyTCPFirewall(((PacketFirewall)info.Packet).Answer);
					yield return new ResponseInfo() { Packet = info.Packet, Info = last_rec_info };
					break;
				}
				else if (count != info.ReceiveInfo.Count)
				{
					last_rec_info = info.ReceiveInfo.Last();
					count = info.ReceiveInfo.Count;
					yield return new ResponseInfo() { Info = last_rec_info };
				}
				Thread.Sleep(1);
			}
			if (info.Packet == null)
				throw new ExceptionEasyTCPTimeout($"Timeout wait response! {stopwatch.ElapsedMilliseconds} \\ {timeout}");
		}

		public BasePacket SendAndWaitResponse(BasePacket packet, int timeout = int.MaxValue)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			var info = Connection.SendAndWaitUnlimited(packet);
			while (stopwatch.ElapsedMilliseconds < timeout)
			{
				if (TCPClient.Connected == false || Connection.NetworkStream == null)
				{
					throw new ExceptionEasyTCPAbortConnect("Lost connect with server!");
				}
				if (info.Packet != null)
					return info.Packet;
				Thread.Sleep(1);
			}
			throw new ExceptionEasyTCPTimeout($"Timeout wait response! {stopwatch.ElapsedMilliseconds} \\ {timeout}");
		}
	}
}
