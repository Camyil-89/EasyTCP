using EasyTCP.Packets;
using EasyTCP.Serialize;
using EasyTCP.Utilities;
using System.Diagnostics;
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

	public class Client
	{
		private X509Certificate Certificate;
		private bool IsSsl = false;
		private bool IsCheckCert = true;
		private TcpClient TCPClient { get; set; }
		/// <summary>
		/// Подключение к серверу (низкий уровень взаимодействия)
		/// </summary>
		public Connection Connection { get; private set; }
		/// <summary>
		/// Менеджер пакетов.
		/// </summary>
		public PacketEntityManager PacketEntityManager { get; private set; } = new PacketEntityManager();
		/// <summary>
		/// Подключение к серверу
		/// </summary>
		/// <param name="host">адрес сервера</param>
		/// <param name="port">порт сервера</param>
		/// <param name="serialization">интерфейс сериализации пакетов</param>
		/// <returns>bool</returns>
		public bool Connect(string host, int port, ISerialization serialization = null)
		{
			try
			{
				TCPClient = new TcpClient();
				TCPClient.Connect(host, port);
				Connection = new Connection(TCPClient.GetStream(), TypeConnection.Client, 700, 700);
				Connection.ServerName = host;
				Connection.PortServer = port;
				if (IsSsl)
					Connection.EnableSsl(Certificate, IsCheckCert);
				if (serialization != null)
					Connection.Serialization = serialization;

				Connection.Init();
				Connection.CallbackReceiveEvent += Connection_CallbackReceiveEvent;
				return true;
			}
			catch { return false; }
		}
		/// <summary>
		/// Закрывает подключение
		/// </summary>
		public void Close()
		{
			Connection.Abort();
			TCPClient.Close();
			TCPClient.Dispose();
			TCPClient = null;
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
			var info = Connection.SendAndWaitUnlimited(obj, PacketType.None, PacketMode.Info);
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
						throw new ExceptionEasyTCPFirewall($"code: {answer.Code} | {answer.Answer}");
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
			CheckConnection();

			var info = Connection.SendAndWaitUnlimited(obj, PacketType.None, PacketMode.Hidden, PacketEntityManager.IsEntity(obj.GetType()));
			while (info.Stopwatch.ElapsedMilliseconds < timeout)
			{
				if (TCPClient.Connected == false || Connection.IsWork == false)
				{
					throw new ExceptionEasyTCPAbortConnect("Lost connect with server!");
				}
				if (info.Packet != null)
					return Connection.Serialization.FromRaw<T>(info.Packet.Bytes);
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
