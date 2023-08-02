using EasyTCP.Packets;
using EasyTCP.Serialize;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
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
		public bool IsReadFromServer { get; set; } = false;
		public int Timeout { get; set; }
		public Packet Packet { get; set; } = null;
		public Stopwatch Stopwatch { get; set; } = Stopwatch.StartNew();
		public bool RSTStopwatch { get; set; } = false;
		public ReceiveInfo ReceiveServer { get; set; }
		public ReceiveInfo ReceiveClient { get; set; }
	}
	public enum TypeConnection : byte
	{
		Client = 0,
		Server = 1,
	}
	public enum TypeStreamConnection : byte
	{
		NotEncrypted = 0,
		Encrypted = 1,
	}
	public class Connection
	{
		private Dictionary<int, WaitInfoPacket> WaitPackets = new Dictionary<int, WaitInfoPacket>();
		private NetworkStream NetworkStream { get; set; }
		private SslStream SslStream { get; set; } = null;
		private byte[] Buffer { get; set; } = new byte[1024 * 512];

		/// <summary>
		/// Client
		/// </summary>
		public string ServerName { get; set; }
		/// <summary>
		/// Client
		/// </summary>
		public int PortServer { get; set; }
		public TypeStreamConnection TypeStreamConnection { get; set; }
		public bool IsWork => NetworkStream != null || SslStream != null;
		public ServerClient ServerClient { get; set; } = null;
		public TypeConnection Mode { get; private set; } = TypeConnection.Client;
		public ISerialization Serialization { get; set; } = new StandardSerialize();
		public Firewall.IFirewall Firewall { get; set; } = null;
		public Statistics Statistics { get; private set; } = new Statistics();
		public int BlockSizeForSendInfoReceive { get; set; } = 1024 * 1024; // 1 mb

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
		public void EnableSsl(X509Certificate certificate, bool CheckCert = true)
		{
			X509Certificate2Collection clientCertificates = new X509Certificate2Collection();
			clientCertificates.Add(certificate);
			TypeStreamConnection = TypeStreamConnection.Encrypted;
			if (CheckCert)
				SslStream = new SslStream(NetworkStream);
			else
				SslStream = new SslStream(NetworkStream, false, (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true);
			SslStream.ReadTimeout = NetworkStream.ReadTimeout;
			SslStream.WriteTimeout = NetworkStream.WriteTimeout;
			if (Mode == TypeConnection.Client)
			{
				SslStream.AuthenticateAsClient(ServerName, clientCertificates, System.Security.Authentication.SslProtocols.Tls12
					| System.Security.Authentication.SslProtocols.Tls |
					System.Security.Authentication.SslProtocols.Tls11 |
					System.Security.Authentication.SslProtocols.Tls13, false);
				if (SslStream.IsAuthenticated == false)
				{
					throw new ExceptionEasyTCPSslNotAuthenticated("Ssl not Authenticated");
				}
				if (SslStream.IsEncrypted == false)
				{
					throw new ExceptionEasyTCPSslNotAuthenticated("Ssl not Encrypted");
				}
			}
			else
			{
				SslStream.AuthenticateAsServer(certificate, false, System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls |
					System.Security.Authentication.SslProtocols.Tls11 |
					System.Security.Authentication.SslProtocols.Tls13, false);
			}

		}
		public void InitSerialization()
		{
			Serialization.InitConnection(this);
		}
		public void Init()
		{
			RXHandler();
		}

		public async Task Send(object data, HeaderPacket header)
		{
			try
			{
				var raw = Serialization.Raw(data);
				await WriteStream(raw, header);
			}
			catch (Exception e) { }

		}
		public async Task WriteStream(byte[] data, HeaderPacket header)
		{
			if (data == null)
				data = new byte[0];
			header.DataSize = data.Length;
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
				ReceiveInfo receiveInfo = new ReceiveInfo() { TotalNeedReceive = result.Length };
				int bytes_read_to_send_info = 0;

				for (int offset = 0; offset < result.Length; offset += Buffer.Length)
				{
					int remainingBytes = result.Length - offset;
					int bytesToSend = Math.Min(Buffer.Length, remainingBytes);
					if (TypeStreamConnection == TypeStreamConnection.NotEncrypted)
					{
						await NetworkStream.WriteAsync(result, offset, bytesToSend);
					}
					else
					{
						await SslStream.WriteAsync(result, offset, bytesToSend);
					}
					receiveInfo.Receive += bytesToSend;
					bytes_read_to_send_info += bytesToSend;
					Statistics.SentBytes += bytesToSend;
					Statistics.UpdateSent();
					if (WaitPackets.ContainsKey(header.UID) && header.Mode == PacketMode.Info && bytes_read_to_send_info >= BlockSizeForSendInfoReceive)
					{
						bytes_read_to_send_info = 0;
						WaitPackets[header.UID].RSTStopwatch = true;
						WaitPackets[header.UID].ReceiveServer = new ReceiveInfo { Receive = receiveInfo.Receive, TotalNeedReceive = receiveInfo.TotalNeedReceive };
						WaitPackets[header.UID].Stopwatch.Restart();
					}
				}
				if (TypeStreamConnection == TypeStreamConnection.NotEncrypted)
				{
					await NetworkStream.FlushAsync();
				}
				else
				{
					await SslStream.FlushAsync();
				}
				Statistics.SentPackets++;
				//Console.WriteLine($"TX: {data.Length} | {result.Length} ({header.UID}) {header.TypePacket} {header.Type}");
			}
			catch (Exception ex) {  }
		}
		public async Task<Packet> WaitPacketConnection()
		{
			Packet pakcet = null;
			while (true)
			{
				pakcet = null;
				try
				{
					var packet = await _Read();
					if (packet != null)
					{
						packet.CallbackAnswerEvent += ReadData_CallbackAnswerEvent;
						return packet;
					}

				}
				catch { }
			}
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
						//Console.WriteLine($"packet: {readData.Header.UID} | {readData.Header.Type} | {readData.Header.TypePacket}");
						if (readData.Header.Type == PacketType.Abort)
						{
							NetworkStream.Close();
							NetworkStream = null;
							return;
						}
						else if (WaitPackets.ContainsKey(readData.Header.UID))
						{
							if (readData.Header.Type == PacketType.RSTStopwatch)
							{
								WaitPackets[readData.Header.UID].Stopwatch.Restart();
								WaitPackets[readData.Header.UID].RSTStopwatch = true;
							}
							else if (readData.Header.Type == PacketType.ReceiveInfo)
							{
								WaitPackets[readData.Header.UID].Stopwatch.Restart();
								WaitPackets[readData.Header.UID].RSTStopwatch = true;
								WaitPackets[readData.Header.UID].ReceiveServer = Serialization.FromRaw<ReceiveInfo>(readData.Bytes);
							}
							else
								WaitPackets[readData.Header.UID].Packet = readData;
						}
						else if (readData.Header.Type == PacketType.Serialize)
						{
							await Task.Run(() => { CallbackReceiveSerializationEvent?.Invoke(readData); });
						}
						else if (readData.Header.Type == PacketType.Ping)
						{
							readData.Answer(readData);
						}
						else
						{
							await Task.Run(() => { CallbackReceiveEvent?.Invoke(readData); });
						}
					});
				}
				catch (Exception ex) { Thread.Sleep(25); }
			}
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

			int bytesRead = 0;
			if (TypeStreamConnection == TypeStreamConnection.NotEncrypted)
				bytesRead = await NetworkStream.ReadAsync(headerBuffer, 0, structSize).ConfigureAwait(false);
			else
				bytesRead = await SslStream.ReadAsync(headerBuffer, 0, structSize).ConfigureAwait(false);

			Statistics.ReceivedBytes += bytesRead;
			if (bytesRead < structSize)
			{
				return null;
			}

			IntPtr ptr = Marshal.AllocHGlobal(structSize);
			Marshal.Copy(headerBuffer, 0, ptr, structSize);
			HeaderPacket header = (HeaderPacket)Marshal.PtrToStructure(ptr, typeof(HeaderPacket));
			Marshal.FreeHGlobal(ptr);

			//Console.WriteLine($"RX: {header.DataSize} | {header.Type} | {header.Mode} | {header.TypePacket} | ({header.UID})");
			long totalBytesRead = 0;

			if (Firewall != null && Firewall.ValidateHeader(header) == false)
			{
				await Send(Firewall.ValidateHeaderAnswer(header), HeaderPacket.CreateFirewallAnswer(header.UID));
				while (totalBytesRead < header.DataSize)
				{
					var buffer_size = (int)(Buffer.Length <= header.DataSize - totalBytesRead ? Buffer.Length : header.DataSize - totalBytesRead);
					if (TypeStreamConnection == TypeStreamConnection.NotEncrypted)
						bytesRead = await NetworkStream.ReadAsync(Buffer, 0, buffer_size).ConfigureAwait(false);
					else
						bytesRead = await SslStream.ReadAsync(Buffer, 0, buffer_size).ConfigureAwait(false);
					totalBytesRead += bytesRead;
					Statistics.ReceivedBytes += bytesRead;
				}
				return null;
			}
			long bytes_read_to_send_info = 0;
			var read_data = new Packet() { Header = header };
			read_data.Client = ServerClient;
			HeaderPacket header_rec_info = HeaderPacket.Create(PacketType.ReceiveInfo, PacketMode.Hidden);
			header_rec_info.UID = header.UID;
			ReceiveInfo receiveInfo = new ReceiveInfo();

			using (MemoryStream ms = new MemoryStream())
			{
				while (totalBytesRead < header.DataSize)
				{
					var buffer_size = (int)(Buffer.Length <= header.DataSize - totalBytesRead ? Buffer.Length : header.DataSize - totalBytesRead);
					if (TypeStreamConnection == TypeStreamConnection.NotEncrypted)
						bytesRead = await NetworkStream.ReadAsync(Buffer, 0, buffer_size).ConfigureAwait(false);
					else
						bytesRead = await SslStream.ReadAsync(Buffer, 0, buffer_size).ConfigureAwait(false);
					totalBytesRead += bytesRead;
					bytes_read_to_send_info += bytesRead;
					ms.Write(Buffer, 0, bytesRead);
					if (WaitPackets.ContainsKey(header.UID) && header.Type != PacketType.ReceiveInfo && bytes_read_to_send_info >= BlockSizeForSendInfoReceive)
					{
						bytes_read_to_send_info = 0;
						WaitPackets[header.UID].RSTStopwatch = true;
						WaitPackets[header.UID].IsReadFromServer = true;
						WaitPackets[header.UID].ReceiveClient = new ReceiveInfo { Receive = totalBytesRead, TotalNeedReceive = header.DataSize };
						WaitPackets[header.UID].Stopwatch.Restart();
					}
					Statistics.ReceivedBytes += bytesRead;
					Statistics.UpdateReceived();
				}
				if (WaitPackets.ContainsKey(header.UID) && header.Type != PacketType.ReceiveInfo)
				{
					WaitPackets[header.UID].RSTStopwatch = true;
					WaitPackets[header.UID].IsReadFromServer = true;
					receiveInfo.Receive = totalBytesRead;
					receiveInfo.TotalNeedReceive = header.DataSize;
					WaitPackets[header.UID].ReceiveClient = new ReceiveInfo() { Receive = totalBytesRead, TotalNeedReceive = header.DataSize };
					WaitPackets[header.UID].Stopwatch.Restart();
				}


				if (totalBytesRead < header.DataSize)
				{
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
				return null;
			}
		}
		public void Abort()
		{
			if (NetworkStream != null)
			{
				var header = HeaderPacket.Create(PacketType.Abort);
				Send(null, header).Wait();
				NetworkStream.Close();
				NetworkStream = null;
			}
		}
		public void Send(object data, PacketType type = PacketType.None, PacketMode mode = PacketMode.Hidden, ushort type_packet = 0)
		{
			var header = HeaderPacket.Create(type, mode);
			header.TypePacket = type_packet;
			Send(data, header).Wait();
		}
		public WaitInfoPacket SendAndWaitUnlimited(object data, PacketType type = PacketType.None, PacketMode mode = PacketMode.Hidden, ushort type_packet = 0)
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
