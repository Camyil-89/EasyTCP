
using System.Runtime.Serialization.Formatters.Binary;

namespace EasyTCP.Packets
{
	public class StandardSerialize : ISerialization
	{
		public T FromRaw<T>(byte[] data)
		{
			using (MemoryStream memoryStream = new MemoryStream(data))
			{
				BinaryFormatter formatter = new BinaryFormatter();
				return (T)formatter.Deserialize(memoryStream);
			}
		}

		public byte[] Raw(object obj)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				BinaryFormatter formatter = new BinaryFormatter();
				formatter.Serialize(memoryStream, obj);
				return memoryStream.ToArray();
			}
		}
	}
}
