using EasyTCP.Firewall;
using EasyTCP.Packets;
using EasyTCP.Serialize;
using EasyTCP.Utilities;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace EasyTCP
{
	/// <summary>
	/// Информация о передачи пакета
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ResponseInfo<T>
	{
		/// <summary>
		/// Ответ от сервера
		/// </summary>
		public T Packet { get; set; }
		/// <summary>
		/// Информация о передачи
		/// </summary>
		public ReceiveInfo Info { get; set; }
		/// <summary>
		/// Информация о режиме передачи (либо на сервер отправляем, либо получаем от сервера)
		/// </summary>
		public bool ReceiveFromServer = false;

		public ResponseInfo()
		{
			object obj = null;
			Packet = (T)obj;
		}
	}
	public enum ConnectStatus : byte
	{
		Connecting = 0,
		WaitResponseFromServer = 1,
		FailConnectBlockFirewall = 2,
		NotFoundServer = 3,
		OK = 4,
		TimeoutConnectToServer = 5,
		InitConnect = 6,
		InitSerialization = 7,
		Fail = 8,
	}
	public class ConnectInfo
	{
		public ConnectStatus Status { get; set; } = ConnectStatus.Connecting;
		/// <summary>
		/// if Status == FailConnectBlockFirewall
		/// </summary>
		public PacketFirewall Firewall { get; set; }
	}

	public class Client
	{
		private X509Certificate Certificate;
		private bool IsSsl = false;
		private bool IsCheckCert = true;
		public TcpClient TCPClient { get; set; }
		/// <summary>
		/// Подключение к серверу (низкий уровень взаимодействия)
		/// </summary>
		public Connection Connection { get; set; }
		/// <summary>
		/// Менеджер пакетов.
		/// </summary>
		public PacketEntityManager PacketEntityManager { get; set; } = new PacketEntityManager();

		public delegate void CallbackReceiveFirewall(PacketFirewall packet);
		public event CallbackReceiveFirewall CallbackReceiveFirewallEvent;
		/// <summary>
		/// Подключение к серверу
		/// </summary>
		/// <param name="host">адрес сервера</param>
		/// <param name="port">порт сервера</param>
		/// <param name="serialization">интерфейс сериализации пакетов</param>
		/// <returns>bool</returns>
		public bool Connect(string host, int port, ISerialization serialization = null, int timeout = 5000)
		{

			foreach (var i in ConnectWithInfo(host, port, serialization, timeout))
			{
				switch (i.Status)
				{
					case ConnectStatus.Connecting:
						break;
					case ConnectStatus.WaitResponseFromServer:
						break;
					case ConnectStatus.FailConnectBlockFirewall:
						throw new ExceptionEasyTCPFirewall($"code: {i.Firewall.Code} | {i.Firewall.Answer}");
						break;
					case ConnectStatus.NotFoundServer:
						break;
					case ConnectStatus.OK:
						return true;
						break;
					case ConnectStatus.TimeoutConnectToServer:
						throw new TimeoutException();
						break;
					case ConnectStatus.InitConnect:
						break;
					case ConnectStatus.InitSerialization:
						break;
					case ConnectStatus.Fail:
						return false;
						break;
					default:
						break;
				}
			}
			return false;
		}
		public IEnumerable<ConnectInfo> ConnectWithInfo(string host, int port, ISerialization serialization = null, int timeout = 5000)
		{
			TCPClient = new TcpClient();
			yield return new ConnectInfo() { Status = ConnectStatus.Connecting };
			IAsyncResult ar = TCPClient.BeginConnect(host, port, null, null);
			if (!ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(timeout), false))
			{
				TCPClient.Close();
				yield return new ConnectInfo() { Status = ConnectStatus.TimeoutConnectToServer };
			}
			else
			{
				TCPClient.EndConnect(ar);
				yield return new ConnectInfo() { Status = ConnectStatus.InitConnect };
				Connection = new Connection(TCPClient.GetStream(), TypeConnection.Client, 700, 700);
				Connection.ServerName = host;
				Connection.PortServer = port;
				if (IsSsl)
					Connection.EnableSsl(Certificate, IsCheckCert);

				Connection.Init();
				Connection.CallbackReceiveEvent += Connection_CallbackReceiveEvent;
				yield return new ConnectInfo() { Status = ConnectStatus.WaitResponseFromServer };
				var answer = SendAndWaitResponse<PacketConnection>(PacketConnection.Create(PacketConnectionType.OK), PacketType.InitConnection, PacketMode.Hidden, 0, timeout);
				if (answer.Type == PacketConnectionType.Abort)
				{
					yield return new ConnectInfo() { Status = ConnectStatus.FailConnectBlockFirewall, Firewall = answer.Firewall };
				}
				else if (answer.Type == PacketConnectionType.OK)
				{
					yield return new ConnectInfo() { Status = ConnectStatus.InitSerialization };
					if (serialization != null)
						Connection.Serialization = serialization;
					Connection.InitSerialization();
					Connection.BlockSizeForSendInfoReceive = answer.BlockSize;
					yield return new ConnectInfo() { Status = ConnectStatus.OK };
				}
				else
				{
					yield return new ConnectInfo() { Status = ConnectStatus.Fail };
				}
			}
		}
		/// <summary>
		/// Закрывает подключение
		/// </summary>
		public void Close()
		{
			if (Connection != null)
			{
				Connection.Abort();
			}
			if (TCPClient != null)
			{
				TCPClient.Close();
				TCPClient.Dispose();
				TCPClient = null;
			}

		}
		private void CheckConnection()
		{
			if (TCPClient.Connected == false || Connection == null || Connection.IsWork == false)
			{
				throw new ExceptionEasyTCPAbortConnect("Lost connect with server!");
			}
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
		/// <summary>
		/// Включает Ssl шифрование
		/// </summary>
		/// <param name="certificate">сертификат</param>
		/// <param name="IsValidateCertificate">проверка валидности сертификата</param>
		public void EnableSsl(X509Certificate certificate, bool IsValidateCertificate)
		{
			IsSsl = true;
			Certificate = certificate;
			IsCheckCert = IsValidateCertificate;
		}
		/// <summary>
		/// Отправка пакета и ожидания получение ответа от сервера.
		/// Получает информацию о передачи пакета.
		/// </summary>
		/// <typeparam name="T">тип возвращаемого пакета</typeparam>
		/// <param name="obj">объект который нужно передать</param>
		/// <param name="timeout">время ожидания ответа</param>
		/// <returns>ResponseInfo<T></returns>
		/// <exception cref="ExceptionEasyTCPAbortConnect"></exception>
		/// <exception cref="ExceptionEasyTCPFirewall"></exception>
		/// <exception cref="ExceptionEasyTCPTimeout"></exception>
		public IEnumerable<ResponseInfo<T>> SendAndReceiveInfo<T>(T obj, int timeout = int.MaxValue)
		{
			CheckConnection();
			var info = Connection.SendAndWaitUnlimited(obj, PacketType.None, PacketMode.Info, PacketEntityManager.IsEntity(obj.GetType()));
			ReceiveInfo last_rec_info = new ReceiveInfo();
			int count_server = 0;
			int count_client = 0;
			while (info.Stopwatch.ElapsedMilliseconds < timeout)
			{
				if (TCPClient.Connected == false || Connection.IsWork == false)
				{
					throw new ExceptionEasyTCPAbortConnect("Lost connect with server!");
				}
				if (info.Packet != null)
				{
					if (info.Packet.Header.Type == PacketType.FirewallBlock)
					{
						var answer = Connection.Serialization.FromRaw<PacketFirewall>(info.Packet.Bytes);
						CallbackReceiveFirewallEvent?.Invoke(answer);
						throw new ExceptionEasyTCPFirewall($"code: {answer.Code} | {answer.Answer}");
					}
					yield return new ResponseInfo<T>() { Packet = Connection.Serialization.FromRaw<T>(info.Packet.Bytes), Info = last_rec_info };
					break;
				}
				else if (last_rec_info.Receive != info.ReceiveServer.Receive && info.IsReadFromServer == false)
				{
					last_rec_info = info.ReceiveServer;
					count_server++;
					yield return new ResponseInfo<T>() { Info = last_rec_info };
				}
				else if (last_rec_info.Receive != info.ReceiveClient.Receive && info.IsReadFromServer == true)
				{
					last_rec_info = info.ReceiveClient;
					count_client++;
					yield return new ResponseInfo<T>() { Info = last_rec_info, ReceiveFromServer = true };
				}
				if (info.Stopwatch.ElapsedMilliseconds >= 50)
					Thread.Sleep(1);
			}
			if (info.Packet == null)
				throw new ExceptionEasyTCPTimeout($"Timeout wait response! {info.Stopwatch.ElapsedMilliseconds} \\ {timeout}");
		}
		/// <summary>
		/// отправляет пакет на сервер и ждет ответ
		/// </summary>
		/// <typeparam name="T">тип возвращаемого пакета</typeparam>
		/// <param name="obj">пакет который нужно отправить</param>
		/// <param name="timeout">время ожидания</param>
		/// <returns>T</returns>
		/// <exception cref="ExceptionEasyTCPAbortConnect"></exception>
		/// <exception cref="ExceptionEasyTCPTimeout"></exception>
		public T SendAndWaitResponse<T>(object obj, int timeout = int.MaxValue)
		{
			return SendAndWaitResponse<T>(obj, PacketType.None, PacketMode.Hidden, PacketEntityManager.IsEntity(obj.GetType()), timeout);
		}
		public T SendAndWaitResponse<T>(object obj, PacketType type, PacketMode mode, ushort type_packet, int timeout = int.MaxValue)
		{
			CheckConnection();

			var info = Connection.SendAndWaitUnlimited(obj, type, mode, type_packet);
			while (info.Stopwatch.ElapsedMilliseconds < timeout)
			{
				if (TCPClient.Connected == false || Connection.IsWork == false)
				{
					throw new ExceptionEasyTCPAbortConnect("Lost connect with server!");
				}
				if (info.Packet != null && info.Packet.Header.Type == PacketType.FirewallBlock)
				{
					var answer = Connection.Serialization.FromRaw<PacketFirewall>(info.Packet.Bytes);
					CallbackReceiveFirewallEvent?.Invoke(answer);
					throw new ExceptionEasyTCPFirewall($"code: {answer.Code} | {answer.Answer}");
				}
				if (info.Packet != null)
					return Connection.Serialization.FromRaw<T>(info.Packet.Bytes);
				if (info.Stopwatch.ElapsedMilliseconds >= 50)
					Thread.Sleep(1);
			}
			throw new ExceptionEasyTCPTimeout($"Timeout wait response! {info.Stopwatch.ElapsedMilliseconds} \\ {timeout}");
		}
		/// <summary>
		/// Отправляет пакет и не ждет ответа
		/// </summary>
		/// <param name="obj">отправляет объект на сервер</param>
		public void Send(object obj)
		{
			CheckConnection();
			Connection.Send(obj, PacketType.None, PacketMode.Hidden, PacketEntityManager.IsEntity(obj.GetType()));
		}
	}
}
