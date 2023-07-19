using EasyTCP.Packets;
using EasyTCP.Serialize;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
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
		public BasePacket Packet { get; set; } = null;
		public Stopwatch Stopwatch { get; set; } = Stopwatch.StartNew();
		public bool RSTStopwatch { get; set; } = false;
		public List<PacketReceiveInfo> ReceiveServer { get; set; } = new List<PacketReceiveInfo>();
		public List<PacketReceiveInfo> ReceiveClient { get; set; } = new List<PacketReceiveInfo>();
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
		public int BlockSizeForSendInfoReceive { get; set; } = 1024 * 1024;

		public delegate void CallbackReceive(BasePacket packet);
		public event CallbackReceive CallbackReceiveEvent;
		public delegate void CallbackReceiveSerialization(BasePacket packet);
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
		public async Task Send(BasePacket packet, PacketMode mode = PacketMode.Hidden)
		{
			byte[] raw = new byte[0];
			raw = Serialization.Raw(packet);
			await WriteStream(raw, packet.UID, packet.Type == TypePacket.ReceiveInfo ? PacketMode.ReceiveInfo: mode);
		}
		public async Task WriteStream(byte[] data, int uid, PacketMode mode = PacketMode.Hidden)
		{
			var header = HeaderPacket.Create(data.Length, uid, mode);

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
				//Statistics.TXBytes += result.LongLength;
			}
			catch (Exception ex) { }
		}
		private async Task RXHandler()
		{
			int bytesRead = 0;

			while (NetworkStream != null && NetworkStream.CanWrite && NetworkStream.CanRead)
			{
				Packets.BasePacket packet = null;
				try
				{
					await _Read().ContinueWith(async task =>
					{
						if (task.Result == null)
							return;
						packet = Serialization.FromRaw<BasePacket>(task.Result);
						if (Firewall != null && Firewall.ValidatePacket(packet) == false)
						{
							packet.Answer(new Firewall.PacketFirewall() { Answer = Firewall.ValidatePacketAnswer(packet), UID = packet.UID });
							return;
						}
						packet.CallbackAnswerEvent += Packet_CallbackAnswerEvent;
						if (WaitPackets.ContainsKey(packet.UID))
						{
							if (packet.Type == TypePacket.RSTStopwatch)
							{
								Console.WriteLine($"[RST] {packet.Type};{packet.UID}");
								WaitPackets[packet.UID].Stopwatch.Restart();
								WaitPackets[packet.UID].RSTStopwatch = true;
							}
							else if (packet.Type == TypePacket.ReceiveInfo)
							{
								WaitPackets[packet.UID].ReceiveServer.Add((PacketReceiveInfo)packet);
								WaitPackets[packet.UID].Stopwatch.Restart();
								WaitPackets[packet.UID].RSTStopwatch = true;
							}
							else
								WaitPackets[packet.UID].Packet = packet;
						}
						else
						{
							Task.Run(() => { CallbackReceiveSerializationEvent?.Invoke(packet); });
							Task.Run(() => { CallbackReceiveEvent?.Invoke(packet); });
						}
					});
				}
				catch (Exception ex) { Console.WriteLine(ex); Thread.Sleep(25); }
			}
			Console.WriteLine("END [RXHandler]");
		}

		private async Task<byte[]> _Read()
		{
			int structSize = Marshal.SizeOf(typeof(HeaderPacket));
			byte[] headerBuffer = new byte[structSize];
			int bytesRead = await NetworkStream.ReadAsync(headerBuffer, 0, structSize).ConfigureAwait(false);

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
				await Send(new Firewall.PacketFirewall() { UID = header.UID, Answer = Firewall.ValidateHeaderAnswer(header) });
				while (totalBytesRead < header.DataSize)
				{
					var buffer_size = (int)(Buffer.Length <= header.DataSize - totalBytesRead ? Buffer.Length : header.DataSize - totalBytesRead);
					bytesRead = await NetworkStream.ReadAsync(Buffer, 0, buffer_size).ConfigureAwait(false);
					totalBytesRead += bytesRead;
					//Statistics.RXBytes += bytesRead;
				}
				return null;
			}
			int bytes_read_to_send_info = 0;
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
						WaitPackets[header.UID].ReceiveClient.Add(new PacketReceiveInfo() { Receive = totalBytesRead, TotalNeedReceive = header.DataSize});
						WaitPackets[header.UID].Stopwatch.Restart();
					}
					if (header.Mode == PacketMode.Info && bytes_read_to_send_info >= BlockSizeForSendInfoReceive) // позволяет уменьшить накладные расходы.
					{
						bytes_read_to_send_info = 0;
						Send(new PacketReceiveInfo() { UID = header.UID, Receive = totalBytesRead, TotalNeedReceive = header.DataSize });
					}
					//Statistics.RXBytes += bytesRead;
				}

				if (WaitPackets.ContainsKey(header.UID) && header.Mode != PacketMode.ReceiveInfo)
				{
					WaitPackets[header.UID].RSTStopwatch = true;
					WaitPackets[header.UID].IsReadFromServer = true;
					WaitPackets[header.UID].ReceiveClient.Add(new PacketReceiveInfo() { Receive = totalBytesRead, TotalNeedReceive = header.DataSize });
					WaitPackets[header.UID].Stopwatch.Restart();
				}
				if (header.Mode == PacketMode.Info) // позволяет уменьшить накладные расходы.
				{
					Send(new PacketReceiveInfo() { UID = header.UID, Receive = totalBytesRead, TotalNeedReceive = header.DataSize });
				}


				if (totalBytesRead < header.DataSize)
				{
					//Console.WriteLine($"ERROR READ");
					return null;
				}
				var data = ms.ToArray();
				if (Firewall != null && Firewall.ValidateRaw(data))
				{
					return data;
				}
				if (Firewall == null)
					return data;
				await Send(new Firewall.PacketFirewall() { UID = header.UID, Answer = Firewall.ValidateRawAnswer(data) });
				//Console.WriteLine($"[CONNECTION FIREWALL] ValidateHeader");
				return null;
			}
		}
		public WaitInfoPacket SendAndWaitUnlimited(BasePacket packet, PacketMode mode = PacketMode.Hidden)
		{
			var wait_info_packet = new WaitInfoPacket() { };
			WaitPackets.Add(packet.UID, wait_info_packet);
			Send(packet, mode);

			return wait_info_packet;
		}

		private void Packet_CallbackAnswerEvent(BasePacket packet)
		{
			Send(packet).Wait();
		}
	}
}
