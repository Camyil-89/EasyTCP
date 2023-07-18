using EasyTCP.Packets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EasyTCP.Serialize
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
	[Serializable]
	public class SecurePacket : BasePacket
	{
		public SecurePacket()
		{
			Type = TypePacket.Serialize;
		}
		public SecureAES AES { get; set; } = null;
		public override string ToString()
		{
			return $"{base.ToString()} | {BitConverter.ToString(AES.AES_KEY)}";
		}
	}
	public class SecureSerialize : ISerialization
	{
		private SecureAES AES = new SecureAES();
		public bool Initial { get; private set; } = false;
		private Connection Connection { get; set; }
		public byte[] PublicKey { get; set; } = new byte[0];
		public byte[] PrivateKey { get; set; } = new byte[0];
		public T FromRaw<T>(byte[] data)
		{
			if (Initial)
			{
				using (MemoryStream memoryStream = new MemoryStream(AES.Decrypt(data)))
				{
					BinaryFormatter formatter = new BinaryFormatter();
					return (T)formatter.Deserialize(memoryStream);
				}
			}
			else
			{
				using (MemoryStream memoryStream = new MemoryStream(DecryptWithRSA(data, PrivateKey)))
				{
					BinaryFormatter formatter = new BinaryFormatter();
					return (T)formatter.Deserialize(memoryStream);
				}
			}

		}
		public byte[] Raw(object obj)
		{
			Console.WriteLine(obj);
			if (Initial)
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(memoryStream, obj);
					return AES.Encrypt(memoryStream.ToArray());
				}
			}
			else
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					BinaryFormatter formatter = new BinaryFormatter();
					formatter.Serialize(memoryStream, obj);
					return EncryptWithRSA(memoryStream.ToArray(), PublicKey);
				}
			}

		}


		public void InitConnection(Connection connection)
		{
			Connection = connection;
			Connection.CallbackReceiveSerializationEvent += Connection_CallbackReceiveEvent;
			if (connection.Mode == TypeConnection.Server)
				return;
			AES.CreateKey();

			var info = Connection.SendAndWaitUnlimited(new SecurePacket() { AES = AES });
			Initial = true;
			while (info.Packet == null)
			{
				Thread.Sleep(16);
			}
			Task.Run(TimerUpdate);
		}
		private void TimerUpdate()
		{
			while (Connection != null && Connection.NetworkStream != null)
			{
				Thread.Sleep(1);
				Console.WriteLine("UPDATE KEYS");
				var aes = new SecureAES();
				aes.CreateKey();
				Connection.Send(new SecurePacket() { AES = aes }).Wait();
				AES = aes;
			}
			Console.WriteLine("END TimerUpdate");
		}
		private void Connection_CallbackReceiveEvent(Packets.BasePacket packet)
		{
			if (packet.Type == Packets.TypePacket.Serialize)
			{
				AES = ((SecurePacket)packet).AES;
				Initial = true;
				packet.Answer(new Packet() { UID = packet.UID });
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
	}
}
