using EasyTCP;
using EasyTCP.Packets;
using EasyTCP.Serialize;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace TestClient.Examples.Serialize
{
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
}
