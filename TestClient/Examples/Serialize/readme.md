# ISerialize
пример реализации интерфейса ISerialize и добавлением в него самописного шифрования.
# Клиент
```C#
[Serializable]
	public class SecureSerializeTestPacket
	{
		public string Message { get; set; } = "SecureSerializeTestPacket is work!!!!";
		public override string ToString()
		{
			return $"{base.ToString()} | {Message}";
		}
	}
	internal class example_client
	{
		private byte[] public_key = new byte[] { 48, 129, 137, 2, 129, 129, 0, 190, 198, 163, 196, 175, 81, 66, 182, 20, 140, 114, 1, 108, 35, 34, 183, 218, 29, 147, 84, 95, 129, 32, 77, 234, 131, 103, 224, 174, 210, 156, 62, 121, 169, 42, 139, 229, 117, 179, 137, 199, 184, 98, 212, 254, 93, 198, 65, 195, 56, 185, 142, 90, 195, 163, 27, 196, 227, 202, 183, 245, 124, 115, 171, 137, 131, 195, 253, 46, 188, 83, 252, 138, 9, 9, 130, 83, 98, 202, 240, 91, 111, 71, 30, 107, 81, 196, 10, 173, 178, 17, 1, 207, 157, 148, 187, 147, 154, 111, 103, 80, 106, 138, 36, 167, 133, 146, 165, 95, 201, 248, 218, 242, 212, 75, 37, 77, 70, 46, 39, 21, 26, 126, 213, 209, 170, 229, 57, 2, 3, 1, 0, 1 };
		public void Example()
		{
			EasyTCP.Client client = new EasyTCP.Client();

			var sec = new SecureSerialize();
			sec.PublicKey = public_key;

			client.PacketEntityManager.RegistrationPacket<SecureSerializeTestPacket>(1);
			client.Connect("localhost", 2020, sec);

			while (true)
			{
				Console.WriteLine(client.SendAndWaitResponse<SecureSerializeTestPacket>(new SecureSerializeTestPacket()));
				Thread.Sleep(1);
			}
		}
	}
```
# Сервер
```C#
internal class example_server
	{
		private byte[] private_key = new byte[] { 48, 130, 2, 92, 2, 1, 0, 2, 129, 129, 0, 190, 198, 163, 196, 175, 81, 66, 182, 20, 140, 114, 1, 108, 35, 34, 183, 218, 29, 147, 84, 95, 129, 32, 77, 234, 131, 103, 224, 174, 210, 156, 62, 121, 169, 42, 139, 229, 117, 179, 137, 199, 184, 98, 212, 254, 93, 198, 65, 195, 56, 185, 142, 90, 195, 163, 27, 196, 227, 202, 183, 245, 124, 115, 171, 137, 131, 195, 253, 46, 188, 83, 252, 138, 9, 9, 130, 83, 98, 202, 240, 91, 111, 71, 30, 107, 81, 196, 10, 173, 178, 17, 1, 207, 157, 148, 187, 147, 154, 111, 103, 80, 106, 138, 36, 167, 133, 146, 165, 95, 201, 248, 218, 242, 212, 75, 37, 77, 70, 46, 39, 21, 26, 126, 213, 209, 170, 229, 57, 2, 3, 1, 0, 1, 2, 129, 128, 127, 160, 220, 135, 4, 210, 212, 82, 131, 196, 193, 176, 121, 235, 183, 154, 79, 237, 97, 87, 28, 221, 130, 3, 30, 84, 242, 245, 185, 127, 100, 207, 215, 12, 121, 78, 70, 32, 76, 16, 108, 240, 202, 13, 188, 110, 119, 232, 30, 246, 160, 12, 192, 100, 9, 134, 214, 93, 158, 141, 27, 74, 59, 6, 234, 179, 213, 150, 185, 113, 190, 178, 179, 174, 161, 51, 87, 185, 55, 62, 159, 70, 66, 193, 241, 21, 243, 33, 251, 220, 159, 56, 0, 38, 87, 146, 161, 225, 95, 221, 202, 148, 253, 253, 24, 198, 14, 130, 68, 94, 153, 32, 23, 181, 113, 71, 143, 132, 219, 50, 244, 217, 232, 43, 140, 126, 212, 209, 2, 65, 0, 239, 230, 245, 70, 52, 155, 112, 38, 26, 10, 187, 240, 189, 162, 141, 37, 159, 26, 228, 193, 104, 93, 165, 199, 40, 179, 187, 150, 149, 115, 241, 132, 206, 60, 72, 244, 190, 164, 31, 188, 129, 22, 61, 74, 29, 225, 164, 47, 236, 191, 217, 86, 171, 104, 126, 173, 184, 45, 254, 145, 48, 95, 122, 55, 2, 65, 0, 203, 147, 202, 97, 8, 203, 2, 199, 247, 108, 161, 31, 142, 207, 163, 129, 152, 67, 118, 132, 153, 158, 234, 124, 35, 161, 229, 216, 136, 231, 97, 40, 167, 222, 164, 98, 172, 98, 85, 119, 212, 52, 123, 78, 172, 228, 233, 127, 70, 34, 151, 157, 41, 29, 140, 5, 126, 242, 163, 57, 10, 181, 36, 15, 2, 65, 0, 159, 190, 67, 126, 95, 19, 77, 167, 33, 90, 26, 113, 32, 100, 247, 229, 160, 63, 49, 41, 148, 12, 31, 146, 49, 9, 21, 21, 29, 41, 90, 30, 27, 145, 202, 230, 165, 118, 245, 230, 248, 113, 205, 151, 231, 179, 211, 55, 82, 71, 33, 58, 115, 226, 157, 207, 161, 63, 135, 46, 56, 110, 171, 27, 2, 64, 105, 10, 21, 151, 33, 169, 86, 3, 5, 136, 56, 78, 135, 42, 93, 204, 37, 91, 81, 208, 179, 79, 10, 224, 8, 166, 165, 104, 167, 162, 243, 63, 189, 246, 35, 205, 129, 242, 174, 244, 200, 58, 88, 17, 77, 38, 67, 208, 86, 200, 204, 127, 219, 210, 18, 8, 87, 235, 44, 10, 231, 154, 117, 67, 2, 64, 106, 25, 143, 111, 46, 221, 234, 113, 85, 15, 122, 7, 194, 149, 96, 115, 102, 167, 202, 183, 59, 105, 197, 123, 240, 195, 112, 193, 204, 120, 189, 88, 94, 126, 248, 245, 75, 58, 21, 93, 120, 108, 141, 80, 69, 176, 14, 165, 154, 249, 131, 77, 67, 212, 176, 50, 105, 223, 137, 71, 118, 61, 219, 222 };

		EasyTCP.Server server = new EasyTCP.Server();
		public void Example()
		{
			server.CallbackConnectClientEvent += Server_CallbackConnectClientEvent;
			server.CallbackDisconnectClientEvent += Server_CallbackDisconnectClientEvent;

			var sec = new TestClient.Examples.Serialize.SecureSerialize();
			sec.PrivateKey = private_key;


			server.PacketEntityManager.RegistrationPacket<SecureSerializeTestPacket>(1).CallbackReceiveEvent += Example_server_CallbackReceiveEvent;

			server.Start(2020, serialization: sec);
		}

		private void Example_server_CallbackReceiveEvent(object Packet, EasyTCP.Packets.Packet RawPacket)
		{
			Console.WriteLine($"[SERVER] {Packet}");
			RawPacket.Answer(RawPacket);
		}

		private void Server_CallbackDisconnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] Disconnect: {client.IpPort}");
		}

		private void Server_CallbackConnectClientEvent(EasyTCP.ServerClient client)
		{
			Console.WriteLine($"[SERVER] Connect: {client.IpPort}");
		}
	}
```
# ISerialize
```C#
[Serializable]
	public class SecureAES
	{
		public byte[] AES_KEY;
		public byte[] AES_IV;
		public void CreateKey(bool iv_generate = true)
		{
			var aes = Aes.Create();
			aes.KeySize = 128;
			aes.BlockSize = 128;
			aes.Padding = PaddingMode.Zeros;
			aes.GenerateKey();
			if (iv_generate)
			{
				aes.GenerateIV();
				AES_IV = aes.IV;
			}
			else
			{
				AES_IV = new byte[16];
				for (int i = 0; i < 16; i++)
				{
					AES_IV[i] = 0;
				}
			}
			AES_KEY = aes.Key;
		}
		private Aes GetAES()
		{
			Aes aes = Aes.Create();
			aes.KeySize = 128;
			aes.BlockSize = 128;
			aes.Padding = PaddingMode.Zeros;
			aes.Key = AES_KEY;
			aes.IV = AES_IV;
			return aes;
		}
		public byte[] Encrypt(byte[] data)
		{
			var aes = GetAES();

			using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
			{
				return PerformCryptography(data, encryptor);
			}
		}
		private byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
		{
			using (var ms = new MemoryStream())
			using (var cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write))
			{
				cryptoStream.Write(data, 0, data.Length);
				cryptoStream.FlushFinalBlock();

				return ms.ToArray();
			}
		}
		public byte[] Decrypt(byte[] data)
		{
			var aes = GetAES();

			using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
			{
				return PerformCryptography(data, decryptor);
			}
		}
	}
	public class SecureSerialize : ISerialization
	{
		private SecureAES AES = new SecureAES();
		private SecureAES LastAES = new SecureAES();
		public bool Initial { get; private set; } = false;
		private Connection Connection { get; set; }
		public byte[] PublicKey { get; set; } = new byte[0];
		public byte[] PrivateKey { get; set; } = new byte[0];


		public void InitConnection(Connection connection)
		{
			Connection = connection;
			Connection.CallbackReceiveSerializationEvent += Connection_CallbackReceiveEvent;
			if (connection.Mode == TypeConnection.Server)
				return;
			AES.CreateKey();
			LastAES = AES;

			var info = Connection.SendAndWaitUnlimited(AES, PacketType.Serialize);
			Initial = true;
			while (info.Packet == null)
			{
				Thread.Sleep(16);
			}
			Task.Run(TimerUpdate);
		}
		private void TimerUpdate()
		{
			while (Connection != null && Connection.IsWork)
			{
				Thread.Sleep(10000);
				Console.WriteLine("UPDATE KEYS");
				var aes = new SecureAES();
				aes.CreateKey();
				var data = Raw(aes);
				LastAES = AES;
				AES = aes;
				Connection.WriteStream(data, HeaderPacket.Create(PacketType.Serialize)).Wait();
			}
			Console.WriteLine("END TimerUpdate");
		}
		private void Connection_CallbackReceiveEvent(EasyTCP.Packets.Packet packet)
		{
			if (packet.Header.Type == PacketType.Serialize && Connection.Mode == TypeConnection.Server)
			{
				var x = AES;
				AES = FromRaw<SecureAES>(packet.Bytes);
				LastAES = x;
				if (Initial == false)
					packet.AnswerNull();
				Initial = true;
			}
		}
		private byte[] EncryptWithRSA(byte[] data, byte[] publicKey)
		{
			using (var rsa = new RSACryptoServiceProvider())
			{
				rsa.ImportRSAPublicKey(publicKey, out _);

				SecureAES aes = new SecureAES();
				aes.CreateKey(false);
				byte[] encryptedData = aes.Encrypt(data);

				// Шифруем симметричный ключ AES с помощью RSA
				byte[] encryptedKey = rsa.Encrypt(aes.AES_KEY, false);
				// Объединяем зашифрованный ключ и зашифрованные данные
				byte[] encryptedResult = new byte[encryptedKey.Length + encryptedData.Length];
				Buffer.BlockCopy(encryptedKey, 0, encryptedResult, 0, encryptedKey.Length);
				Buffer.BlockCopy(encryptedData, 0, encryptedResult, encryptedKey.Length, encryptedData.Length);

				return encryptedResult;
			}
		}

		private byte[] DecryptWithRSA(byte[] encryptedData, byte[] privateKey)
		{
			using (var rsa = new RSACryptoServiceProvider())
			{
				rsa.ImportRSAPrivateKey(privateKey, out _);

				byte[] encryptedKey = new byte[rsa.KeySize / 8];
				byte[] encryptedDataOnly = new byte[encryptedData.Length - encryptedKey.Length];
				Buffer.BlockCopy(encryptedData, 0, encryptedKey, 0, encryptedKey.Length);
				Buffer.BlockCopy(encryptedData, encryptedKey.Length, encryptedDataOnly, 0, encryptedDataOnly.Length);

				byte[] decryptedKey = rsa.Decrypt(encryptedKey, false);

				SecureAES aes = new SecureAES();
				aes.CreateKey(false);
				aes.AES_KEY = decryptedKey;

				byte[] decryptedData = aes.Decrypt(encryptedDataOnly);

				return decryptedData;
			}
		}

		public T _FromRaw<T>(byte[] data)
		{
			if (typeof(T).IsValueType)
			{
				IntPtr ptr = Marshal.AllocHGlobal(data.Length);
				Marshal.Copy(data, 0, ptr, data.Length);
				T _struct = (T)Marshal.PtrToStructure(ptr, typeof(T));
				Marshal.FreeHGlobal(ptr);
				return _struct;
			}
			using (MemoryStream memoryStream = new MemoryStream(data))
			{
				BinaryFormatter formatter = new BinaryFormatter();
				return (T)formatter.Deserialize(memoryStream);
			}
		}
		public T FromRaw<T>(byte[] data)
		{
			byte[] bytes = new byte[data.Length];
			if (Initial)
				bytes = AES.Decrypt(data);
			else
				bytes = DecryptWithRSA(data, PrivateKey);
			try
			{
				return _FromRaw<T>(bytes);
			}
			catch
			{
				if (Initial)
					bytes = LastAES.Decrypt(data);
				else
					bytes = DecryptWithRSA(data, PrivateKey);
				return _FromRaw<T>(bytes);
			}
		}

		public object _FromRaw(byte[] data, Type type)
		{
			if (type.IsValueType)
			{
				IntPtr ptr = Marshal.AllocHGlobal(data.Length);
				Marshal.Copy(data, 0, ptr, data.Length);
				object _struct = Marshal.PtrToStructure(ptr, type);
				Marshal.FreeHGlobal(ptr);
				return _struct;
			}
			using (MemoryStream memoryStream = new MemoryStream(data))
			{
				BinaryFormatter formatter = new BinaryFormatter();
				return formatter.Deserialize(memoryStream);
			}
		}
		public object FromRaw(byte[] data, Type type)
		{
			byte[] bytes = new byte[data.Length];
			if (Initial)
				bytes = AES.Decrypt(data);
			else
				bytes = DecryptWithRSA(data, PrivateKey);
			try
			{
				return _FromRaw(bytes, type);
			}
			catch
			{
				if (Initial)
					bytes = LastAES.Decrypt(data);
				else
					bytes = DecryptWithRSA(data, PrivateKey);
				return _FromRaw(bytes, type);
			}
		}
		/// <summary>
		/// работает только с классами!!!
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public object _FromRaw(byte[] data)
		{
			using (MemoryStream memoryStream = new MemoryStream(data))
			{
				BinaryFormatter formatter = new BinaryFormatter();
				return formatter.Deserialize(memoryStream);
			}
		}
		public object FromRaw(byte[] data)
		{
			byte[] bytes = new byte[data.Length];
			if (Initial)
				bytes = AES.Decrypt(data);
			else
				bytes = DecryptWithRSA(data, PrivateKey);
			try
			{
				return _FromRaw(bytes);
			}
			catch
			{
				if (Initial)
					bytes = LastAES.Decrypt(data);
				else
					bytes = DecryptWithRSA(data, PrivateKey);
				return _FromRaw(bytes);
			}
		}

		public byte[] _Raw(object obj)
		{
			if (obj.GetType().IsValueType)
			{
				int structSize = Marshal.SizeOf(obj);
				byte[] struct_bytes = new byte[structSize];

				IntPtr ptr = Marshal.AllocHGlobal(structSize);
				Marshal.StructureToPtr(obj, ptr, false);
				Marshal.Copy(ptr, struct_bytes, 0, structSize);
				Marshal.FreeHGlobal(ptr);
				return struct_bytes;
			}
			using (MemoryStream memoryStream = new MemoryStream())
			{
				BinaryFormatter formatter = new BinaryFormatter();
				formatter.Serialize(memoryStream, obj);
				return memoryStream.ToArray();
			}
		}
		public byte[] Raw(object obj)
		{
			if (Initial)
			{
				return AES.Encrypt(_Raw(obj));
			}
			else
			{
				return EncryptWithRSA(_Raw(obj), PublicKey);
			}
		}
	}
```
