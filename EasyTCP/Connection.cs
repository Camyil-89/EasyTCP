using EasyTCP.Packets;
using EasyTCP.Serialize;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace EasyTCP
{
	public class WaitInfoPacket
	{
		public DateTime Time { get; set; } = DateTime.Now;
		public bool IsReadFromServer { get; set; } = false;
		public int Timeout { get; set; }
		public Packet Packet { get; set; } = null;
		public Stopwatch Stopwatch { get; set; } = Stopwatch.StartNew();
		public bool RSTStopwatch { get; set; } = false;
		public List<ReceiveInfo> ReceiveServer { get; set; } = new List<ReceiveInfo>();
		public List<ReceiveInfo> ReceiveClient { get; set; } = new List<ReceiveInfo>();
	}
	public enum TypeConnection : byte
	{
		Client = 0,
		Server = 1,
	}
	public class Connection
	{
		private Dictionary<int, WaitInfoPacket> WaitPackets = new Dictionary<int, WaitInfoPacket>();
		private byte[] Buffer;
		public NetworkStream NetworkStream { get; set; }
		public TypeConnection Mode { get; private set; } = TypeConnection.Client;
		public ISerialization Serialization { get; set; } = new StandardSerialize();
		public Firewall.IFirewall Firewall { get; set; } = null;
		public Statistics Statistics { get; private set; } = new Statistics();
		public int BlockSizeForSendInfoReceive { get; set; } = 1024 * 1024;

		public delegate void CallbackReceive(Packet packet);
		public event CallbackReceive CallbackReceiveEvent;
		public delegate void CallbackReceiveSerialization(Packet packet);
		public event CallbackReceiveSerialization CallbackReceiveSerializationEvent;

		public Connection(NetworkStream stream, TypeConnection mode, int read_timeout = 700, int write_timeout = 700)
		{
			NetworkStream = stream;
			NetworkStream.ReadTimeout = read_timeout;
			NetworkStream.WriteTimeout = write_timeout;
			Mode = mode;
		}

		public void Init()
		{
			Buffer = new byte[1024 * 64]; // 64 kb
			RXHandler();
			Serialization.InitConnection(this);
		}

		public async Task Send(object data, HeaderPacket header)
		{
			var raw = Serialization.Raw(data);
			await WriteStream(raw, header);
		}
		public async Task WriteStream(byte[] data, HeaderPacket header)
		{
			header.DataSize = data.LongLength;
			int structSize = Marshal.SizeOf(header);
			byte[] struct_bytes = new byte[structSize];

			IntPtr ptr = Marshal.AllocHGlobal(structSize);
			Marshal.StructureToPtr(header, ptr, false);
			Marshal.Copy(ptr, struct_bytes, 0, structSize);
			Marshal.FreeHGlobal(ptr);


			byte[] result = new byte[data.Length + struct_bytes.Length];
			struct_bytes.CopyTo(result, 0);
			data.CopyTo(result, struct_bytes.Length);
			try
			{
				await NetworkStream.WriteAsync(result);
				await NetworkStream.FlushAsync();
				Statistics.SentPackets++;
				Statistics.SentBytes += result.LongLength;
				//Console.WriteLine($"WR: {data.Length} | {result.Length}");
			}
			catch (Exception ex) { }
		}
		private async Task RXHandler()
		{
			int bytesRead = 0;

			while (NetworkStream != null && NetworkStream.CanWrite && NetworkStream.CanRead)
			{
				Packet readData = null;
				try
				{
					await _Read().ContinueWith(async task =>
					{
						if (task.Result == null)
							return;
						readData = task.Result;
						readData.CallbackAnswerEvent += ReadData_CallbackAnswerEvent;
						Statistics.ReceivedPackets++;

						//if (Firewall != null && Firewall.ValidatePacket(packet) == false)
						//{
						//	packet.Answer(new Firewall.PacketFirewall() { Answer = Firewall.ValidatePacketAnswer(packet), UID = packet.UID });
						//	return;
						//}
						if (WaitPackets.ContainsKey(readData.Header.UID))
						{
							if (readData.Header.Type == PacketType.RSTStopwatch)
							{
								Console.WriteLine($"[RST] {readData.Header.Type};{readData.Header.UID}");
								WaitPackets[readData.Header.UID].Stopwatch.Restart();
								WaitPackets[readData.Header.UID].RSTStopwatch = true;
							}
							else if (readData.Header.Type == PacketType.ReceiveInfo)
							{
								WaitPackets[readData.Header.UID].Stopwatch.Restart();
								WaitPackets[readData.Header.UID].RSTStopwatch = true;
								WaitPackets[readData.Header.UID].ReceiveServer.Add(Serialization.FromRaw<ReceiveInfo>(readData.Bytes));
							}
							else
								WaitPackets[readData.Header.UID].Packet = readData;
						}
						else
						{
							//Task.Run(() => { CallbackReceiveSerializationEvent?.Invoke(packet); });
							Task.Run(() => { CallbackReceiveEvent?.Invoke(readData); });
						}
					});
				}
				catch (Exception ex) { Console.WriteLine(ex); Thread.Sleep(25); }
			}
			Console.WriteLine("END [RXHandler]");
		}

		private void ReadData_CallbackAnswerEvent(Packet packet)
		{
			if (Mode == TypeConnection.Server)
			{
				packet.Header.Mode = PacketMode.Hidden;
			}
			WriteStream(packet.Bytes, packet.Header).Wait();
		}

		private async Task<Packet> _Read()
		{
			int structSize = Marshal.SizeOf(typeof(HeaderPacket));
			byte[] headerBuffer = new byte[structSize];
			int bytesRead = await NetworkStream.ReadAsync(headerBuffer, 0, structSize).ConfigureAwait(false);

			Statistics.ReceivedBytes += bytesRead;
			if (bytesRead < structSize)
			{
				return null;
			}

			IntPtr ptr = Marshal.AllocHGlobal(structSize);
			Marshal.Copy(headerBuffer, 0, ptr, structSize);
			HeaderPacket header = (HeaderPacket)Marshal.PtrToStructure(ptr, typeof(HeaderPacket));
			Marshal.FreeHGlobal(ptr);

			long totalBytesRead = 0;

			if (Firewall != null && Firewall.ValidateHeader(header) == false)
			{
				await Send(Firewall.ValidateHeaderAnswer(header), HeaderPacket.CreateFirewallAnswer(header.UID));
				while (totalBytesRead < header.DataSize)
				{
					var buffer_size = (int)(Buffer.Length <= header.DataSize - totalBytesRead ? Buffer.Length : header.DataSize - totalBytesRead);
					bytesRead = await NetworkStream.ReadAsync(Buffer, 0, buffer_size).ConfigureAwait(false);
					totalBytesRead += bytesRead;
					Statistics.ReceivedBytes += bytesRead;
				}
				return null;
			}
			int bytes_read_to_send_info = 0;
			var read_data = new Packet() { Header = header };
			HeaderPacket header_rec_info = HeaderPacket.Create(PacketType.ReceiveInfo, PacketMode.ReceiveInfo);
			header_rec_info.UID = header.UID;
			ReceiveInfo receiveInfo = new ReceiveInfo();

			using (MemoryStream ms = new MemoryStream())
			{
				while (totalBytesRead < header.DataSize)
				{
					var buffer_size = (int)(Buffer.LongLength <= header.DataSize - totalBytesRead ? Buffer.Length : header.DataSize - totalBytesRead);
					bytesRead = await NetworkStream.ReadAsync(Buffer, 0, buffer_size).ConfigureAwait(false);
					totalBytesRead += bytesRead;
					bytes_read_to_send_info += bytesRead;
					ms.Write(Buffer, 0, bytesRead);
					if (WaitPackets.ContainsKey(header.UID) && header.Mode != PacketMode.ReceiveInfo && bytes_read_to_send_info >= BlockSizeForSendInfoReceive)
					{
						bytes_read_to_send_info = 0;
						WaitPackets[header.UID].RSTStopwatch = true;
						WaitPackets[header.UID].IsReadFromServer = true;
						WaitPackets[header.UID].ReceiveClient.Add(new ReceiveInfo { Receive = totalBytesRead, TotalNeedReceive = header.DataSize });
						WaitPackets[header.UID].Stopwatch.Restart();
					}
					if (header.Mode == PacketMode.Info && bytes_read_to_send_info >= BlockSizeForSendInfoReceive)
					{
						bytes_read_to_send_info = 0;
						receiveInfo.Receive = totalBytesRead;
						receiveInfo.TotalNeedReceive = header.DataSize;
						Send(receiveInfo, header_rec_info);
					}
					Statistics.ReceivedBytes += bytesRead;
				}
				if (WaitPackets.ContainsKey(header.UID) && header.Mode != PacketMode.ReceiveInfo)
				{
					WaitPackets[header.UID].RSTStopwatch = true;
					WaitPackets[header.UID].IsReadFromServer = true;
					receiveInfo.Receive = totalBytesRead;
					receiveInfo.TotalNeedReceive = header.DataSize;
					WaitPackets[header.UID].ReceiveClient.Add(new ReceiveInfo() { Receive = totalBytesRead, TotalNeedReceive = header.DataSize });
					WaitPackets[header.UID].Stopwatch.Restart();
				}
				if (header.Mode == PacketMode.Info)
				{
					receiveInfo.Receive = totalBytesRead;
					receiveInfo.TotalNeedReceive = header.DataSize;
					Send(receiveInfo, header_rec_info);
				}


				if (totalBytesRead < header.DataSize)
				{
					//Console.WriteLine($"ERROR READ");
					return null;
				}
				read_data.Bytes = ms.ToArray();
				if (Firewall != null && Firewall.ValidateRaw(read_data.Bytes))
				{
					return read_data;
				}
				if (Firewall == null)
					return read_data;
				await Send(Firewall.ValidateRawAnswer(read_data.Bytes), HeaderPacket.CreateFirewallAnswer(header.UID));
				//Console.WriteLine($"[CONNECTION FIREWALL] ValidateHeader");
				return null;
			}
		}

		public WaitInfoPacket SendAndWaitUnlimited(object data, PacketType type = PacketType.None, PacketMode mode = PacketMode.Hidden, byte type_packet = 0)
		{
			var wait_info_packet = new WaitInfoPacket() { };

			var header = HeaderPacket.Create(type, mode);
			header.TypePacket = type_packet;

			WaitPackets.Add(header.UID, wait_info_packet);
			Send(data, header);

			return wait_info_packet;
		}
	}
}
