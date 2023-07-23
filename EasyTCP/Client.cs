using EasyTCP.Firewall;
using EasyTCP.Packets;
using EasyTCP.Serialize;
using EasyTCP.Utilities;
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
	public class ResponseInfo<T>
	{
		public T Packet { get; set; }

		public ReceiveInfo Info { get; set; }

		public bool ReceiveFromServer = false;

        public ResponseInfo()
        {
			object obj = null;
			Packet = (T)obj;
        }
    }

	public class Client
	{
		public Connection Connection { get; private set; }
		public PacketEntityManager PacketEntityManager { get; private set; } = new PacketEntityManager();
		private TcpClient TCPClient { get; set; }
		public bool Connect(string host, int port, ISerialization serialization = null)
		{
			try
			{
				TCPClient = new TcpClient();
				TCPClient.Connect(host, port);
				Connection = new Connection(TCPClient.GetStream(), TypeConnection.Client, 700, 700);

				if (serialization != null)
					Connection.Serialization = serialization;

				Connection.Init();
				Connection.CallbackReceiveEvent += Connection_CallbackReceiveEvent;
				return true;
			}
			catch { return false; }
		}

		private void Connection_CallbackReceiveEvent(Packet packet)
		{
			if (packet.Header.Type == PacketType.None &&
				PacketEntityManager.IsEntity(packet.Header.TypePacket) != 0)
			{
				PacketEntityManager.ReceivePacket(packet, Connection.Serialization);
				return;
			}
		}

		public IEnumerable<ResponseInfo<T>> SendAndReceiveInfo<T>(T obj, int timeout = int.MaxValue)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			var info = Connection.SendAndWaitUnlimited(obj, PacketType.None, PacketMode.Info);
			ReceiveInfo last_rec_info = new ReceiveInfo();
			int count_server = 0;
			int count_client = 0;
			while (stopwatch.ElapsedMilliseconds < timeout)
			{
				if (TCPClient.Connected == false || Connection.NetworkStream == null)
				{
					throw new ExceptionEasyTCPAbortConnect("Lost connect with server!");
				}
				if (info.Packet != null)
				{
					if (info.Packet.Header.Type == PacketType.FirewallBlock)
					{
						throw new ExceptionEasyTCPFirewall("");
					}
					yield return new ResponseInfo<T>() { Packet = Connection.Serialization.FromRaw<T>(info.Packet.Bytes), Info = last_rec_info };
					break;
				}
				else if (count_server != info.ReceiveServer.Count && info.IsReadFromServer == false)
				{
					last_rec_info = info.ReceiveServer.Last();
					count_server++;
					yield return new ResponseInfo<T>() { Info = last_rec_info };
				}
				else if (count_client != info.ReceiveClient.Count && info.IsReadFromServer == true)
				{
					last_rec_info = info.ReceiveClient.Last();
					count_client++;
					yield return new ResponseInfo<T>() { Info = last_rec_info, ReceiveFromServer = true };
				}
				Thread.Sleep(1);
			}
			if (info.Packet == null)
				throw new ExceptionEasyTCPTimeout($"Timeout wait response! {stopwatch.ElapsedMilliseconds} \\ {timeout}");
		}
		public T SendAndWaitResponse<T>(T obj, int timeout = int.MaxValue)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();


			var info = Connection.SendAndWaitUnlimited(obj, PacketType.None, PacketMode.Hidden, PacketEntityManager.IsEntity(typeof(T)));
			while (stopwatch.ElapsedMilliseconds < timeout)
			{
				if (TCPClient.Connected == false || Connection.NetworkStream == null)
				{
					throw new ExceptionEasyTCPAbortConnect("Lost connect with server!");
				}
				if (info.Packet != null)
					return Connection.Serialization.FromRaw<T>(info.Packet.Bytes);
				Thread.Sleep(1);
			}
			throw new ExceptionEasyTCPTimeout($"Timeout wait response! {stopwatch.ElapsedMilliseconds} \\ {timeout}");
		}
	}
}
